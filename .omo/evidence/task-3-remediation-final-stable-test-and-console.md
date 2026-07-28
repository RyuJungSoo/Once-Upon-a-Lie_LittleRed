Todo 3 remediation final stable verification

Successful read-only YAML test invocation:
- Unity MCP run_tests EditMode ExperienceProgressSceneTests.Stage1ExperienceGaugeSerializedYamlMatchesContract
- Job: f2f162e12864427da97df9d4d33f05a4
- Result: succeeded
- Summary: total=1, passed=1, failed=0, skipped=0, durationSeconds=0.073422

Stage1 file immutability around this final stable test:
- See `.omo/evidence/task-3-remediation-final-stable-stage1-sha-before-after.txt`
- final_stable_before_test_stage1_sha256 == final_stable_after_test_stage1_sha256

Console check after final stable run:
- Unity MCP read_console types=[error] count=50 format=detailed include_stacktrace=true
- Retrieved two entries with type=Exception and message="Saving results to: C:/Users/User/AppData/LocalLow/DefaultCompany/Once Upon a Lie - Little Red\TestResults.xml".
- Both entries have file="C:\build\output\unity\unity\Runtime\Export\Debug\Debug.bindings.h", line=40, stackTrace=null.
- Classification: Unity Test Runner result-save infrastructure logs, not compile/runtime exceptions from project code.

Unity restrictions observed:
- No scene load/reload/save calls in this remediation.
- No Play Mode enter/stop calls in this remediation.
- No manage_camera calls in this remediation.
- No Stage1_Scene.unity writes by this remediation.
