# Mental Volume Effects Refactor

## TL;DR
> Summary:      `GlobalVolumeManager`는 Singleton coordinator로 유지하고, 정신력 기반 후처리 책임을 `MentalVolumeEffect`, `MentalVignetteEffect`, `MentalFilmGrainEffect` 형제 컴포넌트로 분리해. 현재 워킹트리에는 일부 구현이 이미 있으니 실행자는 중복 구현이 아니라 요구사항 감사, 라이프사이클 방어, 씬 연결, 검증 보강을 완료해.
> Deliverables:
> - `GlobalVolumeManager` coordinator 구조
> - `MentalVolumeEffect` 추상 컴포넌트와 Vignette/FilmGrain 효과 컴포넌트
> - EditMode 테스트와 Stage1 씬 직렬화 연결
> - `.omo/evidence/` 검증 산출물
> Effort:       Short
> Risk:         Medium - Unity 씬 YAML과 런타임 `Volume.profile` 복제 동작을 잘못 다루면 shared profile이나 무관한 씬 변경이 섞일 수 있어

## Scope
### Must have
- `GlobalVolumeManager`는 `Singleton<GlobalVolumeManager>`를 유지하고 `PlayerMental.OnMentalChanged`를 한 번만 구독/해제해.
- `GlobalVolumeManager`는 `Volume.profile`에서 런타임 프로필을 얻고 `GetComponents<MentalVolumeEffect>()`로 같은 GameObject의 효과들을 초기화/적용해.
- `MentalVolumeEffect`는 `Initialize(VolumeProfile runtimeProfile)`와 `Apply(float dangerRatio)` 계약을 제공해.
- `MentalVignetteEffect`는 기존 Vignette 동작을 유지해: danger `0..1` -> intensity `0..0.88`.
- `MentalFilmGrainEffect`는 Film Grain intensity만 danger `0..1` -> `0..0.5`로 조절해.
- Film Grain `type=8`, `response=0.8`은 profile 소유 설정으로 보존하고 런타임 코드가 바꾸지 않아.
- `Stage1_Scene`의 `GlobalVolumeManager` GameObject에 `GlobalVolumeManager`, `MentalVignetteEffect`, `MentalFilmGrainEffect`가 같이 붙어 있어야 해.
- shared profile asset은 런타임 적용으로 변하지 않아야 해.

### Must NOT have (guardrails, anti-slop, scope boundaries)
- `Volume.sharedProfile`을 런타임 효과 적용 대상으로 직접 수정하지 마.
- `PlayerMental.OnMentalChanged` 시그니처나 `PlayerMental`의 정신력 계산 계약을 바꾸지 마.
- Film Grain `response`, `type`, `texture`를 효과 컴포넌트에서 덮어쓰지 마.
- PlayMode 테스트 인프라를 새로 만들지 마. 현재 검증은 EditMode 중심으로 끝내.
- 무관한 몬스터, UI, 오디오, 프리팹, 위치 변경을 이 리팩터 커밋에 섞지 마.
- `GlobalVolumeManager`를 제거하거나 Singleton 책임을 다른 객체로 옮기지 마.

## Verification strategy
> Zero human intervention - all verification is agent-executed.
- Test decision: tests-after + Unity Test Framework EditMode
- QA policy: every task has agent-executed scenarios
- Evidence: `.omo/evidence/task-<N>-<slug>.<ext>`

## Execution strategy
### Parallel execution waves
> Target 5-8 tasks per wave. <3 per wave (except final) = under-splitting.
> Extract shared dependencies as Wave-1 tasks to maximize parallelism.

Wave 1 (no dependencies):
- Task 1: Lock the `MentalVolumeEffect` contract
- Task 2: Complete `MentalVignetteEffect`
- Task 3: Complete `MentalFilmGrainEffect`

Wave 2 (after Wave 1):
- Task 4: depends [1, 2, 3]
- Task 5: depends [1, 2, 3]

