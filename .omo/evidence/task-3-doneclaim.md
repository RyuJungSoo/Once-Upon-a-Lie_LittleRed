DoneClaim: Todo 3 Stage1 XP Filler scene/test, verifier evidence follow-up

Changed files:
- Assets/Scenes/Stage1_Scene.unity
- Assets/Tests/EditMode/ExperienceProgressSceneTests.cs
- Assets/Tests/EditMode/ExperienceProgressSceneTests.cs.meta
- .omo/evidence/task-3-*

Product/scene/test status:
- No product/test code changes were made during this evidence-only follow-up.
- A temporary Unity in-memory camera/Cinemachine drift was accidentally serialized during release-state capture; it was removed and Stage1 diff was restored to the original Todo 3 final diff.
- `task-3-final-stage1-diff.patch`, `task-3-post-cleanup-stage1-diff.patch`, and `task-3-post-release-stage1-diff.patch` now match by SHA256.

Baseline / failing-first / final tests:
- Baseline live Filler: `.omo/evidence/task-3-live-baseline-filler.txt`
- Failing-first targeted EditMode job `431500eb38a44567a70e16b0c8e9c9cf`: failed for expected green R value vs current cyan R value.
- Final targeted EditMode jobs `a67a8bb118e34ea7a7d4f1262018a07f` and `e30c3bf65b14403f8edc3f7722bad196`: both passed, total=1 passed=1 failed=0.

Verifier evidence follow-up:
- Classified the two `Saving results to ... TestResults.xml` console entries as Unity Test Runner result-save infrastructure logs, not compile/runtime exceptions: `.omo/evidence/task-3-testresults-log-classification.md`
- Cleared console and captured release state with Error/Warning/Exception/other log entries all 0, editor idle, ready, stale=false, sceneDirty=false: `.omo/evidence/task-3-post-clear-console-and-release-state.md`
- Added acceptance/QA matrix mapping exact Filler values, jobs, screenshot, camera preservation, source prefab unchanged, and cleanup: `.omo/evidence/task-3-acceptance-qa-matrix.md`

Manual QA:
- Play Mode runtime `fillAmount=0.5` screenshot: `.omo/evidence/task-3-playmode-filler-half.png`
- Runtime state and cleanup: `.omo/evidence/task-3-playmode-runtime-filler-half.txt`, `.omo/evidence/task-3-after-playmode-stop-scene-state.txt`

Source prefab / cleanup:
- Source prefab diff remains empty: `.omo/evidence/task-3-source-prefab-diff.patch`
- Final status is recorded at `.omo/evidence/task-3-updated-final-git-status.txt`
- Unity is released: playMode=false, sceneDirty=false, console clear, editor ready.
