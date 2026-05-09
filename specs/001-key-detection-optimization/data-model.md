# Data Model: Key Detection Optimization

## Entities

### TuningOffset
- **Value**: `double` (cents)
- **Range**: -50 to +50
- **Validation**: If > 50, wrap to next semitone.

### PitchClassProfile (PCP)
- **EnergyVector**: `double[12]`
- **Properties**: Normalized sum to 1.0.

### MusicalKey
- **Root**: `string` (e.g., "C", "C#")
- **Mode**: `string` ("Major" or "Minor")
- **Confidence**: `double` (0.0 to 1.0)

### KeyDetectionReport
- **PrimaryResult**: `MusicalKey`
- **AlternativeResult**: `MusicalKey`
- **TuningCentOffset**: `double`

## Relationships
- `KeyDetector` computes `PitchClassProfile`.
- `PitchClassProfile` is correlated with `MusicalKey` profiles.
- `MainViewModel` holds the `KeyDetectionReport`.