Wave 3 (after Wave 2):
- Task 6: depends [4, 5]

Critical path: Task 1 -> Task 4 -> Task 6

### Dependency matrix
| Task | Depends on | Blocks | Can parallelize with |
|------|------------|--------|----------------------|
| 1    | none       | 4, 5   | 2, 3                 |
| 2    | none       | 4, 5   | 1, 3                 |
| 3    | none       | 4, 5   | 1, 2                 |
| 4    | 1, 2, 3    | 6      | 5                    |
| 5    | 1, 2, 3    | 6      | 4                    |
| 6    | 4, 5       | none   | none                 |

## Todos
> Implementation + Test = ONE task. Never separate.
> Every task MUST have: References + Acceptance Criteria + QA Scenarios + Commit.

- [ ] 1. Lock the `MentalVolumeEffect` contract

  What to do: Keep or create `Assets/Scripts/Volume/MentalVolumeEffect.cs` as an abstract `MonoBehaviour` with only `Initialize(VolumeProfile runtimeProfile)` and `Apply(float dangerRatio)`. Make concrete effects depend on this contract, not on `GlobalVolumeManager` internals.
  Must NOT do: Do not add player lookup, event subscription, or concrete Vignette/FilmGrain state to the base class.

  Parallelization: Can parallel: YES | Wave 1 | Blocks: [4, 5] | Blocked by: []

  References (executor has NO interview context - be exhaustive):
  - Pattern:  `Assets/Scripts/Volume/MentalVolumeEffect.cs:4` - current base class is a `MonoBehaviour`
  - API/Type: `Assets/Scripts/Volume/MentalVolumeEffect.cs:6` - `Initialize(VolumeProfile runtimeProfile)` contract
  - API/Type: `Assets/Scripts/Volume/MentalVolumeEffect.cs:8` - `Apply(float dangerRatio)` contract
  - Pattern:  `Assets/Scripts/GlobalVolumeManager.cs:93` - coordinator discovers sibling `MentalVolumeEffect` components
  - External: `https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@17.6/api/UnityEngine.Rendering.VolumeProfile.html` - `VolumeProfile` component lookup/add API

  Acceptance criteria (agent-executable only):
  - [ ] `rg -n "public abstract class MentalVolumeEffect|Initialize\\(VolumeProfile runtimeProfile\\)|Apply\\(float dangerRatio\\)" Assets/Scripts/Volume/MentalVolumeEffect.cs` finds all three contract members.
  - [ ] `rg -n "OnMentalChanged|PlayerMental|Vignette|FilmGrain" Assets/Scripts/Volume/MentalVolumeEffect.cs` returns no matches.

  QA scenarios (MANDATORY - task incomplete without these):
  ```
  Scenario: contract shape exists
    Tool:     bash
    Steps:    powershell -NoProfile -Command "New-Item -ItemType Directory -Force .omo/evidence | Out-Null; rg -n 'public abstract class MentalVolumeEffect|Initialize\(VolumeProfile runtimeProfile\)|Apply\(float dangerRatio\)' Assets/Scripts/Volume/MentalVolumeEffect.cs | Tee-Object .omo/evidence/task-1-contract.txt"
    Expected: evidence file contains exactly the base class and both abstract method signatures.
    Evidence: .omo/evidence/task-1-contract.txt

  Scenario: base class has no concrete effect ownership
    Tool:     bash
    Steps:    powershell -NoProfile -Command "rg -n 'OnMentalChanged|PlayerMental|Vignette|FilmGrain' Assets/Scripts/Volume/MentalVolumeEffect.cs > .omo/evidence/task-1-contract-error.txt; if ((Get-Content .omo/evidence/task-1-contract-error.txt).Length -ne 0) { exit 1 }"
    Expected: command exits 0 and evidence file is empty.
    Evidence: .omo/evidence/task-1-contract-error.txt
  ```

  Commit: YES | Message: `refactor(volume): define mental volume effect contract` | Files: [`Assets/Scripts/Volume/MentalVolumeEffect.cs`, `Assets/Scripts/Volume/MentalVolumeEffect.cs.meta`]

