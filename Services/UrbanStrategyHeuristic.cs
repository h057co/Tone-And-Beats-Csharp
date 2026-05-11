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

        // 1. Half-Time Rescue (Standard for Trap/Drill/Phonk/Reggaeton-Double)
        // High range: > 160 -> 80+
        if (originalBpm >= 160 && originalBpm <= 200)
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
        // Only if confidence is VERY low, suggesting the detector is definitely confused by syncopation.
        else if (result.Confidence < 0.35)
        {
            if (originalBpm >= 113 && originalBpm <= 125)
            {
                // 115 -> 76.6 (Tresillo Down)
                result.PrimaryBpm = originalBpm / TRESILLO_RATIO;
                result.AlternateBpms.Add(originalBpm);
                result.IsReinterpreted = true;
                result.ReinterpretationType = "TresilloDown";
                heuristicApplied = true;
            }
            else if (originalBpm >= 80 && originalBpm <= 110)
            {
                // Reggaeton core: 80-110. No aplicamos tresillo automático aquí 
                // para evitar "normalización artificial" en el rango más sensible.
                LoggerService.Log($"UrbanStrategy - In Reggaeton core range ({originalBpm:F1}). Bypassing auto-tresillo.");
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
