using AudioAnalyzer.Interfaces;

namespace AudioAnalyzer.Services;

public class KeyDetector : IKeyDetector
{
    private static readonly double[] MajorProfile = { 6.35, 2.23, 3.48, 2.33, 4.38, 4.09, 2.52, 5.19, 2.39, 3.66, 2.29, 2.88 };
    private static readonly double[] MinorProfile = { 6.33, 2.68, 3.52, 5.38, 2.60, 3.53, 2.54, 4.75, 3.98, 2.69, 3.34, 3.17 };

    private static readonly string[] NoteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

    public async Task<KeyDetectionResult> DetectKeyAsync(string filePath, IProgress<int>? progress = null)
    {
        return await Task.Run(() => 
        {
            var (monoSamples, sampleRate) = new AudioDataProvider().LoadMono(filePath);
            return DetectKeyFromSamples(monoSamples, sampleRate, progress);
        });
    }

    public async Task<KeyDetectionResult> DetectKeyAsync(float[] monoSamples, int sampleRate, IProgress<int>? progress = null)
    {
        return await Task.Run(() => DetectKeyFromSamples(monoSamples, sampleRate, progress));
    }

    /// <summary>
    /// Core key detection logic operating on pre-loaded mono samples.
    /// No file I/O occurs in this method.
    /// </summary>
    private KeyDetectionResult DetectKeyFromSamples(float[] monoSamples, int sampleRate, IProgress<int>? progress = null)
    {
        const int MaxAnalysisSeconds = 30;

        try
        {
            if (monoSamples.Length < sampleRate)
                return new KeyDetectionResult("Unknown", 0, 0);

            // Limit to MaxAnalysisSeconds from center of audio for key detection
            int maxSamples = MaxAnalysisSeconds * sampleRate;
            float[] analysisData;
            if (monoSamples.Length > maxSamples)
            {
                int startOffset = (monoSamples.Length - maxSamples) / 2;
                analysisData = monoSamples.AsSpan(startOffset, maxSamples).ToArray();
                LoggerService.Log($"KeyDetector.DetectKeyFromSamples - Trimmed {monoSamples.Length} -> {maxSamples} samples (center segment)");
            }
            else
            {
                analysisData = monoSamples;
            }

            var tuningOffset = DetectTuningOffset(analysisData, sampleRate);
            LoggerService.Log($"KeyDetector.DetectKeyFromSamples - Detected tuning offset: {tuningOffset * 100:F1} cents");

            progress?.Report(30);

            var pcp = ComputePitchClassProfile(analysisData, sampleRate, tuningOffset);
            
            progress?.Report(60);

            var (best, alternative) = FindBestKeys(pcp);
            
            progress?.Report(100);

            string keyName = $"{NoteNames[best.keyIndex]} {(best.mode == 0 ? "Major" : "Minor")}";
            string altName = $"{NoteNames[alternative.keyIndex]} {(alternative.mode == 0 ? "Major" : "Minor")}";
            
            return new KeyDetectionResult(keyName, best.correlation, tuningOffset * 100, altName);
        }
        catch (Exception ex)
        {
            LoggerService.Log($"KeyDetector.DetectKeyFromSamples - Error: {ex.Message}");
            return new KeyDetectionResult("Error", 0, 0);
        }
    }

    private double[] ComputePitchClassProfile(float[] samples, int sampleRate, double tuningOffset = 0)
    {
        const int fftSize = DspConstants.FFT_SIZE_KEY_DETECTION;
        const int hopSize = 8192;
        const int numBins = 12;
        const double fRef = 440.0; // A4

        var pcp = new double[numBins];
        int numFrames = (samples.Length - fftSize) / hopSize + 1;
        var magnitudes = new double[fftSize / 2];

        for (int frame = 0; frame < numFrames; frame++)
        {
            var frameStart = frame * hopSize;
            if (frameStart + fftSize > samples.Length) break;

            var window = samples.AsSpan(frameStart, fftSize);
            ComputeFFTMagnitudes(window, magnitudes, sampleRate);

            // Skip very low frequencies (below 50Hz) and high frequencies (above 2000Hz) for cleaner key detection
            int minBin = (int)(50.0 * fftSize / sampleRate);
            int maxBin = (int)(2000.0 * fftSize / sampleRate);
            minBin = Math.Max(1, minBin);
            maxBin = Math.Min(magnitudes.Length, maxBin);

            for (int bin = minBin; bin < maxBin; bin++)
            {
                double freq = bin * (double)sampleRate / fftSize;
                // Calculate semitones from A4, applying tuning compensation
                double semitones = 12.0 * Math.Log(freq / fRef, 2) + 9.0 - tuningOffset;
                
                // Map to [0, 12)
                double pitch = (semitones % 12 + 12) % 12;
                int pitchClass = (int)Math.Round(pitch) % 12;
                
                // Weight by magnitude
                double weight = magnitudes[bin];
                
                // --- BASS ATTENUATION ---
                // Reduce the impact of sub-bass and low frequencies (<250Hz) 
                // so that dissonant kicks/808s don't skew the overall melodic key detection.
                if (freq < 120.0)
                {
                    weight *= 0.15; // 85% attenuation for sub-bass
                }
                else if (freq < 250.0)
                {
                    weight *= 0.5; // 50% attenuation for low-mids
                }
                
                // Soft assignment (Gaussian-like) to neighboring bins
                double dist = pitch - pitchClass;
                if (dist > 6.0) dist -= 12.0;
                if (dist < -6.0) dist += 12.0;
                
                double coreContribution = weight * Math.Exp(-0.5 * Math.Pow(dist / 0.1, 2));
                pcp[pitchClass] += coreContribution;
            }
        }

        double totalEnergy = pcp.Sum();
        if (totalEnergy > 0)
        {
            for (int i = 0; i < numBins; i++)
                pcp[i] /= totalEnergy;
        }

        return pcp;
    }

