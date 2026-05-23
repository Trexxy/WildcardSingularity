namespace WildcardSingularity.Models;

public class AppSettings
{
    public string FolderPath { get; set; } = string.Empty;
    public int IntervalSeconds { get; set; } = 10;
    public List<FileSettings> Files { get; set; } = [];
    public List<PersistedRule> Rules { get; set; } = [];
}

public class FileSettings
{
    public string FileName { get; set; } = string.Empty;
    public int LineCount { get; set; } = 0;
}

public class PersistedRule
{
    public string SearchPattern { get; set; } = string.Empty;
    public string Replacement { get; set; } = string.Empty;
}
