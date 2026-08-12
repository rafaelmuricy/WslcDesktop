using System.Text.Json.Serialization;

namespace WslcDesktop.Models;

public sealed class ContainerStats
{
    [JsonPropertyName("ID")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("CPUPerc")]
    public string CpuPerc { get; set; } = string.Empty;

    [JsonPropertyName("MemUsage")]
    public string MemUsage { get; set; } = string.Empty;

    [JsonPropertyName("MemPerc")]
    public string MemPerc { get; set; } = string.Empty;

    [JsonPropertyName("NetIO")]
    public string NetIO { get; set; } = string.Empty;

    [JsonPropertyName("BlockIO")]
    public string BlockIO { get; set; } = string.Empty;

    [JsonPropertyName("PIDs")]
    public int Pids { get; set; }

    public string RamDisplay
    {
        get
        {
            var parts = MemUsage.Split('/', 2, StringSplitOptions.TrimEntries);
            return parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]) ? parts[0] : "—";
        }
    }

    public string FooterDisplay
    {
        get
        {
            var cpu = string.IsNullOrWhiteSpace(CpuPerc) ? "—" : CpuPerc;
            var block = string.IsNullOrWhiteSpace(BlockIO) ? "—" : BlockIO;
            return $"RAM {RamDisplay}    CPU {cpu}    Block I/O: {block}";
        }
    }
}
