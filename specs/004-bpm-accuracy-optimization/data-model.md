# Data Model: BPM Analysis Result

## Entities

### BpmAnalysisResult
Stores the full rhythmic profile of an audio file.

| Field | Type | Description |
|-------|------|-------------|
| `PrimaryBpm` | `double` | The finalized musical tempo (after Urban Strategy). |
| `AlternateBpms` | `List<double>` | Harmonic candidates (e.g., 0.5x, 1.5x, 2.0x). |
| `Confidence` | `double` | Score (0.0 - 1.0) of the primary detection. |
| `BeatTimesSeconds` | `List<double>` | Absolute timestamps (seconds) of every detected beat. |
| `BeatIntervals` | `List<double>` | Time delta between consecutive beats (to detect drift). |
| `IsReinterpreted` | `bool` | True if the final BPM was shifted by the Urban Strategy. |
| `ReinterpretationType` | `string` | "HalfTime", "DoubleTime", or "None". |
| `EngineVersion` | `string` | Identifier for the engine that produced the result (e.g., "Essentia-2013"). |
| `AnalysisTimestamp` | `DateTime` | When the analysis was performed. |

## Relationships
- One **AudioFile** has one **BpmAnalysisResult**.
- **BpmAnalysisResult** is used by the **BeatGridRenderer** to draw indicators on the waveform.

## Validation Rules
- `PrimaryBpm` must be > 0.
- `Confidence` must be between 0.0 and 1.0.
- `BeatTimesSeconds` must be strictly increasing.
