using Newtonsoft.Json.Linq;

public class WwiseObject
{
    public string? Name { get; set; }
    public string? Type { get; set; }
    public string? Id { get; set; }
    public string? Notes { get; set; }
    public double Duration { get; set; }
    public string? Path { get; set; }

    public JObject? Events { get; set; }
}