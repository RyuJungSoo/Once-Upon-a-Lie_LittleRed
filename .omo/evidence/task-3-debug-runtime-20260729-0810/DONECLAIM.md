# DoneClaim - Stage1 camera drift runtime debug

## Scope
- Product/scene/test files intentionally not fixed.
- B/C/D toggles intentionally not run after H3 evidence because disk restore and editor readiness were not stable enough to broaden safely.

## Verified Scenarios
- Baseline disk snapshot
  - Invocation: `Get-FileHash Assets/Scenes/Stage1_Scene.unity -Algorithm SHA256`; `git diff -- Assets/Scenes/Stage1_Scene.unity`
  - Observable: SHA256 `877300EA1C9FB71AE37C9025FA315B564D8C6F8A492E3432503835E66DBC310A`
  - Artifact: `00-disk-hash.txt`, `00-stage1-diff.patch`
- Baseline live Unity snapshot
  - Invocation: Unity MCP `execute_code` read-only dump to `01-baseline-live.txt`
  - Observable: `scene.isDirty=True`, `scene.rootCount=18`, root `CinemachineCamera`, `Main Camera` component `Unity.Cinemachine.CinemachineBrain`, `Camera.orthographicSize=6.3`
  - Artifact: `01-baseline-live.txt`
- Toggle A first run
  - Invocation: Unity MCP `execute_code` with `EditorSceneManager.OpenScene("Assets/Scenes/Stage1_Scene.unity", OpenSceneMode.Single)`
  - Observable: after open `dirty=False`, `rootCount=18`; disk SHA changed from `877300EA...` to `0064238EEAD8387C6A621AC744508F206A08D479136AEFCCE15935B4DCEDF891`
  - Artifact: `02-toggleA-reload-idle-live.txt`, `02-toggleA-disk-hash.txt`, `02-toggleA-stage1-diff-after.patch`
- Disk restore procedure
  - Invocation: reconstruct `HEAD + 00-stage1-diff.patch`, normalize LF, copy to Stage1
  - Observable: restored SHA256 `877300EA1C9FB71AE37C9025FA315B564D8C6F8A492E3432503835E66DBC310A`
  - Artifact: `02-toggleA-restored-disk-hash.txt`, `03-h3-restored-sha-recheck.txt`
- H3 stale/readiness capture
  - Invocation: `refresh_unity(mode=if_dirty, scope=assets, compile=none, wait_for_ready=true)`, editor_state reads, Unity MCP `execute_code` readiness dump
  - Observable: editor_state reported `external_changes_dirty=true` after disk restore, then `external_changes_dirty=false` but `ready_for_tools=false`, `blocking_reasons=["stale_status"]`; live readiness found `application.isPlaying=True`, later after stop `application.isPlaying=False` with `scene.isDirty=True`
  - Artifact: `.debug-journal.md`, `03-h3-pre-repeatA-live-readiness.txt`, `04-h3-ready-repeatA-live-readiness.txt`
- Toggle A repeat
  - Invocation: Unity MCP `execute_code` with `EditorSceneManager.OpenScene("Assets/Scenes/Stage1_Scene.unity", OpenSceneMode.Single)`
  - Observable: before `scene.isDirty=True`, `rootCount=18`, play false; after `scene.isDirty=False`, `rootCount=18`; disk SHA remained `877300EA1C9FB71AE37C9025FA315B564D8C6F8A492E3432503835E66DBC310A`
  - Artifact: `05-h3-repeatA-openScene-single.txt`, `05-h3-repeatA-after-sha.txt`, `05-h3-repeatA-after-diff.patch`
- Delayed post-repeat serialization
  - Invocation: post-repeat SHA recheck only; no second repeat A
  - Observable: SHA changed to `1CFA07E3C33BB1C7D4B4E7A1CC76CA910311DF5CCCCB244831123E1836E5051E`; delayed diff added `MentalCameraShake` to `CinemachineCamera` plus Cinemachine/Brain/camera-size serialization; Stage1 was restored again to `877300EA1C9FB71AE37C9025FA315B564D8C6F8A492E3432503835E66DBC310A`
  - Artifact: `07-post-repeatA-final-sha.txt`, `07-post-repeatA-delayed-drift-diff.patch`, `08-final-restored-sha.txt`

## Verdict Matrix
- H1 ExperienceProgressSceneTests setup/restore creates drift: Not confirmed in this pass. Test toggle B was intentionally not run because H3/file-watcher state was already unsafe and could contaminate the scene.
- H2 Play Mode / manage_camera / Cinemachine tool path creates drift: Partially implicated for Play Mode state only, not confirmed as root. Live state unexpectedly had `application.isPlaying=True` before repeat A, but no screenshot path was run and no manage_camera mutation was tested.
- H3 external disk reload/file-watcher timing leaves stale in-memory scene that can reserialize drift: Confirmed root-cause candidate. Confirmed evidence: after disk restore, Unity reported external/stale state; first reload changed disk without explicit save; Play Mode transition/stale_status then persisted until stop/retry; repeat A initially preserved SHA, then a delayed post-repeat SHA check showed Unity had serialized additional stale live state into Stage1 without an explicit save.
- H4 disk baseline already contains observed Cinemachine drift: Confirmed for current working tree. Before any new debug action, disk diff already contained `CinemachineCamera`, `CinemachineBrain`, `orthographic size: 5 -> 6.3`, and live baseline loaded the same drift.

## Root Cause Candidate
The minimal root cause is stale Unity in-memory scene state around external Stage1 disk changes and Play Mode/editor refresh transitions. The dangerous toggle is: dirty/stale live Stage1 + external disk restore/reload + Unity refresh/play transition. That can cause Unity to normalize/reserialize Cinemachine-derived scene data without an explicit save call, sometimes delayed after the apparent reload result.

## Next Minimal Fix Recommendation
Add an automation guard before any Stage1 scene test/screenshot/reload path: block if `Application.isPlaying`, `EditorApplication.isCompiling`, `EditorApplication.isUpdating`, `scene.isDirty`, `external_changes_dirty`, or editor_state `stale_status` is present. Require a clean reload/discard flow and verify the Stage1 SHA before and after the operation. Do not save Stage1 from automation when drift is present.

## Final State
- Stage1 disk SHA restored and verified after delayed drift: `877300EA1C9FB71AE37C9025FA315B564D8C6F8A492E3432503835E66DBC310A`
- Unity play mode final resource value: `is_playing=false`, `is_changing=false`
- Unity editor_state resource still reports `stale_status`; user/manual Reload may still be needed before broader B/C/D testing.
