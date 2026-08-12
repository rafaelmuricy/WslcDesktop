using System.Text.Json.Serialization;

namespace WslcDesktop.Models;

public sealed class ContainerInstance
{
    [JsonPropertyName("Id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("Image")]
    public string Image { get; set; } = string.Empty;

    [JsonPropertyName("CreatedAt")]
    public long CreatedAt { get; set; }

    [JsonPropertyName("State")]
    public int State { get; set; }

    [JsonPropertyName("StateChangedAt")]
    public long StateChangedAt { get; set; }

    [JsonPropertyName("Ports")]
    public List<ContainerPortMapping> Ports { get; set; } = [];

    public string ShortId
    {
        get
        {
            var id = Id;
            return id.Length <= 12 ? id : id[..12];
        }
    }

    public string CreatedDisplay => FormatRelativeTime(CreatedAt);

    public string StatusDisplay
    {
        get
        {
            var stateName = State switch
            {
                1 => "created",
                2 => "running",
                3 => "exited",
                4 => "paused",
                _ => $"state {State}"
            };

            if (StateChangedAt <= 0)
            {
                return stateName;
            }

            return $"{stateName} {FormatRelativeTime(StateChangedAt)}";
        }
    }

    public string DetailsStatusDisplay
    {
        get
        {
            var stateName = State switch
            {
                1 => "Created",
                2 => "Running",
                3 => "Exited",
                4 => "Paused",
                _ => $"State {State}"
            };

            if (StateChangedAt <= 0)
            {
                return stateName;
            }

            return $"{stateName} ({FormatRelativeTime(StateChangedAt)})";
        }
    }

    public string PortsDisplay
    {
        get
        {
            if (Ports.Count == 0)
            {
                return string.Empty;
            }

            return string.Join(", ", Ports.Select(port => port.Display));
        }
    }

    public bool IsRunning => State == 2;

    public double RunningDotOpacity => IsRunning ? 1 : 0;

    public double StoppedDotOpacity => IsRunning ? 0 : 1;

    public string StartStopGlyph => IsRunning ? "\uE71A" : "\uE768";

    public string StartStopTooltip => IsRunning ? "Stop" : "Play";

    private static string FormatRelativeTime(long unixSeconds)
    {
        var instant = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
        var elapsed = DateTimeOffset.UtcNow - instant;

        if (elapsed.TotalSeconds < 60)
        {
            var seconds = Math.Max(1, (int)elapsed.TotalSeconds);
            return $"{seconds} second{(seconds == 1 ? "" : "s")} ago";
        }

        if (elapsed.TotalMinutes < 60)
        {
            var minutes = (int)elapsed.TotalMinutes;
            return $"{minutes} minute{(minutes == 1 ? "" : "s")} ago";
        }

        if (elapsed.TotalHours < 24)
        {
            var hours = (int)elapsed.TotalHours;
            return $"{hours} hour{(hours == 1 ? "" : "s")} ago";
        }

        if (elapsed.TotalDays < 30)
        {
            var days = (int)elapsed.TotalDays;
            return $"{days} day{(days == 1 ? "" : "s")} ago";
        }

        if (elapsed.TotalDays < 365)
        {
            var months = Math.Max(1, (int)(elapsed.TotalDays / 30));
            return $"{months} month{(months == 1 ? "" : "s")} ago";
        }

        var years = Math.Max(1, (int)(elapsed.TotalDays / 365));
        return $"{years} year{(years == 1 ? "" : "s")} ago";
    }
}

public sealed class ContainerPortMapping
{
    [JsonPropertyName("BindingAddress")]
    public string BindingAddress { get; set; } = string.Empty;

    [JsonPropertyName("HostPort")]
    public int HostPort { get; set; }

    [JsonPropertyName("ContainerPort")]
    public int ContainerPort { get; set; }

    [JsonPropertyName("Protocol")]
    public int Protocol { get; set; }

    public string Display
    {
        get
        {
            var protocol = Protocol switch
            {
                6 => "tcp",
                17 => "udp",
                _ => Protocol.ToString()
            };

            if (string.IsNullOrWhiteSpace(BindingAddress))
            {
                return $"{HostPort}->{ContainerPort}/{protocol}";
            }

            return $"{BindingAddress}:{HostPort}->{ContainerPort}/{protocol}";
        }
    }
}
