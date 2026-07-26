# Monster System Efficiency Refactor

## TL;DR
> Summary:      紐ъ뒪???명삎 ?곹깭 媛깆떊??以묐났 寃쎈줈瑜?以꾩씠?? 湲곗〈 異붿쟻/怨듦꺽/荑⑤떎??Animator 怨꾩빟? ?좎??섎뒗 ?깅뒫 由ы뙥??怨꾪쉷?댁빞. ?듭떖? `MonsterSanityAppearance`???숈씪 ?곹깭 媛?쒕? ?먭퀬, `MonsterChase`媛 ?대룞 ?곹깭瑜?吏곸젒 ?꾨떖?섎ŉ, `MonsterAttack`??怨듦꺽 醫낅즺 ???ㅼ젣 ?대룞 ?곹깭濡?蹂듦??섍쾶 留뚮뱶??嫄곗빞.
> Deliverables:
> - `MonsterSanityAppearance` 紐⑥뀡 ?곹깭 以묐났 ?곌린 諛⑹?? Attack ?곗꽑?쒖쐞 蹂댁〈
> - `MonsterChase` 湲곕컲 ?대룞 ?곹깭 ?꾨떖濡?transform delta 湲곕컲 以묐났 媛먯? ?쒓굅
> - `MonsterAttack` 怨듦꺽 醫낅즺 ??Run/Idle ?ㅼ젣 ?곹깭 蹂듦?
> - EditMode ?뚭? ?뚯뒪??異붽? 諛?湲곗〈 17媛??뚯뒪??蹂댁〈
> - Unity 6000.3.8f1 而댄뙆??肄섏넄/?뚮젅???꾨줈?뚯씪 利앷굅
> Effort:       Medium
> Risk:         Medium - ??而댄룷?뚰듃媛 媛숈? Animator ?곹깭瑜?怨듭쑀?섍퀬, ?꾩옱 ?묒뾽?몃━??愿???뚯씪 蹂寃쎌씠 ?대? ?욎뿬 ?덉뼱.

## Scope
### Must have
- `Assets/Scripts/Monster/MonsterSanityAppearance.cs`??`SetMotionState`/`SetMoving`/`ApplyMotionState` 寃쎈줈?먯꽌 媛숈? `MonsterMotionState`媛 ?곗냽 ?낅젰????`Animator.SetInteger("MotionState", ...)`瑜?諛섎났 ?몄텧?섏? ?딄쾶 ??
- `Assets/Scripts/Monster/MonsterChase.cs`媛 `FixedUpdate()`?먯꽌 ?대? 怨꾩궛???대룞/?뺤? 寃곌낵瑜?`MonsterSanityAppearance`濡??꾨떖?섍쾶 ??
- `MonsterSanityAppearance.LateUpdate()`??transform delta ?먮룞 ?대룞 媛먯???`MonsterChase`媛 ?녿뒗 ?명삎 而댄룷?뚰듃??fallback?쇰줈留??④꺼.
- `Assets/Scripts/Monster/MonsterAttack.cs`??怨듦꺽 ?좊땲硫붿씠??醫낅즺 ??臾댁“嫄?`Run`???꾨땲?? 留덉?留됱쑝濡?蹂닿퀬???ㅼ젣 ?대룞 ?곹깭???곕씪 `Run` ?먮뒗 `Idle`濡?蹂듦??섍쾶 ??
- `OnCollisionStay2D`???뚮젅?댁뼱 ?쒓렇, 寃뚯엫 吏꾪뻾 ?щ?, 荑⑤떎?? ?щ쭩, ?뺤떊???뚯쭊 議곌굔怨?`nextAttackTime = Time.time + attackCooldown` ?쒖꽌???좎???
- 湲곗〈 怨듦컻 API? 吏곷젹???꾨뱶???좎??? `SetMotionState`, `SetMoving`, `CurrentMotionState`, `MentalSource`, `CurrentMentalState`, `playerMental`, `fallbackMentalState`, `motionState`, `detectMovementAutomatically`, `movingSpeedThreshold`.
- `MonsterMotionState` enum 媛믪? `Idle = 0`, `Run = 1`, `Attack = 2` 洹몃?濡??좎???
- 湲곗〈 EditMode 17/17 ?듦낵瑜?蹂댁〈?섍퀬, ?대룞 ?곹깭/怨듦꺽 蹂듦? ?뚭? ?뚯뒪?몃? 異붽???
- Unity 6000.3.8f1 湲곗??쇰줈 batchmode 而댄뙆?? EditMode ?뚯뒪?? PlayMode ?ㅻえ???먮뒗 ?먮뵒???뚮젅??寃利? 肄섏넄 濡쒓렇 臾댁삤瑜? ?꾨줈?뚯씪 鍮꾧탳 利앷굅瑜??④꺼.

### Must NOT have (guardrails, anti-slop, scope boundaries)
- ??AI, pathfinding, pooling, ECS, Job System, Burst, Timeline, Animator Controller 援ъ“ 蹂寃쎌쓣 ?꾩엯?섏? 留?
- `OnCollisionStay2D`瑜?`OnCollisionEnter2D`??trigger 湲곕컲?쇰줈 諛붽씀吏 留?
- `cachedPlayerTarget` 罹먯떆 ?꾨왂??理쒖쟻????곸쑝濡??쇱? 留? ?꾩옱 `ResolveTarget()`?먯꽌留??곗씠???鍮덈룄 寃쎈줈??
- 紐ъ뒪??prefab/controller/animation asset???대쾲 由ы뙥??踰붿쐞?먯꽌 ?섏젙?섏? 留? ?? Unity媛 ?뚯뒪???ㅽ뻾 以??먮룞?쇰줈 `.meta`瑜?媛깆떊?섎㈃ 蹂꾨룄 寃?????쒖쇅??
- `detectMovementAutomatically`? `movingSpeedThreshold` ?꾨뱶瑜???젣?섍굅???대쫫/??낆쓣 諛붽씀吏 留? 湲곗〈 ?꾨━??YAML????λ뤌 ?덉뼱.
- ?뚯뒪?몃? ?꾪빐 product API瑜??볧엳吏 留? ??API媛 ?꾩슂?섎㈃ `MonsterAttack`/`MonsterChase`媛 ?곕뒗 理쒖냼 硫붿꽌?쒕쭔 異붽??섍퀬, 湲곗〈 API??洹몃?濡???
- ?꾩옱 ?묒뾽?몃━??湲곗〈 ?ъ슜??蹂寃쎌쓣 ?섎룎由ъ? 留? ?뱁엳 `MonsterChase.ResolveTarget()`??`GameObject.FindGameObjectWithTag("Player")` 蹂寃쎌쓣 蹂댁〈??

