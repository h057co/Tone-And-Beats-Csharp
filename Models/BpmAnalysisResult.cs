using System;
using System.Collections.Generic;

namespace AudioAnalyzer.Models;

/// <summary>
/// Represents the full rhythmic profile of an audio file.
/// </summary>
public class BpmAnalysisResult
{
    /// <summary>
    /// The finalized musical tempo (after Urban Strategy).
    /// </summary>
    public double PrimaryBpm { get; set; }

    /// <summary>
    /// Harmonic candidates (e.g., 0.5x, 1.5x, 2.0x).
    /// </summary>
    public List<double> AlternateBpms { get; set; } = new();

    /// <summary>
    /// Score (0.0 - 1.0) of the primary detection.
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Absolute timestamps (seconds) of every detected beat.
    /// </summary>
    public List<double> BeatTimesSeconds { get; set; } = new();

    /// <summary>
    /// Time delta between consecutive beats (to detect drift).
    /// </summary>
    public List<double> BeatIntervals { get; set; } = new();

    /// <summary>
    /// True if the final BPM was shifted by the Urban Strategy.
    /// </summary>
    public bool IsReinterpreted { get; set; }

    /// <summary>
    /// "HalfTime", "DoubleTime", or "None".
    /// </summary>
    public string ReinterpretationType { get; set; } = "None";

    /// <summary>
    /// Identifier for the engine that produced the result (e.g., "Essentia-2013").
    /// </summary>
    public string EngineVersion { get; set; } = string.Empty;

    /// <summary>
    /// When the analysis was performed.
    /// </summary>
    public DateTime AnalysisTimestamp { get; set; } = DateTime.UtcNow;
}
