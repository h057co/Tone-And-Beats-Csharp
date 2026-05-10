using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AudioAnalyzer.Services;
using AudioAnalyzer.Models;

namespace AudioAnalyzer.PerfTest;

public class KeyTestHarness
{
    private static readonly string[] NoteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

    public static async Task RunTestsAsync()
    {
        Console.WriteLine("===============================================");
        Console.WriteLine("     SYNTHETIC KEY DETECTION AUDIT (288 tests) ");
        Console.WriteLine("===============================================");
        Console.WriteLine();

        var detector = new KeyDetector();
        int sampleRate = 44100;
        int durationSeconds = 3; 

        int totalTests = 0;
        int correctTests = 0;
        int minorErrors = 0; // Correct key, wrong mode, or relative major/minor

        Console.WriteLine("| Target Key | Bass Note | Detected Key | Alt Detected | Status |");
        Console.WriteLine("|------------|-----------|--------------|--------------|--------|");

        // Test all 12 roots, Major and Minor (24 triads)
        for (int root = 0; root < 12; root++)
        {
            for (int mode = 0; mode < 2; mode++) // 0 = Major, 1 = Minor
            {
                string expectedMode = mode == 0 ? "Major" : "Minor";
                string expectedKey = $"{NoteNames[root]} {expectedMode}";

                // Test all 12 possible bass notes
                for (int bass = 0; bass < 12; bass++)
                {
                    totalTests++;
                    string bassNote = NoteNames[bass];

                    float[] samples = GenerateChordWithBass(root, mode, bass, sampleRate, durationSeconds);

                    var result = await detector.DetectKeyAsync(samples, sampleRate, null);

                    string status = "FAIL";
                    
                    // Perfect match
                    if (result.Key == expectedKey)
                    {
                        status = "PASS";
                        correctTests++;
                    }
                    else if (result.AlternativeKey == expectedKey)
                    {
                        status = "PASS (ALT)";
                        correctTests++;
                    }
                    else
                    {
                        // Check if it's relative minor/major
                        int relativeRoot = mode == 0 ? (root + 9) % 12 : (root + 3) % 12;
                        string relativeMode = mode == 0 ? "Minor" : "Major";
                        string relativeKey = $"{NoteNames[relativeRoot]} {relativeMode}";

                        if (result.Key == relativeKey || result.AlternativeKey == relativeKey)
                        {
                            status = "RELATIVE";
                            minorErrors++;
                        }
                    }

                    if (status != "PASS")
                    {
                        Console.WriteLine($"| {expectedKey,-10} | {bassNote,-9} | {result.Key,-12} | {result.AlternativeKey,-12} | {status,-6} |");
                    }
                }
            }
        }

        Console.WriteLine("===============================================");
        Console.WriteLine($"Total Tests: {totalTests}");
        Console.WriteLine($"Perfect Matches: {correctTests} ({(double)correctTests / totalTests * 100:F1}%)");
        Console.WriteLine($"Relative Matches: {minorErrors} ({(double)minorErrors / totalTests * 100:F1}%)");
        Console.WriteLine($"Failures: {totalTests - correctTests - minorErrors} ({(double)(totalTests - correctTests - minorErrors) / totalTests * 100:F1}%)");
        Console.WriteLine("===============================================");
    }

    private static float[] GenerateChordWithBass(int rootNote, int mode, int bassNote, int sampleRate, int durationSeconds)
    {
        int numSamples = sampleRate * durationSeconds;
        float[] buffer = new float[numSamples];

        // Frequencies
        // C4 = 261.63Hz
        double c4 = 261.625565;
        double rootFreq = c4 * Math.Pow(2, rootNote / 12.0);

        // Triad
        double thirdFreq = mode == 0 
            ? rootFreq * Math.Pow(2, 4.0 / 12.0) // Major third
            : rootFreq * Math.Pow(2, 3.0 / 12.0); // Minor third
        double fifthFreq = rootFreq * Math.Pow(2, 7.0 / 12.0); // Perfect fifth

        // Bass (Octave 2 or 3)
        double bassFreq = (c4 / 4.0) * Math.Pow(2, bassNote / 12.0);

        for (int i = 0; i < numSamples; i++)
        {
            double t = (double)i / sampleRate;

            // Generate sine waves with some simple harmonics to simulate real instruments
            double sample = 0;
            
            // Bass (louder)
            sample += 0.5 * Math.Sin(2 * Math.PI * bassFreq * t);
            sample += 0.25 * Math.Sin(2 * Math.PI * bassFreq * 2 * t); // 1st harmonic
            
            // Triad (softer)
            sample += 0.2 * Math.Sin(2 * Math.PI * rootFreq * t);
            sample += 0.2 * Math.Sin(2 * Math.PI * thirdFreq * t);
            sample += 0.2 * Math.Sin(2 * Math.PI * fifthFreq * t);

            // Taper ends to avoid clicks
            double envelope = 1.0;
            if (i < 4410) envelope = i / 4410.0;
            if (i > numSamples - 4410) envelope = (numSamples - i) / 4410.0;

            buffer[i] = (float)(sample * envelope * 0.5); // Attenuate
        }

        return buffer;
    }
}