    private double DetectTuningOffset(float[] samples, int sampleRate)
    {
        const int fftSize = 16384;
        const int hopSize = 8192;
        const int binsPerSemitone = 5; // 20 cents per bin
        const int totalBins = 12 * binsPerSemitone;
        const double a4Freq = 440.0;

        var highResPcp = new double[totalBins];
        int numFrames = Math.Min(10, (samples.Length - fftSize) / hopSize + 1); // Only few frames for tuning speed
        var magnitudes = new double[fftSize / 2];

        for (int frame = 0; frame < numFrames; frame++)
        {
            var frameStart = (samples.Length / 2) - (numFrames * hopSize / 2) + (frame * hopSize);
            if (frameStart < 0 || frameStart + fftSize > samples.Length) continue;

            var window = samples.AsSpan(frameStart, fftSize);
            ComputeFFTMagnitudes(window, magnitudes, sampleRate);

            for (int i = 0; i < totalBins; i++)
            {
                double c0Freq = a4Freq * Math.Pow(2, (i / (double)binsPerSemitone - 9) / 12.0);
                
                for (int harmonic = 1; harmonic <= 4; harmonic++)
                {
                    double hFreq = c0Freq * harmonic;
                    int bin = (int)(hFreq * fftSize / sampleRate);
                    if (bin > 0 && bin < magnitudes.Length)
                        highResPcp[i] += magnitudes[bin] / harmonic;
                }
            }
        }

        var folded = new double[binsPerSemitone];
        for (int i = 0; i < totalBins; i++)
            folded[i % binsPerSemitone] += highResPcp[i];

        int bestOffsetBin = 0;
        double maxEnergy = 0;
        for (int i = 0; i < binsPerSemitone; i++)
        {
            if (folded[i] > maxEnergy)
            {
                maxEnergy = folded[i];
                bestOffsetBin = i;
            }
        }

        double offset = bestOffsetBin / (double)binsPerSemitone;
        if (offset > 0.5) offset -= 1.0;

        return offset;
    }

    private void ComputeFFTMagnitudes(Span<float> window, double[] magnitudes, int sampleRate)
    {
        int n = magnitudes.Length * 2;
        var complex = new System.Numerics.Complex[n];

        for (int i = 0; i < n; i++)
        {
            double hann = 0.5 * (1 - Math.Cos(2 * Math.PI * i / (n - 1)));
            complex[i] = new System.Numerics.Complex(i < window.Length ? window[i] * hann : 0, 0);
        }

        FFT(complex);

        for (int i = 0; i < magnitudes.Length; i++)
        {
            magnitudes[i] = Math.Sqrt(complex[i].Real * complex[i].Real + complex[i].Imaginary * complex[i].Imaginary);
        }
    }

    private void FFT(System.Numerics.Complex[] data) => FftHelper.FFT(data);

    private ((int keyIndex, int mode, double correlation) best, (int keyIndex, int mode, double correlation) alternative) FindBestKeys(double[] pcp)
    {
        var results = new List<(int keyIndex, int mode, double correlation)>();

        for (int root = 0; root < 12; root++)
        {
            var rotatedPcp = RotateArray(pcp, root);

            double majorCorr = ComputeCorrelation(rotatedPcp, MajorProfile);
            results.Add((root, 0, majorCorr));

            double minorCorr = ComputeCorrelation(rotatedPcp, MinorProfile);
            results.Add((root, 1, minorCorr));
        }

        var sorted = results.OrderByDescending(r => r.correlation).ToList();
        
        var best = sorted[0];
        var alternative = sorted[1];

        // Normalize correlation to 0-1 confidence
        best.correlation = Math.Min(1.0, Math.Max(0, (best.correlation + 1) / 2));
        alternative.correlation = Math.Min(1.0, Math.Max(0, (alternative.correlation + 1) / 2));

        return (best, alternative);
    }

    private double[] RotateArray(double[] arr, int shift)
    {
        var result = new double[arr.Length];
        for (int i = 0; i < arr.Length; i++)
            result[i] = arr[(i + shift) % arr.Length];
        return result;
    }

    private double ComputeCorrelation(double[] a, double[] b)
    {
        double sumAB = 0, sumA2 = 0, sumB2 = 0;
        for (int i = 0; i < a.Length; i++)
        {
            sumAB += a[i] * b[i];
            sumA2 += a[i] * a[i];
            sumB2 += b[i] * b[i];
        }

        double denominator = Math.Sqrt(sumA2) * Math.Sqrt(sumB2);
        return denominator > 0 ? sumAB / denominator : 0;
    }
}