## Verification strategy
> Zero human intervention - all verification is agent-executed.
- Test decision: tests-after + Unity Test Framework/NUnit EditMode. ?깅뒫 由ы뙥?곕씪 ?숈옉 ?좉툑 ?뚯뒪?몃? 癒쇱? 異붽??????덉쑝硫?醫뗭?留? 湲곗〈 肄붾뱶媛 以묐났 ?몄텧??愿李?媛?ν븯寃??몄텧?섏? ?딆븘??援ы쁽 吏곹썑 ?뚭? ?뚯뒪?몄? profiler 利앷굅瑜??④퍡 ?ъ슜??
- QA policy: every task has agent-executed scenarios
- Evidence: `.omo/evidence/task-<N>-monster-system-efficiency-refactor.<ext>`

## Execution strategy
### Parallel execution waves
> Target 5-8 tasks per wave. <3 per wave (except final) = under-splitting.
> Extract shared dependencies as Wave-1 tasks to maximize parallelism.

Wave 1 (no dependencies):
- Task 1: Dirty worktree? baseline 利앷굅 怨좎젙
- Task 2: `MonsterSanityAppearance` ?곹깭 寃뚯씠?몄? ?대룞 ?곹깭 硫붾え由?異붽?
- Task 3: 吏곷젹??API ?명솚??媛먯궗

Wave 2 (after Wave 1):
- Task 4: depends [2, 3] - `MonsterChase`媛 ?대룞 ?곹깭瑜??명삎???꾨떖
- Task 5: depends [2] - `MonsterAttack` 怨듦꺽 醫낅즺 蹂듦?瑜??ㅼ젣 ?대룞 ?곹깭 湲곕컲?쇰줈 蹂댁젙

Wave 3 (after Wave 2):
- Task 6: depends [1, 2, 3, 4, 5] - Unity 而댄뙆???뚯뒪???뚮젅???꾨줈?뚯씪 寃利?
Critical path: Task 2 -> Task 4 -> Task 6

### Dependency matrix
| Task | Depends on | Blocks | Can parallelize with |
|------|------------|--------|----------------------|
| 1    | none       | 6      | 2, 3                 |
| 2    | none       | 4, 5, 6| 1, 3                 |
| 3    | none       | 4, 6   | 1, 2                 |
| 4    | 2, 3       | 6      | 5                    |
| 5    | 2          | 6      | 4                    |
| 6    | 1, 2, 3, 4, 5 | none | none                 |

## Todos
> Implementation + Test = ONE task. Never separate.
> Every task MUST have: References + Acceptance Criteria + QA Scenarios + Commit.