- [ ] 2. Complete `MentalVignetteEffect`

  What to do: Move all Vignette intensity responsibility into `MentalVignetteEffect`: serialized `minIntensity`, serialized `maxIntensity`, `OnValidate` clamp, `TryGet(out Vignette)`, initialize intensity to min, and `Mathf.Lerp(minIntensity, maxIntensity, Mathf.Clamp01(dangerRatio))` on apply. Preserve Stage1 values `0` and `0.88`.
  Must NOT do: Do not resolve `PlayerMental`, subscribe events, or touch Film Grain from this component.

  Parallelization: Can parallel: YES | Wave 1 | Blocks: [4, 5] | Blocked by: []

  References (executor has NO interview context - be exhaustive):
  - Pattern:  `Assets/Scripts/Volume/MentalVignetteEffect.cs:8` - serialized minimum intensity field
  - Pattern:  `Assets/Scripts/Volume/MentalVignetteEffect.cs:12` - serialized maximum intensity field
  - Pattern:  `Assets/Scripts/Volume/MentalVignetteEffect.cs:18` - `OnValidate` clamps min/max
  - Pattern:  `Assets/Scripts/Volume/MentalVignetteEffect.cs:38` - uses `runtimeProfile.TryGet(out vignette)`
  - Pattern:  `Assets/Scripts/Volume/MentalVignetteEffect.cs:47` - initializes runtime intensity to min
  - Pattern:  `Assets/Scripts/Volume/MentalVignetteEffect.cs:57` - applies danger ratio with `Mathf.Lerp`
  - Test:     `Assets/Tests/EditMode/GlobalVolumeManagerVignetteTests.cs:8` - current class is `MentalVignetteEffectTests`
  - Test:     `Assets/Tests/EditMode/GlobalVolumeManagerVignetteTests.cs:69` - danger ratio test cases cover `0`, `0.5`, `1`
  - External: `https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/api/UnityEngine.Rendering.Universal.Vignette.html` - URP Vignette component API

  Acceptance criteria (agent-executable only):
  - [ ] `MentalVignetteEffectTests.DangerRatioUpdatesRuntimeVignetteIntensity` passes for `0 -> 0`, `0.5 -> 0.44`, `1 -> 0.88`.
  - [ ] `MentalVignetteEffectTests.InitializationDoesNotModifySharedProfile` proves shared Vignette intensity remains `0.23`.

  QA scenarios (MANDATORY - task incomplete without these):
  ```
  Scenario: vignette danger mapping passes
    Tool:     bash
    Steps:    powershell -NoProfile -Command "$unity='C:\Program Files\Unity\Hub\Editor\6000.3.8f1\Editor\Unity.exe'; & $unity -batchmode -projectPath 'C:\Users\User\Desktop\Once-Upon-a-Lie_LittleRed' -runTests -testPlatform EditMode -testFilter MentalVignetteEffectTests -testResults '.omo/evidence/task-2-vignette.xml' -logFile '.omo/evidence/task-2-vignette.log' -quit; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }"
    Expected: Unity exits 0 and XML contains passing `MentalVignetteEffectTests`.
    Evidence: .omo/evidence/task-2-vignette.xml

  Scenario: missing Vignette fails gracefully
    Tool:     bash
    Steps:    powershell -NoProfile -Command "$unity='C:\Program Files\Unity\Hub\Editor\6000.3.8f1\Editor\Unity.exe'; & $unity -batchmode -projectPath 'C:\Users\User\Desktop\Once-Upon-a-Lie_LittleRed' -runTests -testPlatform EditMode -testFilter MentalVignetteEffectTests.MissingVignetteDoesNotThrow -testResults '.omo/evidence/task-2-vignette-error.xml' -logFile '.omo/evidence/task-2-vignette-error.log' -quit; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }"
    Expected: test passes; log contains the component warning and no exception.
    Evidence: .omo/evidence/task-2-vignette-error.xml
  ```

  Commit: YES | Message: `refactor(volume): extract mental vignette effect` | Files: [`Assets/Scripts/Volume/MentalVignetteEffect.cs`, `Assets/Scripts/Volume/MentalVignetteEffect.cs.meta`, `Assets/Tests/EditMode/GlobalVolumeManagerVignetteTests.cs`]

