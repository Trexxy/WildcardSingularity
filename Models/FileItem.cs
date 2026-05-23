using CommunityToolkit.Mvvm.ComponentModel;

namespace WildcardSingularity.Models;

public partial class FileItem : ObservableObject
{
    public string FilePath { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;

    [ObservableProperty]
    private bool _isSelected = true;
}
