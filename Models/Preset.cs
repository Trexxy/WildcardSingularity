namespace WildcardSingularity.Models;

public class Preset
{
    public string Name { get; set; } = string.Empty;
    public List<PersistedRule> Rules { get; set; } = [];
}
