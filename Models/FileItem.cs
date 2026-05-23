using CommunityToolkit.Mvvm.ComponentModel;

namespace WildcardSingularity.Models;

public partial class FileItem : ObservableObject
{
    public string FilePath { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;

    [ObservableProperty]
    private int _lineCount = 0;

    public bool IsActive => LineCount > 0;

    partial void OnLineCountChanged(int value) => OnPropertyChanged(nameof(IsActive));
}
