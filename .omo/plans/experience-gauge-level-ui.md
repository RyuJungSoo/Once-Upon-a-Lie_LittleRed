# experience-gauge-level-ui - Work Plan

## TL;DR (For humans)
<!-- Fill this LAST, after the detailed plan below is written, so it summarizes the REAL plan. -->
<!-- Plain English for a non-engineer: NO file paths, NO todo numbers, NO wave/agent/tool names. -->

**What you'll get:** 경험치 크리스탈을 먹으면 초록색 게이지가 왼쪽부터 부드럽게 차고, 100%에 도달한 순간에만 레벨 텍스트가 증가한다. 초과 경험치는 가득 찬 상태를 잠깐 보여준 뒤 다음 게이지로 이월되며, 여러 레벨과 연속 획득도 순서대로 처리된다.

**Why this approach:** 실제 레벨과 능력치는 기존 게임 로직대로 즉시 갱신하고, 화면 표시만 별도의 결정론적 상태로 순서를 제어한다. 덕분에 게임 규칙을 건드리지 않으면서 `100% → 레벨 증가 → 초과분`이라는 시각적 약속을 정확히 지킬 수 있다.

**What it will NOT do:** 경험치 요구량·크리스탈 보상·스탯 적용 시점을 바꾸지 않는다. 새 아트나 외부 패키지를 추가하지 않고, 무료 UI 원본 프리팹과 다른 HUD 요소도 수정하지 않는다.

**Effort:** Medium
**Risk:** Medium - 동기 이벤트가 한 번의 경험치 추가 중 여러 번 발생하므로 직접 UI 갱신을 정확히 차단하고 재동기화해야 한다.
**Decisions to sanity-check:** 전체 바 0.35초, 짧은 구간 최소 0.08초, full hold 0.10초; 초록색 `#8EC54A`; 실제 게임 레벨은 즉시 적용하고 HUD 텍스트만 지연.

Your next move: 실행을 시작하거나, 먼저 고정밀 계획 검토를 요청한다. Full execution detail follows below.

---

> TL;DR (machine): Medium effort / Medium risk; add a deterministic XP presentation state, UIManager event/lifecycle integration, a green left-origin Stage1 Filler override, EditMode ordering regressions, and agent-run Play Mode evidence without changing authoritative XP rules.

## Scope
### Must have
- `ExpCrystal` 획득으로 들어온 경험치를 기존 `PlayerExperience`/`GameManager`가 즉시 처리하는 권위 상태는 그대로 유지한다.
- HUD 경험치 표시는 현재 값에서 시작해 왼쪽에서 오른쪽으로 증가하며, 임계값마다 `fillAmount == 1.0`에 도달한 프레임에만 `Level Text`를 다음 레벨로 바꾼다.
- 레벨 텍스트 변경 후 가득 찬 바를 0.10초 유지하고 0으로 재설정한 다음, 초과 경험치를 다음 바에 표시한다.
- 한 번에 여러 레벨을 올리는 경험치와 같은 프레임/연출 도중 들어오는 추가 경험치를 누락·중복·순서 역전 없이 큐로 처리한다.
- Stage1의 `Canvas/Exp_UI/Bar_1/Filler`만 불투명 초록색 `#8EC54A`로 바꾸고 `Filled / Horizontal / Left` 구성을 고정한다.
- 비활성화, 재바인딩, 스테이지 시작, 직접 경험치 설정 등 애니메이션 외 상태 변경 뒤에는 권위 경험치/레벨과 HUD를 재동기화한다.
- 정확한 중간 표시 순서와 기존 경험치 규칙을 EditMode 자동 테스트로 검증하고, Stage1 Play Mode에서 실제 크리스탈 획득을 검증한다.

