using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ookii.Dialogs.Wpf;
using WildcardSingularity.Models;
using WildcardSingularity.Services;

namespace WildcardSingularity.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly FileProcessorService _processor;
    private readonly SettingsService _settings;
    private CancellationTokenSource? _saveCts;
    private DispatcherTimer? _countdownTimer;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    private ObservableCollection<FileItem> _files = [];

    [ObservableProperty]
    private string _folderPath = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    private int _intervalSeconds = 10;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotRunning))]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    private bool _isRunning;

    [ObservableProperty]
    private string _statusMessage = "Stopped";

    [ObservableProperty]
    private int _countdownSeconds;

    public bool IsNotRunning => !IsRunning;

    public MainViewModel(FileProcessorService processor, SettingsService settings)
    {
        _processor = processor;
        _settings = settings;
        _processor.OnError += msg =>
            Application.Current.Dispatcher.BeginInvoke(() => StatusMessage = msg);

        RestoreSettings();
    }

    private void RestoreSettings()
    {
        var s = _settings.Load();
        IntervalSeconds = s.IntervalSeconds;

        if (string.IsNullOrEmpty(s.FolderPath))
            return;

        if (Directory.Exists(s.FolderPath))
        {
            FolderPath = s.FolderPath;
            LoadFiles(s.FolderPath, s.SelectedFileNames);
        }
        else
        {
            StatusMessage = "Saved folder no longer exists.";
        }
    }

    private void LoadFiles(string folder, ICollection<string>? selectedNames = null)
    {
        Files = new ObservableCollection<FileItem>(
            Directory.GetFiles(folder, "*.txt")
                .OrderBy(f => f)
                .Select(f => new FileItem
                {
                    FilePath = f,
                    FileName = Path.GetFileName(f),
                    IsSelected = selectedNames is null || selectedNames.Contains(Path.GetFileName(f))
                }));

        foreach (var file in Files)
            file.PropertyChanged += (_, _) => ScheduleSave();
    }

    [RelayCommand]
    private void BrowseFolder()
    {
        var dialog = new VistaFolderBrowserDialog();
        if (dialog.ShowDialog() != true) return;

        FolderPath = dialog.SelectedPath;
        LoadFiles(FolderPath);
        ScheduleSave();
    }

    private bool CanStart() =>
        !IsRunning && Files.Count > 0 && Files.Any(f => f.IsSelected) && IntervalSeconds >= 1;

    [RelayCommand(CanExecute = nameof(CanStart))]
    private void Start()
    {
        _processor.Start(Files, IntervalSeconds);
        IsRunning = true;
        StatusMessage = "Running…";
        StartCountdown();
        SaveNow();
    }

    [RelayCommand]
    private void Stop()
    {
        _processor.Stop();
        IsRunning = false;
        StatusMessage = "Stopped";
        StopCountdown();
        SaveNow();
    }

    private void StartCountdown()
    {
        CountdownSeconds = IntervalSeconds;
        _countdownTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _countdownTimer.Tick += (_, _) =>
        {
            CountdownSeconds--;
            if (CountdownSeconds <= 0)
                CountdownSeconds = IntervalSeconds;
        };
        _countdownTimer.Start();
    }

    private void StopCountdown()
    {
        _countdownTimer?.Stop();
        _countdownTimer = null;
        CountdownSeconds = 0;
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var f in Files) f.IsSelected = true;
        StartCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void DeselectAll()
    {
        foreach (var f in Files) f.IsSelected = false;
        StartCommand.NotifyCanExecuteChanged();
    }

    partial void OnIntervalSecondsChanged(int value) => ScheduleSave();

    private void ScheduleSave()
    {
        StartCommand.NotifyCanExecuteChanged();
        _saveCts?.Cancel();
        _saveCts = new CancellationTokenSource();
        var token = _saveCts.Token;
        Task.Delay(500, token).ContinueWith(
            _ => SaveNow(),
            token,
            TaskContinuationOptions.OnlyOnRanToCompletion,
            TaskScheduler.Default);
    }

    private void SaveNow()
    {
        _settings.Save(new AppSettings
        {
            FolderPath = FolderPath,
            IntervalSeconds = IntervalSeconds,
            SelectedFileNames = Files.Where(f => f.IsSelected).Select(f => f.FileName).ToList()
        });
    }
}
