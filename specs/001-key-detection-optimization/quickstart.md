# Quickstart: Key Detection Verification

## Test Scenarios

### 1. Standard A440 Test
- **Input**: `C_Major_440Hz.wav`
- **Expected**: Key="C Major", Offset=0, Confidence > 0.9.

### 2. Detuned 432Hz Test
- **Input**: `C_Major_432Hz.wav`
- **Expected**: Key="C Major", Offset=-32 cents, Confidence > 0.8.

### 3. Stress Test (Max Flat)
- **Input**: `C_Major_Detuned_Minus_49c.wav`
- **Expected**: Key="C Major", Offset=-49 cents.

### 4. Tone Generator Pulse
- **Action**: Enable "Play Scale" on a 120 BPM track.
- **Verification**: Auditory check of pulsing tone at 120 BPM.

## Automation
Run `AudioAnalyzer.PerfTest` to generate batch results for tuning accuracy.
