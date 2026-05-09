using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using AudioAnalyzer.Models;

namespace AudioAnalyzer.Services;

public class StorageService
{
    private static readonly string AppDataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ToneAndBeats",
        "Analysis");

    public StorageService()
    {
        if (!Directory.Exists(AppDataDir))
        {
            Directory.CreateDirectory(AppDataDir);
        }
    }

    public async Task SaveBpmAnalysisAsync(string audioFilePath, BpmAnalysisResult result)
    {
        var fileName = GetAnalysisFileName(audioFilePath);
        var fullPath = Path.Combine(AppDataDir, fileName);

        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(fullPath, json);
    }

    public async Task<BpmAnalysisResult?> LoadBpmAnalysisAsync(string audioFilePath)
    {
        var fileName = GetAnalysisFileName(audioFilePath);
        var fullPath = Path.Combine(AppDataDir, fileName);

        if (!File.Exists(fullPath)) return null;

        try
        {
            var json = await File.ReadAllTextAsync(fullPath);
            return JsonSerializer.Deserialize<BpmAnalysisResult>(json);
        }
        catch (Exception ex)
        {
            LoggerService.Log($"StorageService.LoadBpmAnalysisAsync failed for {audioFilePath}: {ex.Message}");
            return null;
        }
    }

    public async Task SaveVersionPreferenceAsync(string version, bool skip)
    {
        var fullPath = Path.Combine(AppDataDir, "update_prefs.json");
        var prefs = new { LastSkippedVersion = version, SkipUpdates = skip };
        var json = JsonSerializer.Serialize(prefs);
        await File.WriteAllTextAsync(fullPath, json);
    }

    public async Task<string?> GetLastSkippedVersionAsync()
    {
        var fullPath = Path.Combine(AppDataDir, "update_prefs.json");
        if (!File.Exists(fullPath)) return null;

        try
        {
            var json = await File.ReadAllTextAsync(fullPath);
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("LastSkippedVersion").GetString();
        }
        catch { return null; }
    }

    private string GetAnalysisFileName(string audioFilePath)
    {
        // Simple hash of the file path or just the filename if it's unique enough
        var hash = audioFilePath.GetHashCode().ToString("X");
        return $"{hash}_bpm.json";
    }
}
