using System.Windows;
using WildcardSingularity.Services;
using WildcardSingularity.ViewModels;
using WildcardSingularity.Views;

namespace WildcardSingularity;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var vm = new MainViewModel(new FileProcessorService(), new SettingsService());
        new MainWindow { DataContext = vm }.Show();
    }
}