- [ ] 3. Complete `MentalFilmGrainEffect`

  What to do: Implement or audit `MentalFilmGrainEffect` so it only controls `FilmGrain.intensity`. Use serialized `minIntensity=0`, `maxIntensity=0.5`, clamp in `OnValidate`, `TryGet(out FilmGrain)`, initialize intensity to min, and apply `Mathf.Lerp(minIntensity, maxIntensity, Clamp01(dangerRatio))`.
  Must NOT do: Do not set `filmGrain.response`, `filmGrain.type`, or `filmGrain.texture` in code.

  Parallelization: Can parallel: YES | Wave 1 | Blocks: [4, 5] | Blocked by: []

  References (executor has NO interview context - be exhaustive):
  - Pattern:  `Assets/Scripts/Volume/MentalFilmGrainEffect.cs:8` - serialized minimum intensity field
  - Pattern:  `Assets/Scripts/Volume/MentalFilmGrainEffect.cs:12` - serialized maximum intensity field
  - Pattern:  `Assets/Scripts/Volume/MentalFilmGrainEffect.cs:18` - `OnValidate` clamps min/max
  - Pattern:  `Assets/Scripts/Volume/MentalFilmGrainEffect.cs:38` - uses `runtimeProfile.TryGet(out filmGrain)`
  - Pattern:  `Assets/Scripts/Volume/MentalFilmGrainEffect.cs:47` - initializes runtime intensity to min
  - Pattern:  `Assets/Scripts/Volume/MentalFilmGrainEffect.cs:57` - applies danger ratio with `Mathf.Lerp`
  - Test:     `Assets/Tests/EditMode/MentalFilmGrainEffectTests.cs:68` - danger ratio test cases cover `0`, `0.5`, `1`
  - Test:     `Assets/Tests/EditMode/MentalFilmGrainEffectTests.cs:88` - profile-owned Film Grain settings are preserved
  - External: `https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/api/UnityEngine.Rendering.Universal.FilmGrain.html` - URP FilmGrain component API

  Acceptance criteria (agent-executable only):
  - [ ] `MentalFilmGrainEffectTests.DangerRatioUpdatesRuntimeFilmGrainIntensity` passes for `0 -> 0`, `0.5 -> 0.25`, `1 -> 0.5`.
  - [ ] `rg -n "filmGrain\\.(response|type|texture).*Override|filmGrain\\.(response|type|texture)\\.value" Assets/Scripts/Volume/MentalFilmGrainEffect.cs` returns no matches.

  QA scenarios (MANDATORY - task incomplete without these):
  ```
  Scenario: film grain danger mapping passes
    Tool:     bash
    Steps:    powershell -NoProfile -Command "$unity='C:\Program Files\Unity\Hub\Editor\6000.3.8f1\Editor\Unity.exe'; & $unity -batchmode -projectPath 'C:\Users\User\Desktop\Once-Upon-a-Lie_LittleRed' -runTests -testPlatform EditMode -testFilter MentalFilmGrainEffectTests -testResults '.omo/evidence/task-3-filmgrain.xml' -logFile '.omo/evidence/task-3-filmgrain.log' -quit; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }"
    Expected: Unity exits 0 and XML contains passing `MentalFilmGrainEffectTests`.
    Evidence: .omo/evidence/task-3-filmgrain.xml

  Scenario: profile-owned Film Grain settings are untouched
    Tool:     bash
    Steps:    powershell -NoProfile -Command "rg -n 'filmGrain\.(response|type|texture).*Override|filmGrain\.(response|type|texture)\.value' Assets/Scripts/Volume/MentalFilmGrainEffect.cs > .omo/evidence/task-3-filmgrain-error.txt; if ((Get-Content .omo/evidence/task-3-filmgrain-error.txt).Length -ne 0) { exit 1 }"
    Expected: command exits 0 and evidence file is empty.
    Evidence: .omo/evidence/task-3-filmgrain-error.txt
  ```

  Commit: YES | Message: `feat(volume): add mental film grain effect` | Files: [`Assets/Scripts/Volume/MentalFilmGrainEffect.cs`, `Assets/Scripts/Volume/MentalFilmGrainEffect.cs.meta`, `Assets/Tests/EditMode/MentalFilmGrainEffectTests.cs`]

