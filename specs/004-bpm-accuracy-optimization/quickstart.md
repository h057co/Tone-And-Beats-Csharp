# Quickstart: Testing BPM Accuracy Optimization

## Prerequisites
- The `audiotest` folder must be present in `O:\Desarrollos\audiotest`.
- Essentia binaries must be provisioned (handled by the new `BinaryProvisioningService`).

## Running the Accuracy Audit
1. Open the solution in Visual Studio.
2. Set `AudioAnalyzer.PerfTest` as the Startup Project.
3. Run the project (F5).
4. The console will output a comparison table:
   - **Filename BPM**: Expected value.
   - **Detected BPM**: Current engine output.
   - **Is Harmonic**: 1.5x, 0.66x, or 2.0x detection.
   - **Status**: ✅ (Pass) or ❌ (Fail).

## Validating Urban Strategy
1. Select a Trap track (e.g., `audio5_140.mp3`).
2. Verify that the UI displays **70 BPM** as the Primary result.
3. Check that the `IsReinterpreted` flag is active in the diagnostic logs.
4. Use the "Swap" toggle in the UI to see the technical 140 BPM candidate.

## Verifying the Beat Grid
1. Load any track.
2. Check the debug output for the full list of `BeatTimesSeconds`.
3. Verify that the count of beats aligns with `(Duration / 60) * BPM`.