### Must NOT have (guardrails, anti-slop, scope boundaries)
- `PlayerExperience`, `GameManager`, `PlayerLevelStats`의 경험치 계산, 실제 레벨업 시점, 스탯 적용 시점은 변경하지 않는다.
- 레벨당 필요 경험치 100과 `ExpCrystal_low/medium/high`의 5/8/12 보상은 변경하지 않는다.
- 무료 UI 팩 원본 `Assets/Free UI build package/Prefabs/Gray/Bars/Bar_1.prefab`을 수정하거나 다른 바의 색상을 바꾸지 않는다.
- 사용자가 직접 작업 중인 Stage1 카메라 계층, CinemachineCamera, CinemachineBrain, 카메라 크기/노이즈, `MentalCameraShake` 및 관련 직렬화는 수정·삭제·복원하지 않는다.
- 새 이미지, 셰이더, 머티리얼, tween 패키지, 서드파티 의존성, PlayMode 테스트 어셈블리를 추가하지 않는다.
- 경험치 숫자 표기, 레벨업 VFX/오디오, 저장 시스템, HUD 재배치는 추가하지 않는다.
- 코루틴이나 fire-and-forget 비동기 작업을 사용하지 않는다. 프레임 진행은 하나의 결정론적 표시 상태와 `Time.unscaledDeltaTime`만 사용한다.

## Verification strategy
> Zero human intervention - all verification is agent-executed.
- Test decision: TDD + Unity Test Framework/NUnit EditMode. 순수 표시 상태 테스트를 먼저 실패시키고 구현하며, UIManager 통합/씬 설정도 같은 todo 안에서 대응 테스트와 함께 완성한다.
- Targeted commands:
  - Unity MCP `run_tests(mode="EditMode", assembly_names=["LittleRed.Tests.EditMode"], test_names=["ExperienceProgressPresentationTests"], include_details=true)`
  - Unity MCP `run_tests(mode="EditMode", assembly_names=["LittleRed.Tests.EditMode"], test_names=["ExperienceProgressUITests"], include_details=true)`
  - Unity MCP `run_tests(mode="EditMode", assembly_names=["LittleRed.Tests.EditMode"], test_names=["ExperienceProgressSceneTests"], include_details=true)`
  - Unity MCP `run_tests(mode="EditMode", assembly_names=["LittleRed.Tests.EditMode"], test_names=["ExperienceCrystalTests"], include_details=true)`
  - 최종 회귀: Unity MCP `run_tests(mode="EditMode", assembly_names=["LittleRed.Tests.EditMode"], include_details=true, include_failed_tests=true)`
- 각 `run_tests`가 반환한 `job_id`는 `get_test_job(job_id=..., include_details=true, wait_timeout=60)`로 terminal 상태까지 확인하고, `read_console(types=["Error","Warning"], count=200)`에서 새 컴파일 오류/예외가 0개인지 확인한다.
- Play Mode는 Unity MCP로 Stage1을 열고 실제 크리스탈 프리팹을 플레이어 위치에 생성한다. 자동 테스트가 중간 순서를 수치로 증명하고, Play Mode는 실제 트리거·색상·방향·텍스트 표시가 연결됐는지 증명한다.
- Evidence: <attemptDir>/task-<N>-experience-gauge-level-ui.<ext> (attemptDir = currentAttemptDir from 'omo ulw-loop status --json', .omo/evidence/ulw/<session>/<goalId>/a<attempt>; outside ulw-loop use .omo/evidence/)

## Execution strategy
### Parallel execution waves
> Target 5-8 todos per wave. Fewer than 3 (except the final) means you under-split.
- Wave 1 — 독립 기반 작업을 병렬 수행:
  - Todo 1: 결정론적 경험치 표시 상태와 단위 테스트.
  - Todo 2: 기존 권위 경험치/레벨 이벤트 계약 회귀 테스트.
  - Todo 3: Stage1 게이지 씬 설정과 씬 회귀 테스트.
- Wave 2 — Wave 1 결과를 통합:
  - Todo 4: UIManager 이벤트/수명주기 연결, 엄격한 표시 순서, 전체 통합 테스트.
- Final verification wave — 모든 구현 todo 이후 F1~F4를 병렬 실행하고 네 항목 모두 승인돼야 완료한다.

### Dependency matrix
| Todo | Depends on | Blocks | Can parallelize with |
| --- | --- | --- | --- |
| 1 | — | 4 | 2, 3 |
| 2 | — | 4 | 1, 3 |
| 3 | — | 4, F1-F4 | 1, 2 |
| 4 | 1, 2, 3 | F1-F4 | — |

