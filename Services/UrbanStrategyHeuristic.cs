using System;
using System.Collections.Generic;
using System.Linq;
using AudioAnalyzer.Models;

namespace AudioAnalyzer.Services;

public class UrbanStrategyHeuristic
{
    private const double TRESILLO_RATIO = 1.5;

    /// <summary>
    /// Analyzes if a detected BPM (likely technical) should be reinterpreted 
    /// for Urban genres (e.g., 140 -> 70, or 105 -> 157.5).
    /// </summary>
    public BpmAnalysisResult Apply(BpmAnalysisResult result)
    {
        double originalBpm = result.PrimaryBpm;
        bool heuristicApplied = false;

        // 1. Half-Time Rescue (Standard for Trap/Drill/Phonk)
        // High range: 140-185 -> 70-92.5
        if (originalBpm >= 140 && originalBpm <= 185)
        {
            result.PrimaryBpm = originalBpm / 2.0;
            result.AlternateBpms.Add(originalBpm);
            result.IsReinterpreted = true;
            result.ReinterpretationType = "HalfTime";
            heuristicApplied = true;
        }
        // 2. Double-Time Rescue (Low BPM tracks that are actually fast)
        // Low range: 60-72 -> 120-144
        else if (originalBpm >= 60 && originalBpm <= 72)
        {
            result.PrimaryBpm = originalBpm * 2.0;
            result.AlternateBpms.Add(originalBpm);
            result.IsReinterpreted = true;
            result.ReinterpretationType = "DoubleTime";
            heuristicApplied = true;
        }
        // 3. Tresillo / Reggaeton Correction (1.5x relationship) - VERY CONSERVATIVE
        // Only if confidence is low, suggesting the detector is confused by syncopation.
        else if (result.Confidence < 0.45)
        {
            if (originalBpm >= 110 && originalBpm <= 135)
            {
                // 115 -> 76.6 (Tresillo Down)
                result.PrimaryBpm = originalBpm / TRESILLO_RATIO;
                result.AlternateBpms.Add(originalBpm);
                result.IsReinterpreted = true;
                result.ReinterpretationType = "TresilloDown";
                heuristicApplied = true;
            }
            else if (originalBpm >= 85 && originalBpm <= 100)
            {
                // 95 -> 142.5 (Tresillo Up)
                result.PrimaryBpm = originalBpm * TRESILLO_RATIO;
                result.AlternateBpms.Add(originalBpm);
                result.IsReinterpreted = true;
                result.ReinterpretationType = "TresilloUp";
                heuristicApplied = true;
            }
        }

        // Always add common harmonics to alternatives if not already there
        AddHarmonicIfMissing(result, originalBpm * 2.0);
        AddHarmonicIfMissing(result, originalBpm / 2.0);
        
        if (heuristicApplied)
        {
            result.Confidence *= 0.92; // Slight penalty for heuristic dependency
        }

        return result;
    }

    private void AddHarmonicIfMissing(BpmAnalysisResult result, double bpm)
    {
        if (bpm <= 0 || bpm > 300) return;
        if (!result.AlternateBpms.Any(a => Math.Abs(a - bpm) < 1.0) && Math.Abs(result.PrimaryBpm - bpm) > 1.0)
        {
            result.AlternateBpms.Add(bpm);
        }
    }
}
