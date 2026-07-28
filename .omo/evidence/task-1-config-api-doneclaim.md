# DoneClaim: Todo 1 Config API Quick Fix

## Change
- Added `public ExperienceProgressPresentation()`.
- Added `public ExperienceProgressPresentation(float totalFillDuration, float minSegmentDuration, float fullHoldDuration)`.
- Added `public void Configure(float totalFillDuration, float minSegmentDuration, float fullHoldDuration)`.

## Behavior
- Existing parameterless construction and default timings remain `0.35f`, `0.08f`, `0.10f`.
- Invalid timing inputs (`<= 0`, `NaN`, `Infinity`) fall back to defaults.
- `Advance` now uses configured fill, minimum segment, and full-hold durations.

## Verification
- Per request, Unity/tests/scene operations were skipped.
- Stage1, camera, Cinemachine, MentalCameraShake, and UIManager were not edited.