## Todos
> Implementation + Test = ONE todo. Never separate.
<!-- APPEND TASK BATCHES BELOW THIS LINE WITH edit/apply_patch - never rewrite the headers above. -->
- [x] 1. 결정론적 경험치 표시 상태를 추가하고 임계값·이월·다중 레벨·연속 획득을 단위 테스트한다
  - What to do:
    - `Assets/Scripts/UI/ExperienceProgressPresentation.cs`에 Unity 씬이나 정적 싱글톤에 의존하지 않는 표시 상태 객체를 추가한다.
    - 상태는 `Idle`, `Filling`, `HoldingFull` 세 단계와 표시 경험치/필 비율, 표시 레벨, 대기 경험치량, 대기 레벨 큐, 마지막 권위 스냅샷을 소유한다.
    - 초기화/재동기화 입력은 `(currentExperience, requiredExperience, level)`, 이벤트 입력은 `EnqueueExperience(amount)`, `EnqueueLevel(level)`, 권위 상태 입력은 `(currentExperience, requiredExperience, level)`, 프레임 입력은 명시적 `Advance(deltaTime)`로 고정한다.
    - 한 바 전체 시간은 0.35초, 각 부분 구간 최소 시간은 0.08초, full hold는 0.10초로 계산한다. 잘못된 직렬화 값은 각각 최소 양수/0으로 clamp한다.
    - 현재 표시 경험치와 대기 경험치의 합을 100 단위 구간으로 분해한다. 임계값 구간은 정확히 `fill == 1f`가 된 시점에만 대기 레벨 하나를 적용하고 `HoldingFull`로 전환한다. hold가 끝난 뒤 0으로 재설정하고 남은 경험치를 다음 구간에서 채운다.
    - 최종 권위 스냅샷과 예상 최종 XP/레벨이 다르거나 임계값 수와 대기 레벨 수가 맞지 않으면 애니메이션을 계속 추측하지 말고 큐를 취소한 뒤 최신 권위 상태로 즉시 재동기화한다.
    - `Assets/Tests/EditMode/ExperienceProgressPresentationTests.cs`를 만들고 기존 테스트 어셈블리 관례대로 `Type.GetType(..., Assembly-CSharp)`와 reflection을 사용해 내부 구현을 검증한다.
  - Must NOT do:
    - `PlayerExperience`나 `GameManager`를 참조하지 않는다.
    - 코루틴, `Time.deltaTime`, `Time.unscaledDeltaTime`, 실제 프레임 대기, 실시간 sleep을 순수 상태 객체 안에서 사용하지 않는다.
    - 부동소수점 근사 때문에 full 판정이 누락되지 않도록 `Mathf.Approximately`만 믿지 말고 목표 도달 시 값을 명시적으로 1f로 고정한다.
  - Parallelization: Wave 1 | Blocked by: — | Blocks: 4
  - References:
    - `Assets/Scripts/Player/PlayerExperience.cs:7-15` — 고정 100 XP 계약.
    - `Assets/Scripts/Player/PlayerExperience.cs:90-115` — XP 추가 이벤트가 레벨 처리보다 먼저 발생하는 순서.
    - `Assets/Scripts/Player/PlayerExperience.cs:160-203` — 초과 XP와 다중 레벨 처리.
    - `Assets/Tests/EditMode/ExperienceCrystalTests.cs:9-23` — EditMode 어셈블리에서 Assembly-CSharp 타입을 reflection으로 접근하는 프로젝트 관례.
    - `.omo/drafts/experience-gauge-level-ui.md` — 승인된 시간값과 표시 순서.
  - Acceptance criteria:
    - `ExperienceProgressPresentationTests`가 아래를 모두 통과한다.
      - `95 + 5`: full 이전에는 레벨 1, full 프레임에는 fill 1과 레벨 2, hold 이후 fill 0.
      - `95 + 17`: full 프레임에서 레벨 2가 된 뒤에만 reset되고 최종 fill 0.12.
      - `0 + 312`: full/text/reset을 세 번 반복해 레벨 4와 fill 0.12.
      - 같은 `Advance` 전 여러 gain과 `Filling` 중 추가 gain이 모두 최종 권위 값과 일치하며 누락/중복이 없음.
      - 비양수 gain은 무시하고, 음수 deltaTime은 진행시키지 않으며, 불일치 스냅샷은 즉시 권위 상태로 복구함.
    - Unity MCP targeted test 결과가 passed이며 failed/skipped가 0이다.
  - QA scenarios:
    - Happy: `run_tests(... test_names=["ExperienceProgressPresentationTests"])`로 각 전이 직전/직후 fill, 표시 레벨, phase를 검증한다. Evidence `<attemptDir>/task-1-experience-gauge-level-ui.json`.
    - Failure: 레벨 토큰 없는 100 XP, 음수 delta, 예상과 다른 권위 스냅샷을 입력해 무한 대기나 잘못된 레벨 표시 없이 Idle 권위 상태로 복구하는지 검증한다. Evidence `<attemptDir>/task-1-experience-gauge-level-ui-failure.json`.
  - Commit: Y | `feat(ui): add deterministic experience progress presentation`

