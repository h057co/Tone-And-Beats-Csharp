using System;
using System.Collections.Generic;
using System.Linq;

namespace AudioAnalyzer.Services;

/// <summary>
/// Computes a statistically robust BPM from Essentia beat timestamps.
/// Uses median of inter-beat intervals with IQR outlier filtering.
/// The BIH VERIFIES the Essentia BPM rather than replacing it.
/// </summary>
public class BeatIntervalVerifier
{
    /// <summary>
    /// Minimum number of beats required for a reliable BIH calculation.
    /// 16 beats = 4 bars at 4/4 time — minimum for statistical significance.
    /// </summary>
    private const int MIN_BEATS = 16;

    /// <summary>
    /// Stability threshold: IQR/Median ratio below this = very stable tempo.
    /// 0.05 means beat intervals vary less than 5% — typical for quantized music.
    /// </summary>
    private const double STABILITY_THRESHOLD_HIGH = 0.05;

    /// <summary>
    /// Moderate stability threshold for cross-validation path.
    /// </summary>
    private const double STABILITY_THRESHOLD_MEDIUM = 0.12;

    public record BihResult(
        double Bpm,
        double Stability,
        double Iqr,
        double MedianInterval,
        int ValidIntervals,
        bool IsReliable);

    /// <summary>
    /// Computes BPM from beat timestamps using the Beat Interval Histogram method.
    /// </summary>
    public BihResult ComputeFromBeats(List<double> beatTimesSeconds)
    {
        if (beatTimesSeconds == null || beatTimesSeconds.Count < MIN_BEATS)
        {
            LoggerService.Log($"[BIH] Insufficient beats: {beatTimesSeconds?.Count ?? 0} < {MIN_BEATS}");
            return new BihResult(0, 0, 0, 0, 0, false);
        }

        // Step 1: Compute consecutive intervals
        var rawIntervals = new List<double>();
        for (int i = 1; i < beatTimesSeconds.Count; i++)
        {
            double interval = beatTimesSeconds[i] - beatTimesSeconds[i - 1];
            if (interval > 0.1 && interval < 3.0) // Sanity: 20-600 BPM range
                rawIntervals.Add(interval);
        }

        if (rawIntervals.Count < MIN_BEATS / 2)
        {
            LoggerService.Log($"[BIH] Too few valid intervals: {rawIntervals.Count}");
            return new BihResult(0, 0, 0, 0, rawIntervals.Count, false);
        }

        // Step 2: IQR outlier filtering
        rawIntervals.Sort();
        double q1 = Percentile(rawIntervals, 25);
        double q3 = Percentile(rawIntervals, 75);
        double iqr = q3 - q1;
        double lowerFence = q1 - 1.5 * iqr;
        double upperFence = q3 + 1.5 * iqr;

        var filtered = rawIntervals
            .Where(v => v >= lowerFence && v <= upperFence)
            .ToList();

        if (filtered.Count < 8)
        {
            LoggerService.Log($"[BIH] Too few intervals after IQR filter: {filtered.Count}");
            return new BihResult(0, 0, iqr, 0, filtered.Count, false);
        }

        // Step 3: Compute Trimmed Mean (10%) of filtered intervals
        // This is more precise than median for quantized music as it averages the "stable" core.
        int trimCount = (int)(filtered.Count * 0.10);
        var trimmed = filtered
            .Skip(trimCount)
            .Take(filtered.Count - 2 * trimCount)
            .ToList();

        double averageInterval = trimmed.Count > 0 ? trimmed.Average() : filtered.Average();

        // Step 4: BPM = 60 / average (no normalization — keep the raw value)
        double rawBpm = 60.0 / averageInterval;

        // Step 5: Stability = 1.0 - (IQR / average), clamped to [0, 1]
        double stability = averageInterval > 0
            ? Math.Max(0, Math.Min(1.0, 1.0 - (iqr / averageInterval)))
            : 0;

        bool isReliable = stability >= (1.0 - STABILITY_THRESHOLD_MEDIUM)
                          && filtered.Count >= MIN_BEATS;

        LoggerService.Log($"[BIH] Raw={rawBpm:F2}, " +
            $"Stability={stability:F3}, IQR={iqr:F4}s, " +
            $"AvgInterval={averageInterval:F4}s (Trimmed {trimCount}), " +
            $"Intervals={filtered.Count}/{rawIntervals.Count}, " +
            $"Reliable={isReliable}");

        return new BihResult(rawBpm, stability, iqr, averageInterval, filtered.Count, isReliable);
    }

