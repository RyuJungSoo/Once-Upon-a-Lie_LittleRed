Unity Test Runner result-save log classification

Source console entries observed after final targeted EditMode runs:
1. type=Exception
   message=Saving results to: C:/Users/User/AppData/LocalLow/DefaultCompany/Once Upon a Lie - Little Red\TestResults.xml
   file=C:\build\output\unity\unity\Runtime\Export\Debug\Debug.bindings.h
   line=40
   stackTrace=null
2. type=Exception
   message=Saving results to: C:/Users/User/AppData/LocalLow/DefaultCompany/Once Upon a Lie - Little Red\TestResults.xml
   file=C:\build\output\unity\unity\Runtime\Export\Debug\Debug.bindings.h
   line=40
   stackTrace=null

Context:
- The two entries appeared immediately after the two final targeted EditMode test jobs.
- Run 1 job a67a8bb118e34ea7a7d4f1262018a07f succeeded: total=1, passed=1, failed=0.
- Run 2 job e30c3bf65b14403f8edc3f7722bad196 succeeded: total=1, passed=1, failed=0.
- Test result file exists at C:/Users/User/AppData/LocalLow/DefaultCompany/Once Upon a Lie - Little Red/TestResults.xml and is non-empty.
- stackTrace=null and the message is a result-save path announcement, not a thrown compile/runtime exception from project code.

Classification:
- Unity Test Runner result-save infrastructure log.
- Not a C# compile error.
- Not a runtime exception from Stage1 gameplay, UIManager, or ExperienceProgressSceneTests.
- Not a blocker for Todo 3 acceptance.