- [ ] 1. Dirty worktree? baseline 利앷굅 怨좎젙

  What to do: ?꾩옱 ?묒뾽?몃━瑜?癒쇱? 利앷굅?뷀빐. ?대? ?섏젙/誘몄텛???곹깭???뚯씪??留롮쑝誘濡? ?댄썑 ?묒뾽?먮뒗 ??紐⑸줉 諛뽰쑝濡?臾대떒 蹂寃쎌쓣 ?볧엳吏 留먭퀬 寃뱀튂???뚯씪? 湲곗〈 diff瑜?蹂댁〈?섎㈃???섏젙?댁빞 ?? 湲곗〈 baseline?쇰줈 ?ъ슜?먭? ?쒓났??`active MonsterChase/Attack/Appearance 媛?2媛?, `BehaviourUpdate 28.6us`, `LateBehaviourUpdate 21.7us`, `EditMode 17/17 ?듦낵`瑜?evidence 硫붾え??湲곕줉??
  Must NOT do: `git reset`, `git checkout --`, prefab/controller/scene ?섎룎由ш린, baseline ?섏튂瑜???痢≪젙媛믪쑝濡???뼱?곌린.

  Parallelization: Can parallel: YES | Wave 1 | Blocks: [6] | Blocked by: []

  References (executor has NO interview context - be exhaustive):
  - Pattern:  `Assets/Scripts/Monster/MonsterChase.cs:94` - ?꾩옱 dirty diff??`FindFirstObjectByTag` 怨꾩뿴 蹂寃쎌씠 ?덉쑝??蹂댁〈?댁빞 ?섎뒗 ?源??댁꽍 寃쎈줈??
  - Pattern:  `Assets/Scripts/Monster/MonsterSanityAppearance.cs:53` - profiler?먯꽌 以꾩씪 `LateUpdate()` transform delta 寃쎈줈??
  - Pattern:  `Assets/Scripts/Monster/MonsterAttack.cs:39` - 荑⑤떎?댁쓣 ?좎??댁빞 ?섎뒗 諛섎났 異⑸룎 肄쒕갚 寃쎈줈??
  - Test:     `Assets/Tests/EditMode/MonsterChaseTests.cs:45` - ?꾩옱 ??誘몄텛???뚯뒪???뚯씪???ㅼ뼱?덈뒗 ?源??댁꽍 ?뚯뒪?몄빞.
  - Test:     `Assets/Tests/EditMode/PlayerMentalMonsterAppearanceTests.cs:73` - 湲곗〈 ?뺤떊 ?곹깭-Animator ?뚭? ?뚯뒪???⑦꽩?댁빞.
  - API/Type: `ProjectSettings/ProjectVersion.txt:1` - Unity 踰꾩쟾? `6000.3.8f1`?댁빞.

  Acceptance criteria (agent-executable only):
  - [ ] `powershell -NoProfile -Command "New-Item -ItemType Directory -Force .omo/evidence | Out-Null; git status --short | Tee-Object .omo/evidence/task-1-monster-system-efficiency-refactor-status.txt; git diff -- Assets/Scripts/Monster/MonsterChase.cs | Tee-Object .omo/evidence/task-1-monster-system-efficiency-refactor-monsterchase.diff"` ?ㅽ뻾 ??status ?뚯씪???꾩옱 dirty paths媛 ?⑥븘 ?덈떎.
  - [ ] `powershell -NoProfile -Command "$p='.omo/evidence/task-1-monster-system-efficiency-refactor-baseline.txt'; @('Unity: 6000.3.8f1','Existing EditMode: 17/17 pass (caller-provided)','Active MonsterChase/MonsterAttack/MonsterSanityAppearance: 2/2/2 (caller-provided)','BehaviourUpdate baseline: 28.6us','LateBehaviourUpdate baseline: 21.7us') | Set-Content $p; Test-Path $p"`媛 `True`瑜?異쒕젰?쒕떎.

  QA scenarios (MANDATORY - task incomplete without these):
  > Name the exact tool AND its exact invocation - not "verify it works". Browser use: in Codex, use `browser:control-in-app-browser` first when available and no authenticated/persistent user browser profile is required; otherwise use Chrome to drive the page, or agent-browser (https://github.com/vercel-labs/agent-browser) when Chrome is unavailable. Computer use: OS-level GUI automation for a non-browser desktop app.
  ```
  Scenario: dirty baseline captured
    Tool:     powershell
    Steps:    powershell -NoProfile -Command "git status --short | Set-Content .omo/evidence/task-1-monster-system-efficiency-refactor-status.txt; Select-String -Path .omo/evidence/task-1-monster-system-efficiency-refactor-status.txt -Pattern 'Assets/Scripts/Monster/MonsterChase.cs'"
    Expected: command exits 0 and prints the MonsterChase dirty entry.
    Evidence: .omo/evidence/task-1-monster-system-efficiency-refactor-status.txt

  Scenario: missing baseline is rejected
    Tool:     powershell
    Steps:    powershell -NoProfile -Command "$p='.omo/evidence/task-1-monster-system-efficiency-refactor-baseline.txt'; if (-not (Test-Path $p)) { exit 2 }; Select-String -Path $p -Pattern 'LateBehaviourUpdate baseline: 21.7us'"
    Expected: command exits 0 only when the baseline file includes the exact LateBehaviourUpdate baseline.
    Evidence: .omo/evidence/task-1-monster-system-efficiency-refactor-baseline.txt
  ```

  Commit: NO | Message: `chore(monster): capture motion refactor baseline` | Files: [.omo/evidence/task-1-monster-system-efficiency-refactor-status.txt, .omo/evidence/task-1-monster-system-efficiency-refactor-monsterchase.diff, .omo/evidence/task-1-monster-system-efficiency-refactor-baseline.txt]

- [ ] 2. `MonsterSanityAppearance` ?곹깭 寃뚯씠?몄? ?대룞 ?곹깭 硫붾え由?異붽?

  What to do: `MonsterSanityAppearance`??"留덉?留??대룞 ?щ?"? "Animator??留덉?留됱쑝濡??곸슜??motion state"瑜?遺꾨━????ν빐. `SetMotionState`??媛숈? ?곹깭媛 ?대? ?곸슜??寃쎌슦 `Animator.SetInteger`瑜??ㅼ떆 ?몄텧?섏? ?딄쾶 媛?쒗빐. `SetMoving(bool)`? ?대룞 ?щ?瑜?湲곕줉?섎릺, ?꾩옱 `Attack`?대㈃ `motionState`瑜???뼱?곗? ?딄쾶 ?? 怨듦꺽???앸궇 ???????덈뒗 理쒖냼 硫붿꽌???? `RestoreMovementMotionState`)瑜?異붽???留덉?留??대룞 ?щ? 湲곗??쇰줈 `Run`/`Idle`???곸슜?섍쾶 ?? `OnEnable()`??珥덇린 `ApplyMotionState()`???좎??댁꽌 Animator 珥덇린媛믪? 怨꾩냽 ?ㅼ젙?섍쾶 ??
  Must NOT do: serialized field ??젣/?대쫫 蹂寃? enum 媛?蹂寃? `SanityStage`/`MotionState` Animator ?뚮씪誘명꽣紐?蹂寃? ?뺤떊 ?곹깭 援щ룆 濡쒖쭅 蹂寃?

  Parallelization: Can parallel: YES | Wave 1 | Blocks: [4, 5, 6] | Blocked by: []

  References (executor has NO interview context - be exhaustive):
  - Pattern:  `Assets/Scripts/Monster/MonsterSanityAppearance.cs:15` - serialized `motionState`???좎??댁빞 ??
  - Pattern:  `Assets/Scripts/Monster/MonsterSanityAppearance.cs:16` - serialized `detectMovementAutomatically`??fallback ?명솚???꾪빐 ?좎??댁빞 ??
  - Pattern:  `Assets/Scripts/Monster/MonsterSanityAppearance.cs:17` - serialized `movingSpeedThreshold`??fallback ?명솚???꾪빐 ?좎??댁빞 ??
  - API/Type: `Assets/Scripts/Monster/MonsterSanityAppearance.cs:23` - `MonsterMotionState` enum ?レ옄 怨꾩빟?댁빞.
  - API/Type: `Assets/Scripts/Monster/MonsterSanityAppearance.cs:30` - public property/API???몃? ?뚯뒪?몄? 而댄룷?뚰듃媛 ?쎌쓣 ???덉뼱.
  - Pattern:  `Assets/Scripts/Monster/MonsterSanityAppearance.cs:103` - ?꾩옱 `SetMotionState`媛 ?숈씪 ?곹깭?먮룄 臾댁“嫄??곸슜?섎뒗 吏?먯씠??
  - Pattern:  `Assets/Scripts/Monster/MonsterSanityAppearance.cs:109` - `SetMoving`? 湲곗〈 怨듦컻 API???좎??댁빞 ??
  - Pattern:  `Assets/Scripts/Monster/MonsterSanityAppearance.cs:189` - `Animator.SetInteger(MotionStateParameter, ...)` ?몄텧 吏?먯씠??
  - Test:     `Assets/Tests/EditMode/PlayerMentalMonsterAppearanceTests.cs:27` - 鍮꾪솢??GameObject?먯꽌 而댄룷?뚰듃 珥덇린?붾? ?듭젣?섎뒗 ?뚯뒪???⑦꽩?댁빞.
  - External: `https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Animator.SetInteger.html` - Animator int parameter ?곌린 API 洹쇨굅??

  Acceptance criteria (agent-executable only):
  - [ ] `powershell -NoProfile -Command "Select-String -Path Assets/Scripts/Monster/MonsterSanityAppearance.cs -Pattern 'RestoreMovementMotionState|last.*Motion|applied.*Motion|isMoving|moving' -CaseSensitive:$false"`媛 ???곹깭 寃뚯씠???대룞 硫붾え由?援ы쁽 ?붿쟻??異쒕젰?쒕떎.
  - [ ] `powershell -NoProfile -Command "Select-String -Path Assets/Scripts/Monster/MonsterSanityAppearance.cs -Pattern 'Idle = 0|Run = 1|Attack = 2' | Measure-Object | Select-Object -ExpandProperty Count"`媛 `3`??異쒕젰?쒕떎.
  - [ ] `& 'C:\Program Files\Unity\Hub\Editor\6000.3.8f1\Editor\Unity.exe' -batchmode -quit -projectPath 'C:\Users\User\Desktop\Once-Upon-a-Lie_LittleRed' -runTests -testPlatform EditMode -testResults '.omo/evidence/task-2-monster-system-efficiency-refactor.xml' -logFile '.omo/evidence/task-2-monster-system-efficiency-refactor.log'`媛 ?ㅽ뙣 ?놁씠 ?앸굹怨? XML????appearance motion ?뚯뒪?멸? ?ы븿?쒕떎.

  QA scenarios (MANDATORY - task incomplete without these):
  > Name the exact tool AND its exact invocation - not "verify it works". Browser use: in Codex, use `browser:control-in-app-browser` first when available and no authenticated/persistent user browser profile is required; otherwise use Chrome to drive the page, or agent-browser (https://github.com/vercel-labs/agent-browser) when Chrome is unavailable. Computer use: OS-level GUI automation for a non-browser desktop app.
  ```
  Scenario: Attack state is not overwritten by movement input
    Tool:     powershell
    Steps:    powershell -NoProfile -Command "& 'C:\Program Files\Unity\Hub\Editor\6000.3.8f1\Editor\Unity.exe' -batchmode -quit -projectPath 'C:\Users\User\Desktop\Once-Upon-a-Lie_LittleRed' -runTests -testPlatform EditMode -testResults '.omo/evidence/task-2-monster-system-efficiency-refactor.xml' -logFile '.omo/evidence/task-2-monster-system-efficiency-refactor.log'; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }; Select-String -Path '.omo/evidence/task-2-monster-system-efficiency-refactor.xml' -Pattern 'Attack|Movement|Restore'"
    Expected: command exits 0 and the XML contains the new test proving `SetMoving(false)` during `Attack` does not change `CurrentMotionState` until restore.
    Evidence: .omo/evidence/task-2-monster-system-efficiency-refactor.xml

  Scenario: duplicate state path remains behaviorally stable
    Tool:     powershell
    Steps:    powershell -NoProfile -Command "Select-String -Path '.omo/evidence/task-2-monster-system-efficiency-refactor.log' -Pattern 'error CS|Compilation failed|AssertionException|Test Failed' -Quiet; if ($?) { exit 1 } else { exit 0 }"
    Expected: command exits 0, meaning no compile or NUnit failure appears in the Unity log.
    Evidence: .omo/evidence/task-2-monster-system-efficiency-refactor.log
  ```

  Commit: YES | Message: `perf(monster): gate duplicate motion state writes` | Files: [Assets/Scripts/Monster/MonsterSanityAppearance.cs, Assets/Tests/EditMode/PlayerMentalMonsterAppearanceTests.cs]

- [ ] 3. 吏곷젹??API ?명솚??媛먯궗

  What to do: 由ы뙥???꾪썑濡?prefab YAML怨?public API 怨꾩빟??源⑥?吏 ?딅뒗吏 ?먮룞 ?뺤씤?? `motionState`, `detectMovementAutomatically`, `movingSpeedThreshold`, `attackCooldown`, `attackAnimationDuration`, `stopDistance`媛 湲곗〈 紐ъ뒪??prefab/scene???⑥븘 ?덈뒗吏 ?뺤씤?섍퀬, `SetMotionState`, `SetMoving`, `CurrentMotionState` ?쒓렇?덉쿂媛 ?좎??섎뒗吏 ?뺤씤?? ???묒뾽? 媛먯궗/利앷굅 ?묒뾽?대ŉ product code瑜??섏젙?섏? ?딆븘.
  Must NOT do: prefab/controller/scene ??? YAML ?쇨큵 ?щ㎎, `.meta` ?ъ깮?? serialized field rename.

  Parallelization: Can parallel: YES | Wave 1 | Blocks: [4, 6] | Blocked by: []

  References (executor has NO interview context - be exhaustive):
  - Pattern:  `Assets/Sprites/Monster/Rabbit/Rabbit.prefab:139` - prefab??`motionState`媛 吏곷젹?붾뤌 ?덉뼱.
  - Pattern:  `Assets/Sprites/Monster/Rabbit/Rabbit.prefab:140` - prefab??`detectMovementAutomatically`媛 吏곷젹?붾뤌 ?덉뼱.
  - Pattern:  `Assets/Sprites/Monster/Rabbit/Rabbit.prefab:141` - prefab??`movingSpeedThreshold`媛 吏곷젹?붾뤌 ?덉뼱.
  - Pattern:  `Assets/Sprites/Monster/Rabbit/Rabbit.prefab:264` - prefab??`attackCooldown`??吏곷젹?붾뤌 ?덉뼱.
  - Pattern:  `Assets/Sprites/Monster/Rabbit/Rabbit.prefab:265` - prefab??`attackAnimationDuration`??吏곷젹?붾뤌 ?덉뼱.
  - Pattern:  `Assets/Sprites/Monster/Rabbit/Rabbit.prefab:279` - prefab??`stopDistance`媛 吏곷젹?붾뤌 ?덉뼱.
  - Pattern:  `Assets/Scenes/Stage1_Scene.unity:6465` - scene override??`attackAnimationDuration`???덉뼱.
  - API/Type: `Assets/Scripts/Monster/MonsterSanityAppearance.cs:79` - `SetPlayerMental` public API??
  - API/Type: `Assets/Scripts/Monster/MonsterSanityAppearance.cs:103` - `SetMotionState` public API??
  - API/Type: `Assets/Scripts/Monster/MonsterSanityAppearance.cs:109` - `SetMoving` public API??

  Acceptance criteria (agent-executable only):
  - [ ] `powershell -NoProfile -Command "rg -n 'motionState:|detectMovementAutomatically:|movingSpeedThreshold:|attackCooldown:|attackAnimationDuration:|stopDistance:' Assets/Sprites/Monster Assets/Scenes/Stage1_Scene.unity | Set-Content .omo/evidence/task-3-monster-system-efficiency-refactor-serialized-fields.txt; Get-Content .omo/evidence/task-3-monster-system-efficiency-refactor-serialized-fields.txt"` 異쒕젰??媛??꾨뱶紐낆씠 理쒖냼 ??踰??댁긽 ?ы븿?쒕떎.
  - [ ] `powershell -NoProfile -Command "rg -n 'public .*CurrentMotionState|public void SetMotionState|public void SetMoving|enum MonsterMotionState' Assets/Scripts/Monster/MonsterSanityAppearance.cs | Set-Content .omo/evidence/task-3-monster-system-efficiency-refactor-api.txt; (Get-Content .omo/evidence/task-3-monster-system-efficiency-refactor-api.txt).Count"`媛 `4` ?댁긽??異쒕젰?쒕떎.

  QA scenarios (MANDATORY - task incomplete without these):
  > Name the exact tool AND its exact invocation - not "verify it works". Browser use: in Codex, use `browser:control-in-app-browser` first when available and no authenticated/persistent user browser profile is required; otherwise use Chrome to drive the page, or agent-browser (https://github.com/vercel-labs/agent-browser) when Chrome is unavailable. Computer use: OS-level GUI automation for a non-browser desktop app.
  ```
  Scenario: serialized monster fields are still present
    Tool:     powershell
    Steps:    powershell -NoProfile -Command "$out='.omo/evidence/task-3-monster-system-efficiency-refactor-serialized-fields.txt'; rg -n 'motionState:|detectMovementAutomatically:|movingSpeedThreshold:|attackCooldown:|attackAnimationDuration:|stopDistance:' Assets/Sprites/Monster Assets/Scenes/Stage1_Scene.unity | Set-Content $out; foreach ($p in 'motionState','detectMovementAutomatically','movingSpeedThreshold','attackCooldown','attackAnimationDuration','stopDistance') { if (-not (Select-String -Path $out -Pattern $p -Quiet)) { exit 1 } }"
    Expected: command exits 0 only if every required serialized field still appears in assets or scene overrides.
    Evidence: .omo/evidence/task-3-monster-system-efficiency-refactor-serialized-fields.txt

  Scenario: accidental public API removal fails fast
    Tool:     powershell
    Steps:    powershell -NoProfile -Command "$out='.omo/evidence/task-3-monster-system-efficiency-refactor-api.txt'; rg -n 'public .*CurrentMotionState|public void SetMotionState|public void SetMoving|public void SetPlayerMental' Assets/Scripts/Monster/MonsterSanityAppearance.cs | Set-Content $out; foreach ($p in 'CurrentMotionState','SetMotionState','SetMoving','SetPlayerMental') { if (-not (Select-String -Path $out -Pattern $p -Quiet)) { exit 1 } }"
    Expected: command exits 0 only if all existing public members remain.
    Evidence: .omo/evidence/task-3-monster-system-efficiency-refactor-api.txt
  ```

  Commit: NO | Message: `test(monster): verify serialized motion compatibility` | Files: [.omo/evidence/task-3-monster-system-efficiency-refactor-serialized-fields.txt, .omo/evidence/task-3-monster-system-efficiency-refactor-api.txt]

- [ ] 4. `MonsterChase`媛 ?대룞 ?곹깭瑜??명삎???꾨떖

  What to do: `MonsterChase.Awake()`?먯꽌 `MonsterSanityAppearance`瑜?optional濡?罹먯떆?섍퀬, `FixedUpdate()`??紐⑤뱺 ?뺤? 寃쎈줈媛 `StopMovement()`瑜??듯빐 `SetMoving(false)`瑜?蹂닿퀬?섍쾶 ?? ?ㅼ젣 ?대룞 ?띾룄瑜??곸슜?섎뒗 寃쎈줈??`SetMoving(true)`瑜?蹂닿퀬?섍쾶 ?? `MonsterSanityAppearance`???몃? ?대룞 ?낅젰???덈뒗 ?몄뒪?댁뒪?먯꽌??`LateUpdate()`??transform delta 湲곕컲 ?먮룞 媛먯?瑜??ㅽ궢?섍퀬, `MonsterChase`媛 ?녿뒗 ?몄뒪?댁뒪?먯꽌??湲곗〈 fallback ?먮룞 媛먯?瑜??좎??? `StopMovement()`媛 `OnDisable()`?먯꽌???몄텧?섎?濡?鍮꾪솢?깊솕 ???명삎 ?곹깭媛 Idle濡??뺣━?섎뒗吏 ?뺤씤??
  Must NOT do: `ResolveTarget()` 罹먯떆 ?꾨왂 蹂寃? `body.linearVelocity` 怨꾩궛 蹂寃? `stopDistance` ?섎? 蹂寃? ?됰갚/?щ쭩/寃뚯엫 ?뺤? 遺꾧린 ?쒓굅.

  Parallelization: Can parallel: YES | Wave 2 | Blocks: [6] | Blocked by: [2, 3]

  References (executor has NO interview context - be exhaustive):
  - Pattern:  `Assets/Scripts/Monster/MonsterChase.cs:19` - `Awake()`?먯꽌 ?ㅻⅨ 而댄룷?뚰듃瑜?罹먯떆?섎뒗 湲곗〈 ?⑦꽩?댁빞.
  - Pattern:  `Assets/Scripts/Monster/MonsterChase.cs:38` - `OnDisable()`? `StopMovement()`瑜??몄텧?섎?濡??명삎 Idle 蹂닿퀬???ы븿?쇱빞 ??
  - Pattern:  `Assets/Scripts/Monster/MonsterChase.cs:43` - ?대룞/?뺤? ?먮떒??吏꾩엯?먯씠??
  - Pattern:  `Assets/Scripts/Monster/MonsterChase.cs:45` - ?щ쭩 ???뺤? 遺꾧린??
  - Pattern:  `Assets/Scripts/Monster/MonsterChase.cs:51` - 寃뚯엫 誘몄쭊?????뺤? 遺꾧린??
  - Pattern:  `Assets/Scripts/Monster/MonsterChase.cs:62` - target null ???뺤? 遺꾧린??
  - Pattern:  `Assets/Scripts/Monster/MonsterChase.cs:68` - knockback 以??뺤? 遺꾧린??
  - Pattern:  `Assets/Scripts/Monster/MonsterChase.cs:77` - stop distance ?덉そ ?뺤? 遺꾧린??
  - Pattern:  `Assets/Scripts/Monster/MonsterChase.cs:85` - ?ㅼ젣 ?대룞 velocity ?곸슜 吏?먯씠??
  - Pattern:  `Assets/Scripts/Monster/MonsterChase.cs:115` - 紐⑤뱺 ?뺤? 蹂닿퀬媛 紐⑥씪 helper??
  - Pattern:  `Assets/Scripts/Monster/MonsterSanityAppearance.cs:66` - ?쒓굅/?ㅽ궢??transform delta 怨꾩궛 吏?먯씠??
  - Test:     `Assets/Tests/EditMode/MonsterChaseTests.cs:20` - chase ?뚯뒪?몄쓽 SetUp/TearDown ?⑦꽩?댁빞.
  - External: `https://docs.unity3d.com/6000.5/Documentation/Manual/execution-order.html` - `FixedUpdate`/`Update`/`LateUpdate` ?쒖꽌 ?먮떒 洹쇨굅??

  Acceptance criteria (agent-executable only):
  - [ ] `powershell -NoProfile -Command "Select-String -Path Assets/Scripts/Monster/MonsterChase.cs -Pattern 'MonsterSanityAppearance|SetMoving|ReportMovement|RestoreMovement' -CaseSensitive:$false"`媛 `MonsterChase`?먯꽌 appearance ?대룞 蹂닿퀬 ?몄텧??異쒕젰?쒕떎.
  - [ ] `powershell -NoProfile -Command "Select-String -Path Assets/Scripts/Monster/MonsterSanityAppearance.cs -Pattern 'detectMovementAutomatically' | Select-Object -First 5"` 異쒕젰?먯꽌 fallback ?먮룞 媛먯? ?꾨뱶媛 ??젣?섏? ?딆븯?뚯쓣 ?뺤씤?쒕떎.
  - [ ] `& 'C:\Program Files\Unity\Hub\Editor\6000.3.8f1\Editor\Unity.exe' -batchmode -quit -projectPath 'C:\Users\User\Desktop\Once-Upon-a-Lie_LittleRed' -runTests -testPlatform EditMode -testResults '.omo/evidence/task-4-monster-system-efficiency-refactor.xml' -logFile '.omo/evidence/task-4-monster-system-efficiency-refactor.log'`媛 ?ㅽ뙣 ?놁씠 ?앸굹怨? chase movement state ?뚯뒪?멸? ?ы븿?쒕떎.

  QA scenarios (MANDATORY - task incomplete without these):
  > Name the exact tool AND its exact invocation - not "verify it works". Browser use: in Codex, use `browser:control-in-app-browser` first when available and no authenticated/persistent user browser profile is required; otherwise use Chrome to drive the page, or agent-browser (https://github.com/vercel-labs/agent-browser) when Chrome is unavailable. Computer use: OS-level GUI automation for a non-browser desktop app.
  ```
  Scenario: chase reports Run when it applies velocity
    Tool:     powershell
    Steps:    powershell -NoProfile -Command "& 'C:\Program Files\Unity\Hub\Editor\6000.3.8f1\Editor\Unity.exe' -batchmode -quit -projectPath 'C:\Users\User\Desktop\Once-Upon-a-Lie_LittleRed' -runTests -testPlatform EditMode -testResults '.omo/evidence/task-4-monster-system-efficiency-refactor.xml' -logFile '.omo/evidence/task-4-monster-system-efficiency-refactor.log'; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }; Select-String -Path '.omo/evidence/task-4-monster-system-efficiency-refactor.xml' -Pattern 'Chase|Run|Moving'"
    Expected: command exits 0 and the XML contains a test where `FixedUpdate` movement drives `CurrentMotionState` to `Run`.
    Evidence: .omo/evidence/task-4-monster-system-efficiency-refactor.xml

  Scenario: chase reports Idle on stop paths
    Tool:     powershell
    Steps:    powershell -NoProfile -Command "Select-String -Path '.omo/evidence/task-4-monster-system-efficiency-refactor.xml' -Pattern 'Idle|Stop|Disable'; Select-String -Path '.omo/evidence/task-4-monster-system-efficiency-refactor.log' -Pattern 'error CS|AssertionException|Test Failed' -Quiet; if ($?) { exit 1 } else { exit 0 }"
    Expected: command exits 0 and no Unity compile/test failure appears in the log.
    Evidence: .omo/evidence/task-4-monster-system-efficiency-refactor.log
  ```

  Commit: YES | Message: `perf(monster): drive appearance motion from chase` | Files: [Assets/Scripts/Monster/MonsterChase.cs, Assets/Scripts/Monster/MonsterSanityAppearance.cs, Assets/Tests/EditMode/MonsterChaseTests.cs]

- [ ] 5. `MonsterAttack` 怨듦꺽 醫낅즺 蹂듦?瑜??ㅼ젣 ?대룞 ?곹깭 湲곕컲?쇰줈 蹂댁젙

  What to do: `MonsterAttack.Update()`?먯꽌 `attackAnimationEndTime`??吏?섎㈃ `appearance.SetMotionState(Run)`?쇰줈 怨좎젙 蹂듦??섏? 留먭퀬, Task 2??movement restore 硫붿꽌?쒕? ?몄텧??留덉?留??대룞 ?щ? 湲곗??쇰줈 `Run` ?먮뒗 `Idle`濡??뚯븘媛寃??? `PlayAttackAnimation()`??`Attack`???ㅼ젙?섍퀬 `attackAnimationEndTime`??媛깆떊?섎뒗 ?쒖꽌???좎??? `OnCollisionStay2D()`??荑⑤떎???곕?吏 議곌굔? 洹몃?濡??? EditMode ?뚯뒪?몃뒗 private `PlayAttackAnimation()`/`Update()`瑜?reflection?쇰줈 ?몄텧?섍퀬, 怨듦꺽 以??대룞 蹂닿퀬??Attack????뼱?곗? ?딆쑝硫?醫낅즺 ??`Idle`/`Run`?쇰줈 蹂듦??⑥쓣 寃利앺빐.
  Must NOT do: `OnCollisionStay2D` ?몄텧 諛⑹떇 蹂寃? `nextAttackTime` 怨꾩궛 蹂寃? `attackCooldown`/`attackAnimationDuration` serialized field 蹂寃? PlayerMental ?먯깋 ?쒖꽌 蹂寃?

  Parallelization: Can parallel: YES | Wave 2 | Blocks: [6] | Blocked by: [2]

  References (executor has NO interview context - be exhaustive):
  - Pattern:  `Assets/Scripts/Monster/MonsterAttack.cs:27` - 怨듦꺽 醫낅즺 ??대㉧瑜?寃?ы븯??`Update()`??
  - Pattern:  `Assets/Scripts/Monster/MonsterAttack.cs:34` - `attackAnimationEndTime`??由ъ뀑?섎뒗 ?쒖꽌???좎??댁빞 ??
  - Pattern:  `Assets/Scripts/Monster/MonsterAttack.cs:36` - ?꾩옱 臾댁“嫄?`Run`?쇰줈 蹂듦??섎뒗 臾몄젣 吏?먯씠??
  - Pattern:  `Assets/Scripts/Monster/MonsterAttack.cs:39` - 諛섎났 異⑸룎 肄쒕갚 吏꾩엯?먯씠??
  - Pattern:  `Assets/Scripts/Monster/MonsterAttack.cs:52` - 荑⑤떎???щ쭩 議곌굔???좎??댁빞 ??
  - Pattern:  `Assets/Scripts/Monster/MonsterAttack.cs:64` - ?뺤떊???곕?吏 ?곸슜 吏?먯씠??
  - Pattern:  `Assets/Scripts/Monster/MonsterAttack.cs:66` - `nextAttackTime` 媛깆떊 ?쒖꽌媛 蹂댁〈?쇱빞 ??
  - Pattern:  `Assets/Scripts/Monster/MonsterAttack.cs:91` - `PlayAttackAnimation()`? Attack 吏꾩엯???좎??댁빞 ??
  - Test:     `Assets/Tests/EditMode/PlayerMentalMonsterAppearanceTests.cs:125` - private/public 硫붿꽌??reflection ?몄텧 ?⑦꽩?댁빞.
  - External: `https://docs.unity3d.com/6000.0/Documentation/ScriptReference/MonoBehaviour.OnCollisionStay.html` - `OnCollisionStay`媛 ?묒큺 以?諛섎났 ?몄텧?섎뒗 洹쇨굅??
  - External: `https://docs.unity3d.com/6000.0/Documentation/Manual/collider-interactions-oncollision.html` - collision stay媛 臾쇰━ ?낅뜲?댄듃 ?숈븞 諛섎났?섎뒗 洹쇨굅??

  Acceptance criteria (agent-executable only):
  - [ ] `powershell -NoProfile -Command "Select-String -Path Assets/Scripts/Monster/MonsterAttack.cs -Pattern 'SetMotionState\\(MonsterSanityAppearance.MonsterMotionState.Run\\)' -Quiet; if ($?) { exit 1 } else { exit 0 }"`媛 0?쇰줈 ?앸굹??臾댁“嫄?Run 蹂듦?媛 ?쒓굅?먯쓬???뺤씤?쒕떎.
  - [ ] `powershell -NoProfile -Command "Select-String -Path Assets/Scripts/Monster/MonsterAttack.cs -Pattern 'attackAnimationEndTime = -1f|PlayAttackAnimation|nextAttackTime = Time.time \\+ attackCooldown' | Measure-Object | Select-Object -ExpandProperty Count"`媛 `3` ?댁긽??異쒕젰?쒕떎.
  - [ ] `& 'C:\Program Files\Unity\Hub\Editor\6000.3.8f1\Editor\Unity.exe' -batchmode -quit -projectPath 'C:\Users\User\Desktop\Once-Upon-a-Lie_LittleRed' -runTests -testPlatform EditMode -testResults '.omo/evidence/task-5-monster-system-efficiency-refactor.xml' -logFile '.omo/evidence/task-5-monster-system-efficiency-refactor.log'`媛 ?ㅽ뙣 ?놁씠 ?앸굹怨? attack restore ?뚯뒪?멸? ?ы븿?쒕떎.

  QA scenarios (MANDATORY - task incomplete without these):
  > Name the exact tool AND its exact invocation - not "verify it works". Browser use: in Codex, use `browser:control-in-app-browser` first when available and no authenticated/persistent user browser profile is required; otherwise use Chrome to drive the page, or agent-browser (https://github.com/vercel-labs/agent-browser) when Chrome is unavailable. Computer use: OS-level GUI automation for a non-browser desktop app.
  ```
  Scenario: attack ends into Idle when the monster is stopped
    Tool:     powershell
    Steps:    powershell -NoProfile -Command "& 'C:\Program Files\Unity\Hub\Editor\6000.3.8f1\Editor\Unity.exe' -batchmode -quit -projectPath 'C:\Users\User\Desktop\Once-Upon-a-Lie_LittleRed' -runTests -testPlatform EditMode -testResults '.omo/evidence/task-5-monster-system-efficiency-refactor.xml' -logFile '.omo/evidence/task-5-monster-system-efficiency-refactor.log'; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }; Select-String -Path '.omo/evidence/task-5-monster-system-efficiency-refactor.xml' -Pattern 'Attack|Idle|Restore'"
    Expected: command exits 0 and the XML includes a stopped attack restore test.
    Evidence: .omo/evidence/task-5-monster-system-efficiency-refactor.xml

  Scenario: cooldown path is not structurally changed
    Tool:     powershell
    Steps:    powershell -NoProfile -Command "Select-String -Path Assets/Scripts/Monster/MonsterAttack.cs -Pattern 'Time.time < nextAttackTime|nextAttackTime = Time.time \\+ attackCooldown|OnCollisionStay2D' | Set-Content .omo/evidence/task-5-monster-system-efficiency-refactor-cooldown.txt; foreach ($p in 'Time.time < nextAttackTime','nextAttackTime = Time.time + attackCooldown','OnCollisionStay2D') { if (-not (Select-String -Path .omo/evidence/task-5-monster-system-efficiency-refactor-cooldown.txt -Pattern ([regex]::Escape($p)) -Quiet)) { exit 1 } }"
    Expected: command exits 0 only if the cooldown guard, cooldown assignment, and collision stay callback still exist.
    Evidence: .omo/evidence/task-5-monster-system-efficiency-refactor-cooldown.txt
  ```

  Commit: YES | Message: `fix(monster): restore attack to current movement state` | Files: [Assets/Scripts/Monster/MonsterAttack.cs, Assets/Scripts/Monster/MonsterSanityAppearance.cs, Assets/Tests/EditMode/PlayerMentalMonsterAppearanceTests.cs]

- [ ] 6. Unity 而댄뙆???뚯뒪???뚮젅???꾨줈?뚯씪 寃利?
  What to do: 紐⑤뱺 援ы쁽 ??Unity batchmode 而댄뙆?? EditMode ?꾩껜 ?뚯뒪?? PlayMode ?ㅻえ???먮뒗 ?먮뵒???뚮젅??寃利? 肄섏넄 濡쒓렇 ?뺤씤, profiler 鍮꾧탳瑜??ㅽ뻾?? LSP C# ?쒕쾭媛 ?놁쑝誘濡?Unity 而댄뙆??肄섏넄??吏꾨떒 ?뚯뒪濡??쇱븘. ?꾨줈?뚯씪? 媛숈? 議곌굔??`Assets/Scenes/Stage1_Scene.unity`?먯꽌 active `MonsterChase`/`MonsterAttack`/`MonsterSanityAppearance` 媛?2媛쒕? ?뺤씤?섍퀬, `BehaviourUpdate`? `LateBehaviourUpdate`瑜?baseline怨?鍮꾧탳?? ?꾧꺽???깃났 湲곗?? `LateBehaviourUpdate` 3??median??21.7us ?댄븯, `BehaviourUpdate` 3??median??31.5us ?댄븯??
  Must NOT do: ?깅뒫 ?섏튂媛 ???섏솕?ㅺ퀬 scope 諛?理쒖쟻??異붽?, scene/prefab ??? Profiler 利앷굅 ?놁씠 "媛쒖꽑?? 二쇱옣.

  Parallelization: Can parallel: NO | Wave 3 | Blocks: [] | Blocked by: [1, 2, 3, 4, 5]

  References (executor has NO interview context - be exhaustive):
  - Pattern:  `ProjectSettings/ProjectVersion.txt:1` - Unity `6000.3.8f1` ?ㅽ뻾 ?뚯씪????
  - Pattern:  `ProjectSettings/EditorBuildSettings.asset:8` - `Assets/Scenes/MainMenu.unity`??鍮뚮뱶 ?ㅼ젙???ы븿???덉뼱.
  - Pattern:  `ProjectSettings/EditorBuildSettings.asset:11` - `Assets/Scenes/Stage1_Scene.unity`??鍮뚮뱶 ?ㅼ젙???ы븿???덉뼱.
  - Pattern:  `Packages/manifest.json:17` - Unity Test Framework `1.6.0`???ㅼ튂???덉뼱.
  - Test:     `Assets/Tests/EditMode/LittleRed.Tests.EditMode.asmdef:1` - EditMode ?뚯뒪???댁뀍釉붾━??
  - Test:     `Assets/Tests/EditMode/MonsterChaseTests.cs:55` - 紐ъ뒪??prefab/controller ?곹깭留?寃利앹씠 ?덉뼱.
  - Test:     `Assets/Tests/EditMode/PlayerMentalMonsterAppearanceTests.cs:73` - ?뺤떊 ?곹깭 Animator ?뚮씪誘명꽣 寃利앹씠 ?덉뼱.
  - External: `https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/run-tests-from-command-line.html` - `-runTests -batchmode -testResults -testPlatform` 怨듭떇 ?ㅽ뻾 洹쇨굅??
  - External: `https://docs.unity3d.com/6000.5/Documentation/Manual/EditorCommandLineArguments.html` - `-logFile`怨?profiler log command line 洹쇨굅??
  - External: `https://docs.unity3d.com/6000.5/Documentation/Manual/Profiler.html` - profiler ?깅뒫 利앷굅 洹쇨굅??

  Acceptance criteria (agent-executable only):
  - [ ] `& 'C:\Program Files\Unity\Hub\Editor\6000.3.8f1\Editor\Unity.exe' -batchmode -quit -projectPath 'C:\Users\User\Desktop\Once-Upon-a-Lie_LittleRed' -logFile '.omo/evidence/task-6-monster-system-efficiency-refactor-compile.log'`媛 0?쇰줈 ?앸굹怨?log??`error CS`, `Compilation failed`, `Unhandled Exception`???녿떎.
  - [ ] `& 'C:\Program Files\Unity\Hub\Editor\6000.3.8f1\Editor\Unity.exe' -batchmode -quit -projectPath 'C:\Users\User\Desktop\Once-Upon-a-Lie_LittleRed' -runTests -testPlatform EditMode -testResults '.omo/evidence/task-6-monster-system-efficiency-refactor-editmode.xml' -logFile '.omo/evidence/task-6-monster-system-efficiency-refactor-editmode.log'`媛 0?쇰줈 ?앸굹怨?XML??湲곗〈 17媛쒕낫??留롮? ?뚯뒪???섏? 0 failures瑜?蹂닿퀬?쒕떎.
  - [ ] `powershell -NoProfile -Command "rg -n 'm_Name: MonsterChase|m_Name: MonsterAttack|m_Name: MonsterSanityAppearance|MonsterChase|MonsterAttack|MonsterSanityAppearance' Assets/Scenes/Stage1_Scene.unity Assets/Sprites/Monster/*.prefab | Set-Content .omo/evidence/task-6-monster-system-efficiency-refactor-component-count.txt"` ?ㅽ뻾 ??evidence????而댄룷?뚰듃紐낆씠 紐⑤몢 ?덈떎.
  - [ ] profiler 利앷굅 ?뚯씪 `.omo/evidence/task-6-monster-system-efficiency-refactor-profile.csv` ?먮뒗 `.raw`媛 議댁옱?섍퀬, 3??median 鍮꾧탳 硫붾え `.omo/evidence/task-6-monster-system-efficiency-refactor-profile-summary.txt`??`LateBehaviourUpdate <= 21.7us`? `BehaviourUpdate <= 31.5us`媛 湲곕줉???덈떎.

  QA scenarios (MANDATORY - task incomplete without these):
  > Name the exact tool AND its exact invocation - not "verify it works". Browser use: in Codex, use `browser:control-in-app-browser` first when available and no authenticated/persistent user browser profile is required; otherwise use Chrome to drive the page, or agent-browser (https://github.com/vercel-labs/agent-browser) when Chrome is unavailable. Computer use: OS-level GUI automation for a non-browser desktop app.
  ```
  Scenario: Unity batch compile and EditMode suite pass
    Tool:     powershell
    Steps:    powershell -NoProfile -Command "& 'C:\Program Files\Unity\Hub\Editor\6000.3.8f1\Editor\Unity.exe' -batchmode -quit -projectPath 'C:\Users\User\Desktop\Once-Upon-a-Lie_LittleRed' -runTests -testPlatform EditMode -testResults '.omo/evidence/task-6-monster-system-efficiency-refactor-editmode.xml' -logFile '.omo/evidence/task-6-monster-system-efficiency-refactor-editmode.log'; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }; if (Select-String -Path '.omo/evidence/task-6-monster-system-efficiency-refactor-editmode.log' -Pattern 'error CS|Compilation failed|Unhandled Exception|Test Failed' -Quiet) { exit 1 }"
    Expected: command exits 0, XML reports 0 failures, and Unity log has no compile/runtime errors.
    Evidence: .omo/evidence/task-6-monster-system-efficiency-refactor-editmode.xml

  Scenario: play/profile evidence meets baseline
    Tool:     powershell
    Steps:    powershell -NoProfile -Command "$summary='.omo/evidence/task-6-monster-system-efficiency-refactor-profile-summary.txt'; if (-not (Test-Path $summary)) { exit 2 }; foreach ($p in 'LateBehaviourUpdate <= 21.7us','BehaviourUpdate <= 31.5us','MonsterChase active: 2','MonsterAttack active: 2','MonsterSanityAppearance active: 2') { if (-not (Select-String -Path $summary -Pattern ([regex]::Escape($p)) -Quiet)) { exit 1 } }"
    Expected: command exits 0 only if the profiler summary includes component counts and baseline thresholds.
    Evidence: .omo/evidence/task-6-monster-system-efficiency-refactor-profile-summary.txt
  ```

  Commit: NO | Message: `test(monster): validate motion performance refactor` | Files: [.omo/evidence/task-6-monster-system-efficiency-refactor-compile.log, .omo/evidence/task-6-monster-system-efficiency-refactor-editmode.xml, .omo/evidence/task-6-monster-system-efficiency-refactor-editmode.log, .omo/evidence/task-6-monster-system-efficiency-refactor-component-count.txt, .omo/evidence/task-6-monster-system-efficiency-refactor-profile-summary.txt]

## Final verification wave (MANDATORY - after all implementation tasks)
> Runs in PARALLEL. ALL must APPROVE. Surface results to the caller and wait for an explicit "okay" before declaring complete.
- [ ] F1. Plan compliance audit - every task done, every acceptance criterion met
- [ ] F2. Code quality review - diagnostics clean, idioms match, no dead code
- [ ] F3. Real manual QA - every QA scenario executed with evidence captured
- [ ] F4. Scope fidelity - nothing extra shipped beyond Must-Have, nothing Must-NOT-Have introduced

## Commit strategy
- One logical change per commit. Conventional Commits (`<type>(<scope>): <subject>` body + footer).
- Atomic: every commit builds and passes tests on its own.
- No "WIP" / "fix typo squash later" commits on the final branch - clean up before merge.
- Reference the plan file path in the final commit footer: `Plan: .omo/plans/monster-system-efficiency-refactor.md`.

## Success criteria
- All Must-Have shipped; all QA scenarios pass with captured evidence; F1-F4 approved; commit history clean.

