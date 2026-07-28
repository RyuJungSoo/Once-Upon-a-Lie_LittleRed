Todo 3 remediation DoneClaim

Scope actually changed:
- Assets/Tests/EditMode/ExperienceProgressSceneTests.cs only.
- .omo/evidence/task-3-remediation-* evidence files.

Test remediation:
- Removed EditorSceneManager.OpenScene / RestoreSceneManagerSetup path.
- Removed Unity scene/object API assertions from the test.
- New test reads Stage1_Scene.unity and Bar_1.prefab YAML through File.ReadAllText only.
- Contract verified by YAML parsing:
  - UIManager `experienceGauge` serialized fileID is nonzero.
  - UIManager `levelText` serialized fileID is nonzero.
  - Stage1 prefab override target uses guid `ad84c37a896f54e1180d600bd7746b95` and source fileID `114361196886809056`.
  - Stage1 Filler overrides include `m_Color.r/g/b/a` matching #8EC54A alpha 1 within 0.001.
  - Stage1 Filler override includes `m_FillAmount` 0.
  - Source prefab Filler block has `m_Type: 3`, `m_FillMethod: 0`, `m_FillOrigin: 0`.

Validation:
- Job `00742b2976904b1c8492af2c84cc98b2`: passed, total=1 passed=1 failed=0.
- Job `fb524d342b91448c8f24f5e4df8c75c5`: passed, total=1 passed=1 failed=0.
- Job `f2f162e12864427da97df9d4d33f05a4`: passed, total=1 passed=1 failed=0.
- Console after successful runs showed only Unity Test Runner `Saving results to ... TestResults.xml` result-save infrastructure entries with stackTrace=null.

Stage1 immutability evidence:
- I did not call scene load/reload/save, Play Mode enter/stop, manage_camera, or scene mutation APIs during remediation.
- I did not edit Stage1_Scene.unity with apply_patch or any shell write.
- Stage1 SHA could not be proven stable across Unity test execution because the editor/workspace continued writing user-owned scene changes during/around test runs.
- Repeated SHA attempts are recorded in:
  - `.omo/evidence/task-3-remediation-stage1-sha-before-after.txt`
  - `.omo/evidence/task-3-remediation-stable-stage1-sha-before-after.txt`
  - `.omo/evidence/task-3-remediation-final-stable-stage1-sha-before-after.txt`
- The observed Stage1 diff contains user-owned camera/Cinemachine/MentalCameraShake/Bird/audio changes; per instruction I did not modify, delete, or restore them.

Residual blocker:
- Acceptance item "test before/after Stage1 SHA identical" remains blocked by concurrent/user-owned Unity scene writes outside my permitted scope. Satisfying it would require a quiet editor window or authorization to coordinate/pause external scene writers, not changes to Todo3 code.