- [x] 2. 권위 경험치·레벨 이벤트 순서와 즉시 게임플레이 레벨업을 회귀 테스트로 고정한다
  - What to do:
    - `Assets/Tests/EditMode/ExperienceCrystalTests.cs`에 `OnExperienceAdded`가 `GameManager.OnPlayerLevelChanged`보다 먼저 발생하고, `OnLevelGained`가 실제 레벨 변경 뒤 발생한다는 계약 테스트를 추가한다.
    - 95 XP에서 17 XP를 추가해 메서드가 반환되기 전에 `GameManager.CurrentPlayerLevel == 2`, `PlayerExperience.CurrentExperience == 12`가 됨을 확인한다.
    - 312 XP 기존 테스트를 유지해 레벨 4, 잔여 12, gained level `[2,3,4]`를 계속 보호한다.
  - Must NOT do:
    - 이벤트 발행 순서를 맞추기 위해 `PlayerExperience.cs`, `GameManager.cs`, `PlayerLevelStats.cs`를 수정하지 않는다.
    - `OnExperienceChanged`가 레벨 처리 중 여러 번 발생하는 현재 내부 세부 순서 전체를 고정하지 않는다. UI가 의존하는 `OnExperienceAdded before level change`와 최종 권위 상태만 고정한다.
  - Parallelization: Wave 1 | Blocked by: — | Blocks: 4
  - References:
    - `Assets/Scripts/Player/PlayerExperience.cs:90-115` — `OnExperienceAdded` 후 `ProcessLevelUp`.
    - `Assets/Scripts/Player/PlayerExperience.cs:167-202` — XP 차감, `GameManager.LevelUp`, `OnLevelGained`.
    - `Assets/Scripts/GameManager.cs:221-247` — 권위 레벨 변경과 이벤트.
    - `Assets/Scripts/Player/PlayerLevelStats.cs:121-150` — 실제 레벨 이벤트를 즉시 소비하는 게임플레이 경로.
    - `Assets/Tests/EditMode/ExperienceCrystalTests.cs:71-119` — 312 XP 기존 회귀.
  - Acceptance criteria:
    - 새 이벤트 계약 테스트와 기존 `ExperienceCrystalTests` 7개 이상이 모두 통과한다.
    - 테스트는 `AddExperience` 반환 직후 권위 레벨/XP가 이미 최종값임을 확인하며 UI 애니메이션 시간을 기다리지 않는다.
    - `git diff -- Assets/Scripts/Player/PlayerExperience.cs Assets/Scripts/GameManager.cs Assets/Scripts/Player/PlayerLevelStats.cs`가 비어 있다.
  - QA scenarios:
    - Happy: 95+17 및 312 XP를 각각 실행해 이벤트 핵심 순서와 최종 권위 상태를 확인한다. Evidence `<attemptDir>/task-2-experience-gauge-level-ui.json`.
    - Failure: `AddExperience(0)`과 음수 입력이 레벨 이벤트나 gained 이벤트를 발생시키지 않는 기존 방어를 함께 확인한다. Evidence `<attemptDir>/task-2-experience-gauge-level-ui-failure.json`.
  - Commit: Y | `test(experience): lock authoritative level event contract`

