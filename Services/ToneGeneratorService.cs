using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using AudioAnalyzer.Interfaces;

namespace AudioAnalyzer.Services;

public class ToneGeneratorService : IToneGeneratorService
{
    private IWavePlayer? _waveOut;
    private SineWaveSampleProvider? _sineWaveProvider;
    private GatedSampleProvider? _gatedProvider;
    private float _volume = 0.25f;

    private bool _isScalePlaying;
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

        _gatedProvider = new GatedSampleProvider(_sineWaveProvider);
        
        _waveOut = new WaveOutEvent();
        _waveOut.Init(_gatedProvider);
        _waveOut.Play();
    }

    public void StartScalePlayback(double bpm, bool[] scaleNotes)
    {
        _scaleNotes = scaleNotes;
        _isScalePlaying = true;
        _currentScaleIndex = -1;
        
        _gatedProvider?.SetDuration(bpm);
        
        // Find first note to start
        MoveToNextScaleNote();
        
        if (_sineWaveProvider != null)
        {
            Start(_sineWaveProvider.Frequency);
        }
        else
        {
            // Default frequency if not set
            Start(440.0);
        }
    }

    public void UpdateBpm(double bpm)
    {
        _gatedProvider?.SetDuration(bpm);
    }

    public void StopScalePlayback()
    {
        _isScalePlaying = false;
        Stop();
    }

    public void Trigger()
    {
        if (_isScalePlaying)
        {
            MoveToNextScaleNote();
        }
        _gatedProvider?.Trigger();
    }

    private void MoveToNextScaleNote()
    {
        if (_scaleNotes == null || _scaleNotes.Length == 0) return;

        // Find next 'true' in _scaleNotes
        for (int i = 1; i <= 12; i++)
        {
            int nextIdx = (_currentScaleIndex + i) % 12;
            if (_scaleNotes[nextIdx])
            {
                _currentScaleIndex = nextIdx;
                // MIDI to Frequency: f = 440 * 2^((n-69)/12)
                // C4 is MIDI 60. _currentScaleIndex is 0-11
                int midiNote = 60 + _currentScaleIndex;
                double freq = 440.0 * Math.Pow(2.0, (midiNote - 69.0) / 12.0);
                SetFrequency(freq);
                break;
            }
        }
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

    private class GatedSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider _source;
        private bool _isTriggered;
        private int _sampleCount;
        private int _totalSamples;
        
        private const int AttackSamples = 441; // 10ms at 44.1kHz
        private const int ReleaseSamples = 882; // 20ms at 44.1kHz

        public WaveFormat WaveFormat => _source.WaveFormat;

        public GatedSampleProvider(ISampleProvider source)
        {
            _source = source;
            _totalSamples = 44100; // Default 1 second
        }

        public void SetDuration(double bpm)
        {
            if (bpm <= 0) bpm = 120;
            _totalSamples = (int)((60.0 / bpm) * WaveFormat.SampleRate);
        }

        public void Trigger()
        {
            _isTriggered = true;
            _sampleCount = 0;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = _source.Read(buffer, offset, count);

            for (int i = 0; i < samplesRead; i++)
            {
                if (_isTriggered)
                {
                    float envelope = 0;
                    
                    if (_sampleCount < AttackSamples)
                    {
                        // Linear Attack
                        envelope = (float)_sampleCount / AttackSamples;
                    }
                    else if (_sampleCount < _totalSamples - ReleaseSamples)
                    {
                        // Sustain
                        envelope = 1.0f;
                    }
                    else if (_sampleCount < _totalSamples)
                    {
                        // Linear Release
                        int releasePos = _sampleCount - (_totalSamples - ReleaseSamples);
                        envelope = 1.0f - ((float)releasePos / ReleaseSamples);
                    }
                    else
                    {
                        // End of note
                        envelope = 0;
                        _isTriggered = false;
                    }

                    buffer[offset + i] *= envelope;
                    _sampleCount++;
                }
                else
                {
                    buffer[offset + i] = 0;
                }
            }

            return samplesRead;
        }
    }
}
