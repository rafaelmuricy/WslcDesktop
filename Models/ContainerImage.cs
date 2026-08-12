using System.Text.Json.Serialization;

namespace WslcDesktop.Models;

public sealed class ContainerImage
{
    [JsonPropertyName("Repository")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("Tag")]
    public string Tag { get; set; } = string.Empty;

    [JsonPropertyName("Id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("Created")]
    public long Created { get; set; }

    [JsonPropertyName("Size")]
    public long Size { get; set; }

    public string FullName
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Name) || Name == "<none>")
            {
                return ShortId;
            }

            if (string.IsNullOrWhiteSpace(Tag) || Tag == "<none>")
            {
                return Name;
            }

            return $"{Name}:{Tag}";
        }
    }

    public string ShortId
    {
        get
        {
            var id = Id;
            const string prefix = "sha256:";
            if (id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                id = id[prefix.Length..];
            }

            return id.Length <= 12 ? id : id[..12];
        }
    }

    public string CreatedDisplay
    {
        get
        {
            var created = DateTimeOffset.FromUnixTimeSeconds(Created);
            var elapsed = DateTimeOffset.UtcNow - created;

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

    public string SizeDisplay
    {
        get
        {
            // container CLI uses decimal units (1000), e.g. 225930265 -> "225.93 MB"
            const double kb = 1000;
            const double mb = kb * 1000;
            const double gb = mb * 1000;

            return Size switch
            {
                >= (long)gb => $"{Size / gb:0.##} GB",
                >= (long)mb => $"{Size / mb:0.##} MB",
                >= (long)kb => $"{Size / kb:0.##} KB",
                _ => $"{Size} B"
            };
        }
    }
}