- [x] 3. Stage1 경험치 Filler만 불투명 초록색 왼쪽 채움으로 고정하고 씬 테스트를 추가한다
  - What to do:
    - Unity Editor에서 `Assets/Scenes/Stage1_Scene.unity`를 열고 정확히 `Canvas/Exp_UI/Bar_1/Filler`의 `UnityEngine.UI.Image`만 수정한다.
    - scene instance override의 color를 RGBA `(0.5568628, 0.7725490, 0.2901961, 1.0)` (`#8EC54A`)로 설정하고 `type=Filled`, `fillMethod=Horizontal`, `fillOrigin=Left`, `fillClockwise=true`를 확인한다.
    - 원본 `Bar_1.prefab`은 그대로 두고 씬 instance override만 저장한다.
    - `Assets/Tests/EditMode/ExperienceProgressSceneTests.cs`를 추가한다. 사용자의 카메라 작업과 열린 씬 상태를 건드리지 않도록 Stage1을 열거나 복구하지 않고, 씬 YAML의 `UIManager` `experienceGauge`/`levelText` 참조와 Filler prefab override 색/타입/방향 계약을 읽기 전용으로 검증한다.
  - Must NOT do:
    - 근처 다른 녹색 Image나 `Canvas/Exp_UI/Bar_1/Title`, 원본 `Assets/Free UI build package/Prefabs/Gray/Bars/Bar_1.prefab`을 수정하지 않는다.
    - 새 sprite/material을 만들거나 `Assets/Sprites/UI/gauge.png`로 교체하지 않는다.
    - Stage1을 테스트에서 열기·저장·reload하거나 카메라/Cinemachine/`MentalCameraShake` 직렬화를 정규화하지 않는다.
  - Parallelization: Wave 1 | Blocked by: — | Blocks: F1-F4
  - References:
    - `Assets/Scenes/Stage1_Scene.unity:2991-2992` — UIManager의 experienceGauge/levelText 참조.
    - `Assets/Scenes/Stage1_Scene.unity:12426-12448` — 대상 Filler prefab override.
    - `Assets/Free UI build package/Prefabs/Gray/Bars/Bar_1.prefab:80-106` — Filled/Horizontal 원본 구조.
    - `Assets/Scripts/UI/UIManager.cs:363-400` — fillAmount와 Level Text 소비 지점.
  - Acceptance criteria:
    - `ExperienceProgressSceneTests`가 color RGB 허용오차 0.001, alpha 정확히 1, `Image.Type.Filled`, `Image.FillMethod.Horizontal`, `fillOrigin == 0`을 통과한다.
    - `UIManager.experienceGauge`는 `Canvas/Exp_UI/Bar_1/Filler`, `levelText`는 `Canvas/Exp_UI/Text (TMP)`로 해석된다.
    - `git diff -- Assets/Free UI build package/Prefabs/Gray/Bars/Bar_1.prefab`가 비어 있고 씬 diff에는 대상 Filler override 외 무관한 serialization churn이 없다.
  - QA scenarios:
    - Happy: `run_tests(... test_names=["ExperienceProgressSceneTests"])`와 Unity component resource 조회로 초록색/불투명/왼쪽 horizontal fill을 확인한다. Evidence `<attemptDir>/task-3-experience-gauge-level-ui.json`.
    - Failure: 테스트가 원본 prefab이 바뀌거나 UIManager reference가 null이거나 alpha가 1 미만이면 실패하도록 확인한다. Evidence `<attemptDir>/task-3-experience-gauge-level-ui-failure.json`.
  - Commit: Y | `style(ui): tint stage experience gauge green`