- [ ] 4. Refactor `GlobalVolumeManager` into coordinator only

  What to do: Keep the Singleton and serialized `Volume`/`PlayerMental` references. Replace direct Vignette fields and methods with `runtimeProfile`, `MentalVolumeEffect[]`, `InitializeEffects`, `ApplyEffects`, and one `OnMentalChanged` subscription. `HandleMentalChanged(currentMental, maxMental)` must compute `dangerRatio = 1f - Clamp01(current / Max(1, max))`. `ApplyCurrentMental` must apply danger `0` when no source or invalid max exists, matching previous full-mental fallback.
  Must NOT do: Do not import `UnityEngine.Rendering.Universal` or keep direct `Vignette`/`FilmGrain` fields in `GlobalVolumeManager`.

  Parallelization: Can parallel: YES | Wave 2 | Blocks: [6] | Blocked by: [1, 2, 3]

  References (executor has NO interview context - be exhaustive):
  - Pattern:  `Assets/Scripts/GlobalVolumeManager.cs:5` - `[DisallowMultipleComponent]`
  - Pattern:  `Assets/Scripts/GlobalVolumeManager.cs:6` - Singleton manager class
  - Pattern:  `Assets/Scripts/GlobalVolumeManager.cs:10` - serialized `Volume globalVolume`
  - Pattern:  `Assets/Scripts/GlobalVolumeManager.cs:11` - serialized `PlayerMental playerMental`
  - Pattern:  `Assets/Scripts/GlobalVolumeManager.cs:13` - `MentalVolumeEffect[]` cache
  - Pattern:  `Assets/Scripts/GlobalVolumeManager.cs:70` - effect initialization entry point
  - Pattern:  `Assets/Scripts/GlobalVolumeManager.cs:112` - binding entry point
  - Pattern:  `Assets/Scripts/GlobalVolumeManager.cs:150` - unbinding entry point
  - Pattern:  `Assets/Scripts/GlobalVolumeManager.cs:177` - `HandleMentalChanged(float currentMental, float maxMental)`
  - API/Type: `Assets/Scripts/Player/PlayerMental.cs:50` - `OnMentalChanged` event is `Action<float, float>`
  - API/Type: `Assets/Scripts/Player/PlayerMental.cs:364` - event sends `CurrentMental`, `MaxMental`
  - External: `https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@14.0/api/UnityEngine.Rendering.Volume.html` - `Volume.profile` is the per-Volume runtime profile; `sharedProfile` mutates the shared asset

  Acceptance criteria (agent-executable only):
  - [ ] `rg -n "Vignette|FilmGrain|UnityEngine.Rendering.Universal|minVignetteIntensity|maxVignetteIntensity|UpdateMentalVignette|InitializeVignette" Assets/Scripts/GlobalVolumeManager.cs` returns no matches.
  - [ ] A coordinator EditMode test proves `ApplyEffects` reaches all active sibling `MentalVolumeEffect` components and skips disabled/null entries without throwing.
  - [ ] A lifecycle EditMode test proves repeated enable/start/bind paths do not duplicate `OnMentalChanged` effects.

  QA scenarios (MANDATORY - task incomplete without these):
  ```
  Scenario: coordinator dispatches danger to sibling effects
    Tool:     bash
    Steps:    powershell -NoProfile -Command "$unity='C:\Program Files\Unity\Hub\Editor\6000.3.8f1\Editor\Unity.exe'; & $unity -batchmode -projectPath 'C:\Users\User\Desktop\Once-Upon-a-Lie_LittleRed' -runTests -testPlatform EditMode -testFilter GlobalVolumeManagerCoordinatorTests -testResults '.omo/evidence/task-4-coordinator.xml' -logFile '.omo/evidence/task-4-coordinator.log' -quit; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }"
    Expected: Unity exits 0; coordinator tests pass.
    Evidence: .omo/evidence/task-4-coordinator.xml

  Scenario: manager has no direct concrete post-processing ownership
    Tool:     bash
    Steps:    powershell -NoProfile -Command "rg -n 'Vignette|FilmGrain|UnityEngine.Rendering.Universal|minVignetteIntensity|maxVignetteIntensity|UpdateMentalVignette|InitializeVignette' Assets/Scripts/GlobalVolumeManager.cs > .omo/evidence/task-4-coordinator-error.txt; if ((Get-Content .omo/evidence/task-4-coordinator-error.txt).Length -ne 0) { exit 1 }"
    Expected: command exits 0 and evidence file is empty.
    Evidence: .omo/evidence/task-4-coordinator-error.txt
  ```

  Commit: YES | Message: `refactor(volume): make global volume manager coordinate effects` | Files: [`Assets/Scripts/GlobalVolumeManager.cs`, `Assets/Tests/EditMode/GlobalVolumeManagerCoordinatorTests.cs`, `Assets/Tests/EditMode/GlobalVolumeManagerCoordinatorTests.cs.meta`]

