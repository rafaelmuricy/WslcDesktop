using System.Diagnostics;
using System.Text;
using System.Text.Json;
using WslcDesktop.Models;

namespace WslcDesktop.Services;

public static class ContainerCliService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<IReadOnlyList<ContainerImage>> ListImagesAsync(CancellationToken cancellationToken = default)
    {
        var output = await RunAsync("image", "list", "--format", "json");
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(output))
        {
            return [];
        }

        var images = JsonSerializer.Deserialize<List<ContainerImage>>(output, JsonOptions);
        return images ?? [];
    }

    public static async Task<IReadOnlyList<ContainerInstance>> ListContainersAsync(CancellationToken cancellationToken = default)
    {
        var output = await RunAsync("list", "-a", "--format", "json");
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(output))
        {
            return [];
        }

        var containers = JsonSerializer.Deserialize<List<ContainerInstance>>(output, JsonOptions);
        return containers ?? [];
    }

    public static async Task DeleteImageAsync(string imageId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(imageId);
        await RunAsync("image", "delete", imageId);
        cancellationToken.ThrowIfCancellationRequested();
    }

    public static async Task StartContainerAsync(string containerId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(containerId);
        await RunAsync("start", containerId);
        cancellationToken.ThrowIfCancellationRequested();
    }

    public static async Task StopContainerAsync(string containerId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(containerId);
        await RunAsync("stop", containerId);
        cancellationToken.ThrowIfCancellationRequested();
    }

    public static async Task DeleteContainerAsync(string containerId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(containerId);
        await RunAsync("delete", containerId);
        cancellationToken.ThrowIfCancellationRequested();
    }

    public static async Task RestartContainerAsync(string containerId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(containerId);
        await StopContainerAsync(containerId, cancellationToken);
        await StartContainerAsync(containerId, cancellationToken);
    }

    public static async Task<string> GetLogsAsync(
        string containerId,
        int? tail = 200,
        bool timestamps = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(containerId);

        var args = new List<string> { "logs" };
        if (timestamps)
        {
            args.Add("-t");
        }

        if (tail is > 0)
        {
            args.Add("-n");
            args.Add(tail.Value.ToString());
        }

        args.Add(containerId);

        var output = await RunAsync([.. args], workingDirectory: null);
        cancellationToken.ThrowIfCancellationRequested();
        return output;
    }

    public static async Task<string> InspectAsync(
        string objectId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objectId);
        var output = await RunAsync("inspect", objectId);
        cancellationToken.ThrowIfCancellationRequested();
        return output;
    }

    public static async Task<ContainerStats?> GetContainerStatsAsync(
        string containerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(containerId);

        var output = await RunAsync("stats", "--format", "json", containerId);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(output))
        {
            return null;
        }

        var stats = JsonSerializer.Deserialize<List<ContainerStats>>(output, JsonOptions);
        return stats?.FirstOrDefault(item =>
            item.Id.StartsWith(containerId, StringComparison.OrdinalIgnoreCase) ||
            containerId.StartsWith(item.Id, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(item.Name, containerId, StringComparison.OrdinalIgnoreCase));
    }

    public static async Task<IReadOnlyList<string>> GetExposedPortsAsync(
        string imageId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(imageId);

        var output = await RunAsync("image", "inspect", imageId);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(output))
        {
            return [];
        }

        using var document = JsonDocument.Parse(output);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
        {
            return [];
        }

        var config = root[0].GetProperty("Config");
        if (!config.TryGetProperty("ExposedPorts", out var exposedPorts) ||
            exposedPorts.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        return exposedPorts.EnumerateObject()
            .Select(property =>
            {
                var slashIndex = property.Name.IndexOf('/');
                return slashIndex >= 0 ? property.Name[..slashIndex] : property.Name;
            })
            .Where(port => !string.IsNullOrWhiteSpace(port))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    public static async Task BuildAsync(string filePath, string imageName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var workingDirectory = Path.GetDirectoryName(filePath);
        if (string.IsNullOrWhiteSpace(workingDirectory))
        {
            throw new InvalidOperationException("Não foi possível determinar o diretório do arquivo.");
        }

        if (string.IsNullOrWhiteSpace(imageName))
        {
            throw new InvalidOperationException("Não foi possível determinar o nome da pasta para a tag da imagem.");
        }

        var folderPath = Path.GetDirectoryName(filePath)!;

        await RunAsync(["build", "-t", imageName.ToLower(), "-f", filePath, folderPath], workingDirectory);
        cancellationToken.ThrowIfCancellationRequested();
    }

    public static async Task RunContainerAsync(
        string imageReference,
        string? containerName = null,
        IEnumerable<(string HostPort, string ContainerPort)>? ports = null,
        IEnumerable<(string HostPath, string ContainerPath)>? volumes = null,
        IEnumerable<(string Name, string Value)>? environmentVariables = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(imageReference);

        var args = new List<string> { "run", "-d" };

        if (!string.IsNullOrWhiteSpace(containerName))
        {
            args.Add("--name");
            args.Add(containerName);
        }

        if (ports is not null)
        {
            foreach (var (hostPort, containerPort) in ports)
            {
                args.Add("-p");
                args.Add($"{hostPort}:{containerPort}");
            }
        }

        if (volumes is not null)
        {
            foreach (var (hostPath, containerPath) in volumes)
            {
                args.Add("-v");
                args.Add($"{hostPath}:{containerPath}");
            }
        }

        if (environmentVariables is not null)
        {
            foreach (var (name, value) in environmentVariables)
            {
                args.Add("-e");
                args.Add($"{name}={value}");
            }
        }

        args.Add(imageReference);

        await RunAsync([.. args], workingDirectory: null);
        cancellationToken.ThrowIfCancellationRequested();
    }


    private static Task<string> RunAsync(params string[] args) =>
        RunAsync(args, workingDirectory: null);

    private static async Task<string> RunAsync(string[] args, string? workingDirectory)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "container",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        if (!string.IsNullOrWhiteSpace(workingDirectory))
        {
            startInfo.WorkingDirectory = workingDirectory;
        }

        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = new Process { StartInfo = startInfo };

        if (!process.Start())
        {
            throw new InvalidOperationException("Não foi possível iniciar o comando 'container'.");
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (process.ExitCode != 0)
        {
            var message = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(message)
                    ? $"O comando 'container {string.Join(' ', args)}' falhou (código {process.ExitCode})."
                    : message.Trim());
        }

        return stdout.Trim();
    }
}