- [x] 4. UIManager에 표시 상태를 연결해 full 이후에만 레벨 텍스트를 바꾸고 모든 수명주기 경로를 재동기화한다
  - What to do:
    - `Assets/Scripts/UI/UIManager.cs`의 Experience & Level 구역에 inspector 기본값 `fullBarDuration=0.35f`, `minimumSegmentDuration=0.08f`, `fullHoldDuration=0.10f`를 추가하고 Todo 1의 표시 상태에 전달한다.
    - `TryBindProgressUI`/`UnbindProgressUI`에서 기존 이벤트와 함께 `PlayerExperience.OnExperienceAdded`, `PlayerExperience.OnLevelGained`을 정확히 한 번 구독/해제한다.
    - `OnExperienceAdded` 수신 즉시 표시 상태를 active로 만들고 amount를 큐잉한다. 이 이벤트는 `GameManager.OnPlayerLevelChanged`보다 먼저 오므로, 같은 호출 스택에서 뒤이어 오는 권위 레벨 이벤트가 HUD 텍스트를 먼저 바꾸지 못하게 한다.
    - active 중 `HandlePlayerLevelChanged`와 `HandleExperienceChanged`는 Image/TMP를 직접 쓰지 않고 최신 권위 스냅샷만 기록한다. idle 중 외부 `SetPlayerLevel`, `SetExperience`, `ResetExperience`는 즉시 표시와 상태를 동기화한다.
    - `OnLevelGained`은 레벨 토큰을 큐에 추가한다. 매 `Update()`에서 다른 HUD 갱신과 독립적으로 `AdvanceExperiencePresentation(Time.unscaledDeltaTime)`을 한 번 호출한다.
    - `AdvanceExperiencePresentation(float deltaTime)`를 결정론적 테스트 seam으로 두고, 반환된 frame에 따라 `experienceGauge.fillAmount`를 쓴다. 레벨 변경 flag는 반드시 같은 frame의 fill이 1f인 것을 먼저 적용/검증한 뒤 `UpdateLevel`을 호출한다. hold 종료 frame에서 0으로 reset하고 overflow 구간을 시작한다.
    - 초기 바인딩, `HandleStageStarted`, `OnDisable`/재바인딩에서는 큐를 폐기하고 `PlayerExperience.CurrentExperience`, `RequiredExperience`, `GameManager.CurrentPlayerLevel`로 재동기화한다. direct 변경이 active 큐의 예상 최종 상태와 다르면 다음 `Update`에서 애니메이션을 취소하고 최신 권위 상태를 표시한다.
    - `Assets/Tests/EditMode/ExperienceProgressUITests.cs`를 추가해 실제 `GameManager`, `PlayerExperience`, `UIManager`, `Image`, TMP 텍스트를 구성하고 private serialized fields/methods는 기존 프로젝트 관례처럼 reflection으로 주입/호출한다.
  - Must NOT do:
    - `GameManager.OnPlayerLevelChanged` 발행이나 `PlayerLevelStats` 적용을 UI 애니메이션 뒤로 미루지 않는다.
    - `HandleExperienceChanged`를 제거해 직접 설정/리셋 동기화 경로를 깨뜨리지 않는다.
    - active 중 새 gain이 오면 현재 연출을 재시작하거나 대기량을 덮어쓰지 않는다.
    - component 비활성화/재활성화 시 중복 구독을 남기지 않는다.
  - Parallelization: Wave 2 | Blocked by: 1, 2 | Blocks: F1-F4
  - References:
    - `Assets/Scripts/UI/UIManager.cs:22-29` — serialized gauge/text.
    - `Assets/Scripts/UI/UIManager.cs:129-155` — Start/Update/OnDisable 수명주기.
    - `Assets/Scripts/UI/UIManager.cs:206-215` — 초기 progress 표시.
    - `Assets/Scripts/UI/UIManager.cs:244-328` — 기존 bind/unbind 소유권.
    - `Assets/Scripts/UI/UIManager.cs:330-346` — 현재 즉시 반영 handlers.
    - `Assets/Scripts/UI/UIManager.cs:363-400` — fill/text writers.
    - `Assets/Scripts/Player/PlayerExperience.cs:29-37` — 세 경험치 이벤트 계약.
    - `Assets/Scripts/Player/PlayerExperience.cs:90-115,160-203,241-256` — 실제 동기 이벤트 순서와 중간/final OnExperienceChanged.
    - `Assets/Scripts/GameManager.cs:174-183,221-257` — stage start와 권위 level events.
  - Acceptance criteria:
    - `ExperienceProgressUITests`가 다음 중간 순서를 수치로 확인한다.
      - 95+5 호출 직후 권위 level은 2지만 TMP는 `Lv. 1`; fill<1 동안 TMP는 계속 `Lv. 1`; fill==1인 frame에만 `Lv. 2`; hold 이후 fill==0.
      - 95+17은 위 순서 후 최종 fill 0.12와 `Lv. 2`.
      - 312는 세 번의 full frame에서만 `Lv. 2`, `Lv. 3`, `Lv. 4`로 순차 변경되고 최종 fill 0.12.
      - 같은 frame 여러 `AddExperience`와 active animation 중 추가 호출 모두 최종 권위/표시 상태가 일치함.
      - idle `SetExperience`/`ResetExperience`, `HandleStageStarted`, disable→enable 재바인딩이 대기 큐 없이 즉시 권위 값과 일치함.
      - 두 번 bind/unbind 후 gain 하나당 handler 효과가 한 번만 발생함.
    - `ExperienceProgressUITests`, `ExperienceProgressPresentationTests`, `ExperienceCrystalTests`가 모두 통과하고 Unity console에 새 Error/Exception이 없다.
  - QA scenarios:
    - Happy: reflection으로 `AdvanceExperiencePresentation`에 경계 직전/도달/hold 종료 delta를 넣어 fill과 TMP의 정확한 프레임 순서를 검증한다. Evidence `<attemptDir>/task-4-experience-gauge-level-ui.json`.
    - Failure: active 중 `SetExperience`로 예상 최종 상태를 변경하고 stage start/disable을 발생시켜 stale queue가 재생되지 않고 권위 값으로 복구하는지 확인한다. Evidence `<attemptDir>/task-4-experience-gauge-level-ui-failure.json`.
  - Commit: Y | `feat(ui): sequence experience gauge before level text`