- [ ] 5. Wire `Stage1_Scene` and the scene Volume Profile

  What to do: On `Assets/Scenes/Stage1_Scene.unity`, keep `GlobalVolumeManager` on the existing `GlobalVolumeManager` GameObject and add sibling `MentalVignetteEffect` and `MentalFilmGrainEffect`. Preserve `globalVolume` reference to the scene `Volume` and `playerMental` reference to the scene `PlayerMental`. On `Assets/Scenes/Stage1_Scene/Global Volume Profile.asset`, keep both `Vignette` and `FilmGrain`; ensure Film Grain has `type: 8`, `intensity: 0`, `response: 0.8`.
  Must NOT do: Do not move the manager to another GameObject, change unrelated scene objects, or remove the existing Vignette/FilmGrain profile entries.

  Parallelization: Can parallel: YES | Wave 2 | Blocks: [6] | Blocked by: [1, 2, 3]

  References (executor has NO interview context - be exhaustive):
  - Pattern:  `Assets/Scenes/Stage1_Scene.unity:1519` - `GlobalVolumeManager` GameObject component list
  - Pattern:  `Assets/Scenes/Stage1_Scene.unity:1542` - serialized `GlobalVolumeManager`
  - Pattern:  `Assets/Scenes/Stage1_Scene.unity:1544` - `globalVolume` reference
  - Pattern:  `Assets/Scenes/Stage1_Scene.unity:1545` - `playerMental` reference
  - Pattern:  `Assets/Scenes/Stage1_Scene.unity:1572` - serialized `MentalFilmGrainEffect`
  - Pattern:  `Assets/Scenes/Stage1_Scene.unity:1573` - Film Grain min intensity
  - Pattern:  `Assets/Scenes/Stage1_Scene.unity:1574` - Film Grain max intensity
  - Pattern:  `Assets/Scenes/Stage1_Scene.unity:1586` - serialized `MentalVignetteEffect`
  - Pattern:  `Assets/Scenes/Stage1_Scene.unity:1588` - Vignette max intensity `0.88`
  - Pattern:  `Assets/Scenes/Stage1_Scene/Global Volume Profile.asset:28` - FilmGrain profile component
  - Pattern:  `Assets/Scenes/Stage1_Scene/Global Volume Profile.asset:31` - FilmGrain type override
  - Pattern:  `Assets/Scenes/Stage1_Scene/Global Volume Profile.asset:34` - FilmGrain intensity override
  - Pattern:  `Assets/Scenes/Stage1_Scene/Global Volume Profile.asset:37` - FilmGrain response override
  - Pattern:  `Assets/Scenes/Stage1_Scene/Global Volume Profile.asset:53` - Vignette profile component

  Acceptance criteria (agent-executable only):
  - [ ] `Select-String` proves `Assembly-CSharp::GlobalVolumeManager`, `Assembly-CSharp::MentalVignetteEffect`, and `Assembly-CSharp::MentalFilmGrainEffect` are present in `Stage1_Scene.unity`.
  - [ ] `Select-String` proves profile lines include `m_Name: FilmGrain`, `m_Value: 8`, `m_Value: 0.8`, and `m_Name: Vignette`.
  - [ ] `git diff -- Assets/Scenes/Stage1_Scene.unity` is reviewed so only requested volume-manager changes are committed; unrelated scene churn is excluded or moved to a separate commit.

  QA scenarios (MANDATORY - task incomplete without these):
  ```
  Scenario: Stage1 manager has both sibling effect components
    Tool:     bash
    Steps:    powershell -NoProfile -Command "Select-String -Path 'Assets/Scenes/Stage1_Scene.unity' -Pattern 'Assembly-CSharp::GlobalVolumeManager','Assembly-CSharp::MentalVignetteEffect','Assembly-CSharp::MentalFilmGrainEffect','maxIntensity: 0.88','maxIntensity: 0.5' | Tee-Object .omo/evidence/task-5-scene.txt; if ((Get-Content .omo/evidence/task-5-scene.txt).Length -lt 5) { exit 1 }"
    Expected: evidence contains all five scene markers.
    Evidence: .omo/evidence/task-5-scene.txt

  Scenario: Film Grain profile-owned settings are serialized unchanged
    Tool:     bash
    Steps:    powershell -NoProfile -Command "Select-String -Path 'Assets/Scenes/Stage1_Scene/Global Volume Profile.asset' -Pattern 'm_Name: FilmGrain','m_Value: 8','m_Value: 0.8','m_Name: Vignette' | Tee-Object .omo/evidence/task-5-profile-error.txt; if ((Get-Content .omo/evidence/task-5-profile-error.txt).Length -lt 4) { exit 1 }"
    Expected: evidence contains FilmGrain, type 8, response 0.8, and Vignette markers.
    Evidence: .omo/evidence/task-5-profile-error.txt
  ```

  Commit: YES | Message: `chore(scene): wire mental volume effects in stage one` | Files: [`Assets/Scenes/Stage1_Scene.unity`, `Assets/Scenes/Stage1_Scene/Global Volume Profile.asset`]

