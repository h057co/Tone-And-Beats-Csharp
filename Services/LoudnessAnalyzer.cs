using System.IO;
using AudioAnalyzer.Interfaces;
using AudioAnalyzer.Models;
using System.Diagnostics;

namespace AudioAnalyzer.Services;

public class LoudnessAnalyzer : ILoudnessAnalyzerService
{
    private readonly IDependencyService _dependencyService;

    public LoudnessAnalyzer(IDependencyService dependencyService)
    {
        _dependencyService = dependencyService;
    }

    public async Task<LoudnessResult> AnalyzeAsync(string filePath, IProgress<int>? progress = null)
    {
        var result = new LoudnessResult();

        try
        {
            LoggerService.Log("LoudnessAnalyzer.AnalyzeAsync - Starting for: " + filePath);
            progress?.Report(10);

            if (!_dependencyService.IsFFmpegAvailable())
            {
                throw new FileNotFoundException("FFmpeg module not found.");
            }

            var ffmpegPath = _dependencyService.GetFFmpegPath();
            LoggerService.Log("LoudnessAnalyzer - Using FFmpeg: " + ffmpegPath);

            progress?.Report(30);

            var startInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = $"-threads 0 -hide_banner -i \"{filePath}\" -af \"loudnorm=I=-23:print_format=json\" -f null -",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8
            };

            using var process = new Process { StartInfo = startInfo };
            
            var outputBuilder = new System.Text.StringBuilder();
            var errorBuilder = new System.Text.StringBuilder();

            process.OutputDataReceived += (s, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };
            process.ErrorDataReceived += (s, e) => { if (e.Data != null) errorBuilder.AppendLine(e.Data); };

            if (!process.Start())
                throw new Exception("Could not start FFmpeg");

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Wait for exit or timeout (3 minutes)
            var exited = await Task.Run(() => process.WaitForExit(180000));
            
            if (!exited)
            {
                LoggerService.Log("LoudnessAnalyzer - FFmpeg timed out");
                try { process.Kill(); } catch { }
            }

            var fullOutput = outputBuilder.ToString() + errorBuilder.ToString();
            LoggerService.Log("LoudnessAnalyzer - FFmpeg execution complete. ExitCode: " + (exited ? process.ExitCode.ToString() : "Timeout"));

            progress?.Report(70);

            result = ParseLoudnormOutput(fullOutput);

            progress?.Report(100);

            LoggerService.Log($"LoudnessAnalyzer - Final Result: Integrated={result.IntegratedLufs} LUFS, LRA={result.LoudnessRange} LU, TP={result.TruePeak} dBFS");
        }
        catch (FileNotFoundException ex)
        {
            LoggerService.Log("LoudnessAnalyzer - Error: " + ex.Message);
            result.HasError = true;
            result.ErrorMessage = "FFmpeg Missing";
        }
        catch (Exception ex)
        {
            LoggerService.Log("LoudnessAnalyzer.AnalyzeAsync - Exception: " + ex.Message);
            result.HasError = true;
            result.ErrorMessage = "Analysis Error";
        }

        return result;
    }

    private double ExtractValue(string output, string key)
    {
        // JSON format regex: "key" : "-10.5" or "key": "-10.5"
        var jsonPattern = $"\"{key}\"\\s*:\\s*\"?([\\-\\d\\.]+)\"?";
        var jsonMatch = System.Text.RegularExpressions.Regex.Match(output, jsonPattern);
        
        if (jsonMatch.Success && double.TryParse(jsonMatch.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var jsonVal))
        {
            return jsonVal;
        }

        // Fallback for non-JSON output (summary text)
        string searchKey = key.Replace("input_", "");
        if (searchKey == "i") searchKey = "I";
        else if (searchKey == "lra") searchKey = "LRA";
        else if (searchKey == "tp") searchKey = "Peak";

        var textPattern = $"{searchKey}:\\s*([\\-\\d\\.]+)\\s*(?:LUFS|LU|dBFS)?";
        var textMatch = System.Text.RegularExpressions.Regex.Match(output, textPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (textMatch.Success && double.TryParse(textMatch.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var textVal))
        {
            return textVal;
        }

        return double.NaN;
    }

    private LoudnessResult ParseLoudnormOutput(string output)
    {
        var result = new LoudnessResult();

        if (string.IsNullOrWhiteSpace(output))
        {
            LoggerService.Log("LoudnessAnalyzer - FFmpeg output was empty");
            result.HasError = true;
            result.ErrorMessage = "Empty output from FFmpeg";
            return result;
        }

        try
        {
            result.IntegratedLufs = ExtractValue(output, "input_i");
            result.TruePeak = ExtractValue(output, "input_tp");
            result.LoudnessRange = ExtractValue(output, "input_lra");

            // Validation: if any are NaN, something is wrong with parsing
            if (double.IsNaN(result.IntegratedLufs) || double.IsNaN(result.TruePeak) || double.IsNaN(result.LoudnessRange))
            {
                LoggerService.Log("LoudnessAnalyzer - Error: Parsing failed for one or more values.");
                result.HasError = true;
                result.ErrorMessage = "Failed to parse FFmpeg results";
                
                // Set defaults if NaN
                if (double.IsNaN(result.IntegratedLufs)) result.IntegratedLufs = 0;
                if (double.IsNaN(result.TruePeak)) result.TruePeak = 0;
                if (double.IsNaN(result.LoudnessRange)) result.LoudnessRange = 0;
            }
        }
        catch (Exception ex)
        {
            LoggerService.Log("LoudnessAnalyzer.ParseLoudnormOutput - Error: " + ex.Message);
            result.HasError = true;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }
}