## Final verification wave
> 사용자 지시로 이 섹션(F1~F4), 반복 테스트, Play Mode 증거 수집, Global Review/Debugging Gate는 실행하지 않는다. 검증은 사용자가 직접 수행한다.
> Runs in parallel after ALL todos. ALL must APPROVE. Surface results and wait for the user's explicit okay before declaring complete.
- [ ] F1. Plan compliance audit
  - 승인 초안과 이 계획의 Must have/Must NOT have를 실제 diff에 대조한다.
  - `95+5`, `95+17`, `312`, same-frame gain, active-animation gain의 자동 테스트 이름과 assertion을 직접 확인한다.
  - 권위 게임플레이 레벨은 즉시 변하고 HUD 텍스트만 full까지 지연된다는 assertion이 없으면 거절한다.
  - Evidence `<attemptDir>/final-F1-plan-compliance.md`.

- [ ] F2. Code quality and automated regression review
  - `ExperienceProgressPresentation`의 단일 상태 소유권, 유한 상태 전이, 큐 boundedness, 이벤트 구독 대칭, float clamp, mismatch fallback을 검토한다.
  - Unity MCP `run_tests(mode="EditMode", assembly_names=["LittleRed.Tests.EditMode"], include_details=true, include_failed_tests=true)`를 실행해 전체 어셈블리 실패 0을 확인한다.
  - compilation 종료 후 `read_console(types=["Error","Warning"], count=200)`에서 이번 변경으로 생긴 오류/예외/중복 구독 경고가 없음을 확인한다.
  - Evidence `<attemptDir>/final-F2-code-quality.json`.

