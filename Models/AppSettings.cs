namespace WildcardSingularity.Models;

public class AppSettings
{
    public string FolderPath { get; set; } = string.Empty;
    public int IntervalSeconds { get; set; } = 10;
    public List<string> SelectedFileNames { get; set; } = [];
}
