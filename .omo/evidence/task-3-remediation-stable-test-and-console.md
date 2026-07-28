Todo 3 remediation stable verification

Successful read-only YAML test invocation:
- Unity MCP run_tests EditMode ExperienceProgressSceneTests.Stage1ExperienceGaugeSerializedYamlMatchesContract
- Job: fb524d342b91448c8f24f5e4df8c75c5
- Result: succeeded
- Summary: total=1, passed=1, failed=0, skipped=0, durationSeconds=0.0791157

Stage1 file immutability around this test:
- See `.omo/evidence/task-3-remediation-stable-stage1-sha-before-after.txt`
- stable_before_test_stage1_sha256 == stable_after_test_stage1_sha256

Console check after successful stable run:
- Unity MCP read_console types=[error] count=50 format=detailed include_stacktrace=true
- Retrieved one entry with type=Exception, message="Saving results to: C:/Users/User/AppData/LocalLow/DefaultCompany/Once Upon a Lie - Little Red\TestResults.xml", file="C:\build\output\unity\unity\Runtime\Export\Debug\Debug.bindings.h", line=40, stackTrace=null.
- Classification: Unity Test Runner result-save infrastructure log, not a compile/runtime exception from project code.

Unity restrictions observed:
- Did not call scene load/reload/save APIs.
- Did not enter/stop Play Mode.
- Did not call manage_camera.
- Did not write Stage1_Scene.unity.