- [ ] 6. Run full EditMode verification and isolate the commit scope

  What to do: Run the full EditMode suite, inspect Unity logs for compile errors/warnings related to this refactor, and verify the final diff only contains the planned files. If unrelated dirty changes exist, leave them untouched and do not include them in the refactor commits.
  Must NOT do: Do not run destructive git commands, do not reset unrelated user changes, and do not claim completion without evidence files.

  Parallelization: Can parallel: NO | Wave 3 | Blocks: [] | Blocked by: [4, 5]

  References (executor has NO interview context - be exhaustive):
  - Test:     `Assets/Tests/EditMode/LittleRed.Tests.EditMode.asmdef:4` - tests reference RenderPipelines Core runtime
  - Test:     `Assets/Tests/EditMode/LittleRed.Tests.EditMode.asmdef:5` - tests reference URP runtime
  - API/Type: `ProjectSettings/ProjectVersion.txt:1` - Unity version is `6000.3.8f1`
  - API/Type: `Packages/manifest.json:17` - URP version is `17.3.0`
  - API/Type: `Packages/manifest.json:18` - Unity Test Framework version is `1.6.0`
  - Pattern:  `Assets/Scripts/GlobalVolumeManager.cs:65` - manager unbinds on disable
  - Pattern:  `Assets/Scripts/GlobalVolumeManager.cs:190` - manager applies effects through coordinator method

  Acceptance criteria (agent-executable only):
  - [ ] Full EditMode test run exits 0.
  - [ ] `git diff --name-only` for the refactor includes only planned code/test/scene/profile files plus `.omo/plans/mental-volume-effects.md`.
  - [ ] Evidence files exist for tasks 1-6.

  QA scenarios (MANDATORY - task incomplete without these):
  ```
  Scenario: full EditMode suite passes
    Tool:     bash
    Steps:    powershell -NoProfile -Command "$unity='C:\Program Files\Unity\Hub\Editor\6000.3.8f1\Editor\Unity.exe'; & $unity -batchmode -projectPath 'C:\Users\User\Desktop\Once-Upon-a-Lie_LittleRed' -runTests -testPlatform EditMode -testResults '.omo/evidence/task-6-editmode.xml' -logFile '.omo/evidence/task-6-editmode.log' -quit; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }"
    Expected: Unity exits 0 and XML reports no failed EditMode tests.
    Evidence: .omo/evidence/task-6-editmode.xml

  Scenario: refactor commit scope excludes unrelated dirty files
    Tool:     bash
    Steps:    powershell -NoProfile -Command "git diff --name-only | Tee-Object .omo/evidence/task-6-scope-error.txt; $allowed=@('.omo/plans/mental-volume-effects.md','Assets/Scripts/GlobalVolumeManager.cs','Assets/Scripts/Volume/MentalVolumeEffect.cs','Assets/Scripts/Volume/MentalVolumeEffect.cs.meta','Assets/Scripts/Volume/MentalVignetteEffect.cs','Assets/Scripts/Volume/MentalVignetteEffect.cs.meta','Assets/Scripts/Volume/MentalFilmGrainEffect.cs','Assets/Scripts/Volume/MentalFilmGrainEffect.cs.meta','Assets/Tests/EditMode/GlobalVolumeManagerVignetteTests.cs','Assets/Tests/EditMode/MentalFilmGrainEffectTests.cs','Assets/Tests/EditMode/GlobalVolumeManagerCoordinatorTests.cs','Assets/Tests/EditMode/GlobalVolumeManagerCoordinatorTests.cs.meta','Assets/Scenes/Stage1_Scene.unity','Assets/Scenes/Stage1_Scene/Global Volume Profile.asset'); $bad=Get-Content .omo/evidence/task-6-scope-error.txt | Where-Object { $allowed -notcontains $_ }; if ($bad) { $bad | Tee-Object .omo/evidence/task-6-unrelated.txt; exit 1 }"
    Expected: command exits 0; if it fails, `.omo/evidence/task-6-unrelated.txt` lists files that must not be included in this refactor commit.
    Evidence: .omo/evidence/task-6-scope-error.txt
  ```

  Commit: YES | Message: `test(volume): verify mental volume effects` | Files: [`Assets/Tests/EditMode/GlobalVolumeManagerCoordinatorTests.cs`, `.omo/evidence/task-6-editmode.xml`, `.omo/plans/mental-volume-effects.md`]

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
- Reference the plan file path in the final commit footer: `Plan: .omo/plans/mental-volume-effects.md`.

## Success criteria
- All Must-Have shipped; all QA scenarios pass with captured evidence; F1-F4 approved; commit history clean.
