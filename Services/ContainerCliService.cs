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

    private static async Task<string> RunAsync(params string[] args)
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
