using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using AudioAnalyzer.Interfaces;

namespace AudioAnalyzer.Services;

public class ToneGeneratorService : IToneGeneratorService
{
    private IWavePlayer? _waveOut;
    private SineWaveSampleProvider? _sineWaveProvider;
    private ScaleSequencerProvider? _sequencerProvider;
    private float _volume = 0.25f;

    private bool[] _scaleNotes = new bool[12];
    private int _currentScaleIndex = -1;

    public void Start(double frequency)
    {
        if (_waveOut != null) Stop();

        _sineWaveProvider = new SineWaveSampleProvider
        {
            Frequency = (float)frequency,
            Amplitude = _volume
        };

        _sequencerProvider = new ScaleSequencerProvider(_sineWaveProvider);
        
        _waveOut = new WaveOutEvent();
        _waveOut.Init(_sequencerProvider);
        _waveOut.Play();
    }

    public void StartScalePlayback(double bpm, bool[] scaleNotes, int tonicIndex)
    {
        _scaleNotes = scaleNotes;
        _currentScaleIndex = (tonicIndex - 1 + 12) % 12;

        // Build the frequency list for the full scale cycle
        var frequencies = BuildFrequencyList();

        if (_waveOut != null) Stop();

        _sineWaveProvider = new SineWaveSampleProvider
        {
            Frequency = (float)(frequencies.Length > 0 ? frequencies[0] : 440.0),
            Amplitude = _volume
        };

        _sequencerProvider = new ScaleSequencerProvider(_sineWaveProvider);
        _sequencerProvider.SetScale(frequencies, bpm);

        _waveOut = new WaveOutEvent();
        _waveOut.Init(_sequencerProvider);
        _waveOut.Play();
    }

    private double[] BuildFrequencyList()
    {
        var freqs = new System.Collections.Generic.List<double>();
        int startIdx = _currentScaleIndex;

        for (int i = 1; i <= 12; i++)
        {
            int idx = (startIdx + i) % 12;
            if (_scaleNotes[idx])
            {
                int midiNote = 60 + idx;
                double freq = 440.0 * Math.Pow(2.0, (midiNote - 69.0) / 12.0);
                freqs.Add(freq);
            }
        }

        return freqs.ToArray();
    }

    public void UpdateBpm(double bpm)
    {
        _sequencerProvider?.SetBpm(bpm);
    }

    public void StopScalePlayback()
    {
        Stop();
    }

    public void Trigger()
    {
        // No longer needed — sequencing is now handled internally by ScaleSequencerProvider
    }

    public void Stop()
    {
        _waveOut?.Stop();
        _waveOut?.Dispose();
        _waveOut = null;
    }

    public void SetFrequency(double frequency)
    {
        if (_sineWaveProvider != null)
        {
            _sineWaveProvider.Frequency = (float)frequency;
        }
    }

    public void SetVolume(float volume)
    {
        _volume = volume;
        if (_sineWaveProvider != null)
        {
            _sineWaveProvider.Amplitude = volume;
        }
    }

    public void Dispose()
    {
        Stop();
    }

    /// <summary>
    /// Generates a continuous sine wave. Phase is never reset externally.
    /// Frequency changes are applied smoothly by the sequencer at zero-envelope points.
    /// </summary>
    private class SineWaveSampleProvider : ISampleProvider
    {
        public WaveFormat WaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(44100, 1);
        public float Frequency { get; set; } = 440;
        public float Amplitude { get; set; } = 0.25f;
        private float _phase;

        public int Read(float[] buffer, int offset, int count)
        {
            for (int i = 0; i < count; i++)
            {
                buffer[offset + i] = (float)(Amplitude * Math.Sin(_phase));
                _phase += (float)(2 * Math.PI * Frequency / WaveFormat.SampleRate);
                if (_phase > 2 * Math.PI) _phase -= (float)(2 * Math.PI);
            }
            return count;
        }
    }

    /// <summary>
    /// Sample-accurate scale sequencer. Handles all note timing internally
    /// on the audio thread — no DispatcherTimer jitter.
    /// Cycle: Attack → Sustain → Release → (silence gap) → next note...
    /// </summary>
    private class ScaleSequencerProvider : ISampleProvider
    {
        private readonly SineWaveSampleProvider _source;

        private double[] _frequencies = Array.Empty<double>();
        private int _noteIndex;

        private int _sampleCount;
        private int _noteSamples;     // attack + sustain + release
        private int _beatSamples;     // full beat duration (includes silence gap)

        private const int FadeSamples = 441; // 10ms at 44.1kHz for both attack and release
        private bool _isPlaying;

        public WaveFormat WaveFormat => _source.WaveFormat;

        public ScaleSequencerProvider(SineWaveSampleProvider source)
        {
            _source = source;
            _beatSamples = 44100;
            _noteSamples = (int)(_beatSamples * 0.85);
        }

        public void SetScale(double[] frequencies, double bpm)
        {
            _frequencies = frequencies;
            _noteIndex = 0;
            _sampleCount = 0;
            _isPlaying = frequencies.Length > 0;
            SetBpm(bpm);

            if (_isPlaying)
            {
                _source.Frequency = (float)_frequencies[0];
            }
        }

        public void SetBpm(double bpm)
        {
            if (bpm <= 0) bpm = 120;
            _beatSamples = (int)((60.0 / bpm) * WaveFormat.SampleRate);
            // Note occupies 85% of the beat; remaining 15% is silence gap
            _noteSamples = (int)(_beatSamples * 0.85);
            // Ensure note is long enough for attack + release
            if (_noteSamples < FadeSamples * 3)
                _noteSamples = FadeSamples * 3;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = _source.Read(buffer, offset, count);

            if (!_isPlaying || _frequencies.Length == 0)
            {
                Array.Clear(buffer, offset, samplesRead);
                return samplesRead;
            }

            for (int i = 0; i < samplesRead; i++)
            {
                float envelope;

                if (_sampleCount < FadeSamples)
                {
                    // Cosine attack: smooth 0 → 1
                    double x = (double)_sampleCount / FadeSamples;
                    envelope = (float)(0.5 * (1.0 - Math.Cos(Math.PI * x)));
                }
                else if (_sampleCount < _noteSamples - FadeSamples)
                {
                    // Sustain
                    envelope = 1.0f;
                }
                else if (_sampleCount < _noteSamples)
                {
                    // Cosine release: smooth 1 → 0
                    int releasePos = _sampleCount - (_noteSamples - FadeSamples);
                    double x = (double)releasePos / FadeSamples;
                    envelope = (float)(0.5 * (1.0 + Math.Cos(Math.PI * x)));
                }
                else
                {
                    // Silence gap between notes
                    envelope = 0;
                }

                buffer[offset + i] *= envelope;
                _sampleCount++;

                // End of beat: advance to next note
                if (_sampleCount >= _beatSamples)
                {
                    _sampleCount = 0;
                    _noteIndex = (_noteIndex + 1) % _frequencies.Length;
                    // Change frequency during the silence gap (envelope = 0)
                    _source.Frequency = (float)_frequencies[_noteIndex];
                }
            }

            return samplesRead;
        }
    }
}
