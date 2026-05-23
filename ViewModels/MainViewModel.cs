using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
    private readonly PresetService _presets;
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

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SavePresetCommand))]
    [NotifyCanExecuteChangedFor(nameof(LoadPresetCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeletePresetCommand))]
    private string _presetName = string.Empty;

    [ObservableProperty]
    private ObservableCollection<Preset> _presetList = [];

    public ObservableCollection<SearchReplaceRule> Rules { get; } = [];

    public bool IsNotRunning => !IsRunning;

    public MainViewModel(FileProcessorService processor, SettingsService settings, PresetService presets)
    {
        _processor = processor;
        _settings = settings;
        _presets = presets;
        _processor.OnError += msg =>
            Application.Current.Dispatcher.BeginInvoke(() => StatusMessage = msg);

        Rules.CollectionChanged += OnRulesCollectionChanged;
        PresetList = new ObservableCollection<Preset>(_presets.Load());
        RestoreSettings();
    }

    private void OnRulesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
            foreach (SearchReplaceRule rule in e.NewItems)
                rule.PropertyChanged += (_, _) => ScheduleSave();
        ScheduleSave();
    }

    private void RestoreSettings()
    {
        var s = _settings.Load();
        IntervalSeconds = s.IntervalSeconds;

        foreach (var r in s.Rules)
            Rules.Add(new SearchReplaceRule { SearchPattern = r.SearchPattern, Replacement = r.Replacement });

        if (string.IsNullOrEmpty(s.FolderPath))
            return;

        if (Directory.Exists(s.FolderPath))
        {
            FolderPath = s.FolderPath;
            LoadFiles(s.FolderPath, s.Files);
        }
        else
        {
            StatusMessage = "Saved folder no longer exists.";
        }
    }

    private void LoadFiles(string folder, ICollection<FileSettings>? savedFiles = null)
    {
        Files = new ObservableCollection<FileItem>(
            Directory.GetFiles(folder, "*.txt")
                .OrderBy(f => f)
                .Select(f =>
                {
                    var name = Path.GetFileName(f);
                    var saved = savedFiles?.FirstOrDefault(s => s.FileName == name);
                    return new FileItem
                    {
                        FilePath = f,
                        FileName = name,
                        LineCount = saved?.LineCount ?? 0,
                    };
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
        !IsRunning && Files.Count > 0 && Files.Any(f => f.LineCount > 0) && IntervalSeconds >= 1;

    [RelayCommand(CanExecute = nameof(CanStart))]
    private void Start()
    {
        _processor.Start(Files, Rules, IntervalSeconds);
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
        foreach (var f in Files) f.LineCount = 1;
    }

    [RelayCommand]
    private void DeselectAll()
    {
        foreach (var f in Files) f.LineCount = 0;
    }

    [RelayCommand]
    private void AddRule() => Rules.Add(new SearchReplaceRule());

    [RelayCommand]
    private void RemoveRule(SearchReplaceRule rule) => Rules.Remove(rule);

    private bool CanSavePreset() => !string.IsNullOrWhiteSpace(PresetName);

    [RelayCommand(CanExecute = nameof(CanSavePreset))]
    private void SavePreset()
    {
        var preset = new Preset
        {
            Name = PresetName,
            Rules = Rules.Select(r => new PersistedRule
            {
                SearchPattern = r.SearchPattern,
                Replacement = r.Replacement,
            }).ToList()
        };

        var existing = PresetList.FirstOrDefault(p => p.Name == PresetName);
        if (existing is not null)
            PresetList[PresetList.IndexOf(existing)] = preset;
        else
            PresetList.Add(preset);

        _presets.Save([.. PresetList]);
        LoadPresetCommand.NotifyCanExecuteChanged();
        DeletePresetCommand.NotifyCanExecuteChanged();
    }

    private bool CanLoadPreset() => PresetList.Any(p => p.Name == PresetName);

    [RelayCommand(CanExecute = nameof(CanLoadPreset))]
    private void LoadPreset()
    {
        var preset = PresetList.First(p => p.Name == PresetName);
        Rules.Clear();
        foreach (var rule in preset.Rules)
            Rules.Add(new SearchReplaceRule { SearchPattern = rule.SearchPattern, Replacement = rule.Replacement });
    }

    private bool CanDeletePreset() => PresetList.Any(p => p.Name == PresetName);

    [RelayCommand(CanExecute = nameof(CanDeletePreset))]
    private void DeletePreset()
    {
        var existing = PresetList.FirstOrDefault(p => p.Name == PresetName);
        if (existing is null) return;
        PresetList.Remove(existing);
        _presets.Save([.. PresetList]);
        PresetName = string.Empty;
        LoadPresetCommand.NotifyCanExecuteChanged();
        DeletePresetCommand.NotifyCanExecuteChanged();
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
            Files = Files.Select(f => new FileSettings
            {
                FileName = f.FileName,
                LineCount = f.LineCount,
            }).ToList(),
            Rules = Rules.Select(r => new PersistedRule
            {
                SearchPattern = r.SearchPattern,
                Replacement = r.Replacement,
            }).ToList()
        });
    }
}