    /// <summary>
    /// Arbitrates between BIH result and Essentia's raw BPM.
    /// 
    /// KEY PRINCIPLE: BIH VERIFIES, Essentia PROVIDES the value.
    /// When BIH confirms Essentia is correct (within harmonic tolerance),
    /// we use Essentia's calibrated BPM (which is properly rounded).
    /// BIH only overrides when Essentia disagrees with the beat timestamps.
    /// </summary>
    public (double bpm, string path) Arbitrate(
        BihResult bih,
        double essentiaBpm,
        double essentiaConfidence,
        double histPeak1Bpm,
        double histPeak1Weight,
        double histPeak2Bpm,
        double histPeak2Weight)
    {
        if (!bih.IsReliable || bih.Bpm <= 0)
        {
            LoggerService.Log($"[BIH Arbitrate] BIH not reliable → fallback to legacy");
            return (0, "legacy");
        }

        // Compute harmonic ratios between BIH and Essentia
        double ratio = essentiaBpm > 0 ? bih.Bpm / essentiaBpm : 0;

        double finalBpm = 0;
        string decisionPath = "";

        // Check if BIH confirms Essentia at any harmonic level
        if (essentiaBpm > 0 && IsHarmonicMatch(ratio))
        {
            finalBpm = essentiaBpm;
            decisionPath = "essentia_confirmed";
        }
        else if (histPeak1Bpm > 0 && IsHarmonicMatch(bih.Bpm / histPeak1Bpm))
        {
            finalBpm = histPeak1Bpm;
            decisionPath = "histpeak1_confirmed";
        }
        else if (histPeak2Bpm > 0 && IsHarmonicMatch(bih.Bpm / histPeak2Bpm))
        {
            finalBpm = histPeak2Bpm;
            decisionPath = "histpeak2_confirmed";
        }
        else
        {
            double normalizedBih = NormalizeToRange(bih.Bpm, 60, 180);
            double normalizedEssentia = NormalizeToRange(essentiaBpm, 60, 180);
            
            if (Math.Abs(normalizedBih - normalizedEssentia) < 3.0)
            {
                finalBpm = normalizedEssentia;
                decisionPath = "normalized_match";
            }
            else
            {
                finalBpm = normalizedBih;
                decisionPath = "bih_override";
            }
        }

        // ---------------------------------------------------------
        // METRICAL AMBIGUITY CORRECTIONS (Urban Strategy integration)
        // ---------------------------------------------------------
        
        // 1. Double-time Correction (e.g., 180 -> 90)
        // Umbral sincronizado a 160 BPM según feedback del usuario.
        if (finalBpm > 160)
        {
            double half = finalBpm / 2.0;
            // If histPeak2 strongly suggests the half-time, use it.
            if (histPeak2Bpm > 0 && Math.Abs(histPeak2Bpm - half) < 3.0 && histPeak2Weight > 0.1)
            {
                LoggerService.Log($"[BIH Arbitrate] {finalBpm:F1} is too high, matching HistPeak2={histPeak2Bpm:F1} → Halving");
                return (histPeak2Bpm, decisionPath + "_halved_by_peak2");
            }
        }

        // 2. Tresillo Correction (e.g., 115 -> 76.6)
        // Essentia often catches the 3-over-2 tresillo rhythm as the primary beat.
        // 115 / 1.5 = 76.66 BPM
        if (finalBpm > 110 && finalBpm < 125)
        {
            double tresilloBase = finalBpm / 1.5;
            
            // Check if secondary peak confirms the tresillo base
            if (histPeak2Bpm > 0 && Math.Abs(histPeak2Bpm - tresilloBase) < 2.0)
            {
                LoggerService.Log($"[BIH Arbitrate] Tresillo detected! {finalBpm:F1} -> HistPeak2={histPeak2Bpm:F1}");
                return (histPeak2Bpm, decisionPath + "_tresillo_peak2");
            }
            
            // Check if BIH raw median was closer to tresillo
            if (Math.Abs(NormalizeToRange(bih.Bpm, 60, 180) - tresilloBase) < 2.0)
            {
                LoggerService.Log($"[BIH Arbitrate] Tresillo detected! {finalBpm:F1} -> BIH={tresilloBase:F1}");
                return (tresilloBase, decisionPath + "_tresillo_bih");
            }
            
            // Only apply if we have high confidence it's urban, but we don't have genre data here.
            // DO NOT apply unconditionally, as it breaks legitimate 110-125 BPM House tracks.
        }

        LoggerService.Log($"[BIH Arbitrate] Final selection: {finalBpm:F1} via {decisionPath}");
        return (finalBpm, decisionPath);
    }

    /// <summary>
    /// Normalizes BPM to the specified range using octave (2x/0.5x) only.
    /// </summary>
    private double NormalizeToRange(double bpm, double min, double max)
    {
        if (bpm <= 0) return 0;
        while (bpm > max) bpm /= 2.0;
        while (bpm < min) bpm *= 2.0;
        return bpm;
    }

    private bool IsHarmonicMatch(double ratio)
    {
        if (ratio <= 0) return false;
        // Check for simple harmonic ratios: 1:1, 2:1, 1:2
        double[] harmonics = { 0.5, 1.0, 2.0 };
        foreach (var h in harmonics)
        {
            if (Math.Abs(ratio - h) < 0.06) return true;
        }
        return false;
    }

    private static double Percentile(List<double> sorted, double percentile)
    {
        if (sorted.Count == 0) return 0;
        double index = (percentile / 100.0) * (sorted.Count - 1);
        int lower = (int)Math.Floor(index);
        int upper = (int)Math.Ceiling(index);
        if (lower == upper || upper >= sorted.Count)
            return sorted[lower];
        double fraction = index - lower;
        return sorted[lower] + fraction * (sorted[upper] - sorted[lower]);
    }
}
