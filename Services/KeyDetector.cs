using AudioAnalyzer.Interfaces;

namespace AudioAnalyzer.Services;

public class KeyDetector : IKeyDetector
{
    // Krumhansl-Schmuckler Profiles (best for classical & acoustic)
    private static readonly double[] MajorProfileKS = { 6.35, 2.23, 3.48, 2.33, 4.38, 4.09, 2.52, 5.19, 2.39, 3.66, 2.29, 2.88 };
    private static readonly double[] MinorProfileKS = { 6.33, 2.68, 3.52, 5.38, 2.60, 3.53, 2.54, 4.75, 3.98, 2.69, 3.34, 3.17 };
    
    // Temperley Profiles (best for pop, rock & electronic)
    private static readonly double[] MajorProfileTemperley = { 5.0, 2.0, 3.5, 2.0, 4.5, 4.0, 2.0, 4.5, 2.0, 3.5, 1.5, 4.0 };
    private static readonly double[] MinorProfileTemperley = { 5.0, 2.0, 3.5, 4.5, 2.0, 4.0, 2.0, 4.5, 3.5, 2.0, 1.5, 4.0 };

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

            // Limit to MaxAnalysisSeconds from center of audio for tuning offset estimation
            int tuningSamples = MaxAnalysisSeconds * sampleRate;
            float[] tuningData;
            if (monoSamples.Length > tuningSamples)
            {
                int startOffset = (monoSamples.Length - tuningSamples) / 2;
                tuningData = monoSamples.AsSpan(startOffset, tuningSamples).ToArray();
            }
            else
            {
                tuningData = monoSamples;
            }

            var tuningOffset = DetectTuningOffset(tuningData, sampleRate);
            LoggerService.Log($"KeyDetector.DetectKeyFromSamples - Detected tuning offset: {tuningOffset * 100:F1} cents");

            progress?.Report(30);

            var pcp = new double[12];
            int segmentLength = 6 * sampleRate;

            // If audio is long enough, perform Multi-Segment Analysis (5 distributed segments of 6s)
            // to avoid intro/outro noise and capture global harmonic structures.
            if (monoSamples.Length > segmentLength * 2)
            {
                double[] ratios = { 0.20, 0.35, 0.50, 0.65, 0.80 };
                for (int s = 0; s < ratios.Length; s++)
                {
                    double ratio = ratios[s];
                    int centerOffset = (int)(monoSamples.Length * ratio);
                    int startOffset = centerOffset - (segmentLength / 2);
                    startOffset = Math.Max(0, Math.Min(startOffset, monoSamples.Length - segmentLength));
                    
                    var segmentSamples = monoSamples.AsSpan(startOffset, segmentLength).ToArray();
                    var segmentPcp = ComputePitchClassProfile(segmentSamples, sampleRate, tuningOffset);
                    
                    for (int i = 0; i < 12; i++)
                    {
                        pcp[i] += segmentPcp[i];
                    }
                    progress?.Report(30 + (s + 1) * 10);
                }

                // Normalize combined PCP
                double total = pcp.Sum();
                if (total > 0)
                {
                    for (int i = 0; i < 12; i++) pcp[i] /= total;
                }
            }
            else
            {
                pcp = ComputePitchClassProfile(monoSamples, sampleRate, tuningOffset);
                progress?.Report(80);
            }

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
                
                // --- SMOOTH BASS ROLL-OFF ---
                // Continuous high-pass filter using a sigmoidal attenuation curve centered at 180Hz.
                // Attenuates sub-bass (bombs/kicks) while keeping fundamental musical notes.
                double rollOff = 1.0 / (1.0 + Math.Exp(-(freq - 180.0) / 30.0));
                weight *= rollOff;
                
                // Soft assignment (Gaussian-like) to neighboring bins
                double dist = pitch - pitchClass;
                if (dist > 6.0) dist -= 12.0;
                if (dist < -6.0) dist += 12.0;
                
                double coreContribution = weight * Math.Exp(-0.5 * Math.Pow(dist / 0.1, 2));
                pcp[pitchClass] += coreContribution;
            }
        }

        // --- HARMONIC DEMIXING / DECOUPLING ---
        // Attenuate fifth (3rd harmonic) and third (5th harmonic) ghost leaks.
        var demixedPcp = new double[numBins];
        for (int i = 0; i < numBins; i++)
        {
            double val = pcp[i];
            demixedPcp[i] += val;
            
            // Subtract 20% of energy from perfect fifth (3rd harmonic)
            demixedPcp[(i + 7) % numBins] -= val * 0.20;
            
            // Subtract 10% of energy from major third (5th harmonic)
            demixedPcp[(i + 4) % numBins] -= val * 0.10;
        }

        for (int i = 0; i < numBins; i++)
        {
            pcp[i] = Math.Max(0.0, demixedPcp[i]);
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

            // Correlation against Krumhansl-Schmuckler
            double majorCorrKS = ComputeCorrelation(rotatedPcp, MajorProfileKS);
            double minorCorrKS = ComputeCorrelation(rotatedPcp, MinorProfileKS);

            // Correlation against Temperley
            double majorCorrTemp = ComputeCorrelation(rotatedPcp, MajorProfileTemperley);
            double minorCorrTemp = ComputeCorrelation(rotatedPcp, MinorProfileTemperley);

            // Combined Consensus
            double combinedMajor = (majorCorrKS + majorCorrTemp) / 2.0;
            double combinedMinor = (minorCorrKS + minorCorrTemp) / 2.0;

            results.Add((root, 0, combinedMajor));
            results.Add((root, 1, combinedMinor));
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