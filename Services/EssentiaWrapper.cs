using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using AudioAnalyzer.Interfaces;
using AudioAnalyzer.Models;

namespace AudioAnalyzer.Services;

public class EssentiaWrapper
{
    private readonly IDependencyService _dependencyService;

    public EssentiaWrapper(IDependencyService dependencyService)
    {
        _dependencyService = dependencyService;
    }

    public async Task<BpmAnalysisResult?> AnalyzeAsync(string audioFilePath)
    {
        if (!_dependencyService.IsEssentiaAvailable())
        {
            LoggerService.Log("Essentia binary not available.");
            return null;
        }

        var essentiaPath = _dependencyService.GetEssentiaPath();
        var tempOutputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}_essentia.json");

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = essentiaPath,
                Arguments = $"\"{audioFilePath}\" \"{tempOutputPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                var error = await errorTask;
                LoggerService.Log($"Essentia failed with exit code {process.ExitCode}: {error}");
                return null;
            }

            if (!File.Exists(tempOutputPath))
            {
                LoggerService.Log("Essentia finished but output file not found.");
                return null;
            }

            var jsonContent = await File.ReadAllTextAsync(tempOutputPath);
            return ParseEssentiaOutput(jsonContent);
        }
        catch (Exception ex)
        {
            LoggerService.Log($"EssentiaWrapper.AnalyzeAsync failed: {ex.Message}");
            return null;
        }
        finally
        {
            if (File.Exists(tempOutputPath))
                File.Delete(tempOutputPath);
        }
    }

    private BpmAnalysisResult? ParseEssentiaOutput(string jsonContent)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            if (!root.TryGetProperty("rhythm", out var rhythm))
                return null;

            double confidence = 0;
            if (rhythm.TryGetProperty("beats_confidence", out var confProp))
            {
                if (confProp.ValueKind == JsonValueKind.Object)
                {
                    if (confProp.TryGetProperty("mean", out var confMean))
                        confidence = confMean.GetDouble();
                }
                else if (confProp.ValueKind == JsonValueKind.Number)
                {
                    confidence = confProp.GetDouble();
                }
            }
            else if (rhythm.TryGetProperty("bpm_histogram_first_peak_weight", out var weightProp))
            {
                // Fallback to histogram weight as a proxy for confidence
                if (weightProp.ValueKind == JsonValueKind.Object)
                {
                    if (weightProp.TryGetProperty("mean", out var meanWeight))
                        confidence = meanWeight.GetDouble();
                }
                else if (weightProp.ValueKind == JsonValueKind.Number)
                {
                    confidence = weightProp.GetDouble();
                }
            }

            double rawBpm = 0;
            if (rhythm.TryGetProperty("bpm", out var bpmProp))
            {
                rawBpm = bpmProp.ValueKind == JsonValueKind.Object
                    ? (bpmProp.TryGetProperty("mean", out var bpmMean) ? bpmMean.GetDouble() : 0)
                    : (bpmProp.ValueKind == JsonValueKind.Number ? bpmProp.GetDouble() : 0);
            }

            var result = new BpmAnalysisResult
            {
                PrimaryBpm = rawBpm,
                Confidence = confidence,
                EngineVersion = "Essentia-2013-Static",
                AnalysisTimestamp = DateTime.UtcNow
            };

            // Histogram peak data for cross-validation
            if (rhythm.TryGetProperty("bpm_histogram_first_peak_bpm", out var peak1Bpm))
                result.HistogramPeak1Bpm = peak1Bpm.ValueKind == JsonValueKind.Object
                    ? (peak1Bpm.TryGetProperty("mean", out var m1) ? m1.GetDouble() : 0)
                    : peak1Bpm.GetDouble();

            if (rhythm.TryGetProperty("bpm_histogram_first_peak_weight", out var peak1W))
                result.HistogramPeak1Weight = peak1W.ValueKind == JsonValueKind.Object
                    ? (peak1W.TryGetProperty("mean", out var w1) ? w1.GetDouble() : 0)
                    : peak1W.GetDouble();

            if (rhythm.TryGetProperty("bpm_histogram_second_peak_bpm", out var peak2Bpm))
                result.HistogramPeak2Bpm = peak2Bpm.ValueKind == JsonValueKind.Object
                    ? (peak2Bpm.TryGetProperty("mean", out var m2) ? m2.GetDouble() : 0)
                    : peak2Bpm.GetDouble();

            if (rhythm.TryGetProperty("bpm_histogram_second_peak_weight", out var peak2W))
                result.HistogramPeak2Weight = peak2W.ValueKind == JsonValueKind.Object
                    ? (peak2W.TryGetProperty("mean", out var w2) ? w2.GetDouble() : 0)
                    : peak2W.GetDouble();

            if (rhythm.TryGetProperty("beats_position", out var beatsPos))
            {
                foreach (var beat in beatsPos.EnumerateArray())
                {
                    result.BeatTimesSeconds.Add(beat.GetDouble());
                }
            }

            if (rhythm.TryGetProperty("beats_count", out var bcProp))
            {
                result.BeatsCount = bcProp.ValueKind == JsonValueKind.Number
                    ? bcProp.GetInt32()
                    : result.BeatTimesSeconds.Count;
            }
            else
            {
                result.BeatsCount = result.BeatTimesSeconds.Count;
            }

            if (rhythm.TryGetProperty("beats_intervals", out var intervals))
            {
                foreach (var interval in intervals.EnumerateArray())
                {
                    result.BeatIntervals.Add(interval.GetDouble());
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            LoggerService.Log($"ParseEssentiaOutput failed: {ex.Message}");
            return null;
        }
    }
}
