using System.IO;
using System.Timers;
using WildcardSingularity.Models;

namespace WildcardSingularity.Services;

public class FileProcessorService
{
    public event Action<string>? OnError;

    private System.Timers.Timer? _timer;
    private IEnumerable<FileItem>? _files;
    private IEnumerable<SearchReplaceRule>? _rules;

    public void Start(IEnumerable<FileItem> files, IEnumerable<SearchReplaceRule> rules, int intervalSeconds)
    {
        _files = files;
        _rules = rules;
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
        var active = _files.Where(f => f.LineCount > 0).ToList();
        var rules = _rules?.Where(r => !string.IsNullOrEmpty(r.SearchPattern)).ToList() ?? [];
        foreach (var file in active)
            ProcessFile(file, rules);
    }

    private void ProcessFile(FileItem file, List<SearchReplaceRule> rules)
    {
        try
        {
            var lines = File.ReadAllLines(file.FilePath)
                            .Where(l => !string.IsNullOrWhiteSpace(l))
                            .ToArray();

            if (lines.Length == 0) return;

            var count = Math.Min(file.LineCount, lines.Length);
            var selected = PickRandom(lines, count);

            foreach (var rule in rules)
                selected = selected.Select(l => l.Replace(rule.SearchPattern, rule.Replacement)).ToArray();

            var outputDir = Path.Combine(Path.GetDirectoryName(file.FilePath)!, "ws");
            Directory.CreateDirectory(outputDir);
            File.WriteAllLines(Path.Combine(outputDir, file.FileName), selected);
        }
        catch (Exception ex)
        {
            OnError?.Invoke($"{file.FileName}: {ex.Message}");
        }
    }

    private static string[] PickRandom(string[] source, int count)
    {
        var pool = (string[])source.Clone();
        for (int i = 0; i < count; i++)
        {
            int j = Random.Shared.Next(i, pool.Length);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }
        return pool[..count];
    }
}
