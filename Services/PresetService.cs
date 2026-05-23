using System.IO;
using System.Text.Json;
using WildcardSingularity.Models;

namespace WildcardSingularity.Services;

public class PresetService
{
    private static readonly string PresetsPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "WildcardSingularity", "presets.json");

    public List<Preset> Load()
    {
        try
        {
            if (!File.Exists(PresetsPath))
                return [];

            var json = File.ReadAllText(PresetsPath);
            return JsonSerializer.Deserialize<List<Preset>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public void Save(List<Preset> presets)
    {
        var dir = Path.GetDirectoryName(PresetsPath)!;
        Directory.CreateDirectory(dir);

        var tmp = PresetsPath + ".tmp";
        var json = JsonSerializer.Serialize(presets, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(tmp, json);
        File.Move(tmp, PresetsPath, overwrite: true);
    }
}
