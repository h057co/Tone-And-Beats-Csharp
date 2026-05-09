using System.Diagnostics;
using System.IO;
using AudioAnalyzer.Services;
using AudioAnalyzer.Models;

namespace AudioAnalyzer.PerfTest;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== PERFORMANCE AUDIT - Tone & Beats ===");
        Console.WriteLine();

        string testFolder = @"O:\Desarrollos\audiotest";
        if (!Directory.Exists(testFolder))
        {
            testFolder = @"..\..\..\Assets\audiotest";
            if (!Directory.Exists(testFolder))
            {
                testFolder = @"..\..\Assets\audiotest";
            }
        }
        
        var files = Directory.GetFiles(testFolder)
            .Where(f => !f.EndsWith(".bak") && !f.EndsWith(".bk"))
            .OrderBy(f => f)
            .ToArray();
        
        Console.WriteLine($"Archivos encontrados: {files.Length}");
        Console.WriteLine();

        var results = new List<PerformanceResult>();
        var stopwatchTotal = Stopwatch.StartNew();
        
        var dependencyService = new DependencyService();
        
        if (!dependencyService.IsEssentiaAvailable())
        {
            Console.WriteLine("Essentia binary missing. Downloading...");
            await dependencyService.DownloadEssentiaAsync(null!);
            Console.WriteLine("Essentia binary ready.");
        }

        var essentiaWrapper = new EssentiaWrapper(dependencyService);
        var bpmDetector = new BpmDetector(essentiaWrapper);
        var keyDetector = new KeyDetector();
        var loudnessAnalyzer = new LoudnessAnalyzer(dependencyService);
        var waveformAnalyzer = new WaveformAnalyzer();

        for (int i = 0; i < files.Length; i++)
        {
            string file = files[i];
            string fileName = Path.GetFileName(file);
            
            // Extract expected BPM from filename
            double expectedBpm = 0;
            var match = System.Text.RegularExpressions.Regex.Match(fileName, @"bpm\s+([0-9]+([.,][0-9]+)?)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success)
            {
                string val = match.Groups[1].Value.Replace(',', '.');
                double.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out expectedBpm);
            }

            Console.WriteLine($"[{i+1}/{files.Length}] Procesando: {fileName}");
            if (expectedBpm > 0) Console.WriteLine($"  Esperado: {expectedBpm} BPM");
            
            var stopwatch = Stopwatch.StartNew();

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));
                // We use DetectBpmAsync which returns (Primary, Alternative)
                var bpmTask = bpmDetector.DetectBpmAsync(file);
                
                await Task.WhenAll(bpmTask);
                
                stopwatch.Stop();
                
                var (primaryBpm, altBpm) = bpmTask.Result;
                
                double error = expectedBpm > 0 ? Math.Abs(primaryBpm - expectedBpm) : 0;
                bool isCorrect = expectedBpm > 0 ? error < 0.5 : true;

                // Handle common harmonic matches (Double/Half time)
                bool isHarmonic = false;
                if (!isCorrect && expectedBpm > 0)
                {
                    if (Math.Abs(primaryBpm * 2 - expectedBpm) < 0.5 || Math.Abs(primaryBpm / 2 - expectedBpm) < 0.5)
                    {
                        isHarmonic = true;
                    }
                }

                Console.WriteLine($"  OK - BPM: {primaryBpm:F1} (Alt: {altBpm:F1}) | Error: {error:F2}");

                results.Add(new PerformanceResult
                {
                    FileName = fileName,
                    DurationMs = (int)stopwatch.ElapsedMilliseconds,
                    Success = true,
                    DetectedBpm = primaryBpm,
                    ExpectedBpm = expectedBpm,
                    Error = error.ToString("F2"),
                    IsCorrect = isCorrect,
                    IsHarmonic = isHarmonic
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Console.WriteLine($"  ERROR: {ex.Message}");
                results.Add(new PerformanceResult
                {
                    FileName = fileName,
                    DurationMs = (int)stopwatch.ElapsedMilliseconds,
                    Success = false,
                    ErrorText = ex.Message
                });
            }

            Console.WriteLine();
        }

        stopwatchTotal.Stop();

        Console.WriteLine("===============================================");
        Console.WriteLine("        AUDITORIA DE PRECISION BPM             ");
        Console.WriteLine("===============================================");
        Console.WriteLine();
        
        Console.WriteLine($"| #  | Archivo                | Esperado | Detectado | Error | Estado |");
        Console.WriteLine("|----|------------------------|----------|-----------|-------|--------|");
        
        int idx = 1;
        int correctCount = 0;
        int totalWithExpected = 0;

        foreach (var r in results)
        {
            string status = "N/A";
            if (r.ExpectedBpm > 0)
            {
                totalWithExpected++;
                if (r.IsCorrect) { status = "CORRECTO"; correctCount++; }
                else if (r.IsHarmonic) status = "ARMONICO";
                else status = "FALLO";
            }

            Console.WriteLine($"| {idx,2} | {r.FileName,-22} | {r.ExpectedBpm,8:F1} | {r.DetectedBpm,9:F1} | {r.Error,5} | {status,-8} |");
            idx++;
        }

        Console.WriteLine();
        if (totalWithExpected > 0)
        {
            double accuracy = (double)correctCount / totalWithExpected * 100;
            Console.WriteLine($"Precision Total: {accuracy:F1}% ({correctCount}/{totalWithExpected})");
        }
        Console.WriteLine($"Tiempo total: {stopwatchTotal.Elapsed.TotalSeconds:F2} s");
    }
}

class PerformanceResult
{
    public string FileName { get; set; } = "";
    public long DurationMs { get; set; }
    public bool Success { get; set; }
    public double DetectedBpm { get; set; }
    public double ExpectedBpm { get; set; }
    public string Error { get; set; } = "";
    public string? ErrorText { get; set; }
    public bool IsCorrect { get; set; }
    public bool IsHarmonic { get; set; }
}
