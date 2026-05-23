using System.IO;
using System.Timers;
using WildcardSingularity.Models;

namespace WildcardSingularity.Services;

public class FileProcessorService
{
    public event Action<string>? OnError;

    private System.Timers.Timer? _timer;
    private IEnumerable<FileItem>? _files;

    public void Start(IEnumerable<FileItem> files, int intervalSeconds)
    {
        _files = files;
        _timer?.Dispose();
        _timer = new System.Timers.Timer(intervalSeconds * 1000);
        _timer.Elapsed += OnTick;
        _timer.AutoReset = true;
        _timer.Start();
        Task.Run(ProcessSelected);
    }

    public void Stop()
    {
        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;
    }

    private void OnTick(object? sender, ElapsedEventArgs e) => ProcessSelected();

    private void ProcessSelected()
    {
        if (_files is null) return;
        // Snapshot the selected files so collection changes mid-tick don't cause issues
        var selected = _files.Where(f => f.IsSelected).ToList();
        foreach (var file in selected)
            ProcessFile(file);
    }

    private void ProcessFile(FileItem file)
    {
        try
        {
            var lines = File.ReadAllLines(file.FilePath)
                            .Where(l => !string.IsNullOrWhiteSpace(l))
                            .ToArray();

            if (lines.Length == 0) return;

            var line = lines[Random.Shared.Next(lines.Length)];
            var outputDir = Path.Combine(Path.GetDirectoryName(file.FilePath)!, "ws");
            Directory.CreateDirectory(outputDir);
            File.WriteAllText(Path.Combine(outputDir, file.FileName), line);
        }
        catch (Exception ex)
        {
            OnError?.Invoke($"{file.FileName}: {ex.Message}");
        }
    }
}