- [ ] F3. Real Stage1 Play Mode QA
  - Unity MCP의 exact resource URI를 목록에서 확인하고 `mcpforunity://custom-tools`, instances, editor state를 읽은 뒤 해당 instance를 고정한다.
  - 사용자가 열어 둔 Stage1과 카메라 작업을 그대로 보존하고 scene load/reload/save 없이 `manage_editor(action="play")`로 실행한다. Play 전후 Stage1 disk hash를 비교하되 차이가 생겨도 카메라 관련 내용을 자동 복원하지 않고 즉시 중단·보고한다.
  - `execute_code`로 runtime-only setup을 수행한다: `PlayerExperience.SetExperience(95)`, 플레이어 위치에 `Assets/Sprites/Item/ExpCrystal_low.prefab`을 instantiate해 실제 trigger로 +5를 수집한다. `GameManager.CurrentPlayerLevel`, `CurrentExperience`, Filler `fillAmount`, TMP text를 phase별로 반환해 exact-threshold 순서를 기록한다.
  - 새로 추가한 hold duration serialized field를 runtime reflection으로 1.0초에만 임시 설정해 full 상태에서 `manage_camera(action="screenshot", capture_source="game_view", include_image=true)`로 초록색 full bar와 갱신된 Level Text를 캡처한다. scene은 저장하지 않는다.
  - overflow는 XP 95에서 low(+5)와 high(+12)를 같은 frame 또는 첫 연출 active 중 플레이어 위치에 생성해 최종 level 2, XP 12, fill 0.12를 확인한다.
  - multi-level은 runtime `AddExperience(312)`로 full/text/reset이 세 번 발생하고 최종 level 4, XP 12, fill 0.12인지 반환 로그로 확인한다.
  - 방향은 시작/중간/final game-view screenshots에서 Filler의 왼쪽 edge는 고정되고 오른쪽 edge만 전진하는지 비교한다.
  - Play Mode를 stop하고 console Error/Exception 0과 scene dirty/save prompt 없음까지 확인한다.
  - Evidence `<attemptDir>/final-F3-playmode.json`, `<attemptDir>/final-F3-left-mid-full.png`, `<attemptDir>/final-F3-overflow.png`.

- [ ] F4. Scope fidelity and asset integrity
  - 경험치 UI 소유 변경은 계획된 `UIManager.cs`, 새 presentation 파일, 세 EditMode 테스트 파일, `Stage1_Scene.unity`의 Filler override로 한정한다. 사용자가 소유한다고 명시한 카메라/Cinemachine/`MentalCameraShake` 변경은 보존하고 경험치 UI 커밋에서 제외한다.
  - `git diff -- Assets/Scripts/Player/PlayerExperience.cs Assets/Scripts/GameManager.cs Assets/Scripts/Player/PlayerLevelStats.cs "Assets/Free UI build package/Prefabs/Gray/Bars/Bar_1.prefab"`가 비어 있음을 확인한다.
  - scene diff에서 경험치 UI가 추가한 내용은 `Canvas/Exp_UI/Bar_1/Filler` override뿐임을 확인한다. 카메라/Cinemachine/`MentalCameraShake` 관련 diff는 사용자 소유로 분리해 수정하거나 제거하지 않는다.
  - 새 이미지/셰이더/머티리얼/package/PlayMode asmdef가 없음을 확인한다.
  - Evidence `<attemptDir>/final-F4-scope-fidelity.md`.

## Commit strategy
- Todo 1: `feat(ui): add deterministic experience progress presentation`
- Todo 2: `test(experience): lock authoritative level event contract`
- Todo 3: `style(ui): tint stage experience gauge green`
- Todo 4: `feat(ui): sequence experience gauge before level text`
- 각 커밋은 해당 todo의 테스트와 코드/씬만 포함한다. 사용자의 기존 변경이 발견되면 stage하지 않고 evidence에 기록한다.
- Final verification은 코드 변경 없이 검증 결과만 남긴다. 검토 중 수정이 필요하면 원래 todo 커밋에 fixup하고 관련 targeted/full 테스트를 다시 실행한다.

## Success criteria
- 실제 `ExpCrystal` 획득이 기존과 같은 즉시 권위 XP/레벨/스탯 경로를 사용한다.
- 경험치 게이지는 불투명 초록색이고 왼쪽에서 오른쪽으로 채워진다.
- HUD Level Text는 게이지가 100%에 도달한 frame 이전에는 절대 증가하지 않는다.
- 100% frame에 Level Text가 한 단계 증가하고, full hold/reset 뒤에만 초과 경험치가 다음 바에 나타난다.
- 여러 레벨과 연속 획득에서도 각 full마다 레벨이 정확히 한 번 증가하고 최종 XP/level/fill이 권위 상태와 일치한다.
- 비활성화, 재바인딩, 스테이지 시작, 직접 XP 변경 뒤 stale animation이 남지 않는다.
- 대상/전체 EditMode 테스트가 모두 통과하고 Unity console에 새 오류가 없다.
- Stage1 Play Mode 실제 크리스탈 QA와 시각 증거가 방향·색상·순서를 확인한다.
- 계획 밖 제품 파일, 무료 UI 원본 prefab, 경험치 규칙, 패키지는 변경되지 않는다.
