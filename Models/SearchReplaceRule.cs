using CommunityToolkit.Mvvm.ComponentModel;

namespace WildcardSingularity.Models;

public partial class SearchReplaceRule : ObservableObject
{
    [ObservableProperty]
    private string _searchPattern = string.Empty;

    [ObservableProperty]
    private string _replacement = string.Empty;
}
