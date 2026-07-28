---
slug: experience-gauge-level-ui
status: plan-complete
intent: clear
review_required: false
pending-action: user chooses start-work or high-accuracy review
approach: Preserve the authoritative XP/level rules, add a UI-only presentation sequencer that visibly fills to 100% before changing Level Text and then carries overflow into the next bar, tint only the Stage1 Filler green, and verify event ordering with deterministic EditMode tests plus Play Mode QA.
---

# Draft: experience-gauge-level-ui

## Components (topology ledger)
<!-- Lock the SHAPE before depth. One row per top-level component that can succeed or fail independently. -->
<!-- id | outcome (one line) | status: active|deferred | evidence path -->
C1 | ExpCrystal pickup increases PlayerExperience and crossing 100 XP increments GameManager level while preserving overflow XP | active | Assets/Scripts/Item/ExperienceCrystalPickup.cs:53; Assets/Scripts/Player/PlayerExperience.cs:90; Assets/Scripts/GameManager.cs:221
C2 | UIManager presents each threshold in strict visual order: fill to 100%, update Level Text, reset, then fill carried overflow; repeated for multi-level gains | active | Assets/Scripts/UI/UIManager.cs:136; Assets/Scripts/UI/UIManager.cs:244; Assets/Scripts/Player/PlayerExperience.cs:90
C3 | The existing Bar_1 Filler renders left-to-right with an opaque green tint and no new art dependency | active | Assets/Free UI build package/Prefabs/Gray/Bars/Bar_1.prefab:80; Assets/Scenes/Stage1_Scene.unity:12434
C4 | Automated EditMode checks plus agent-driven Play Mode QA prove ordering, exact threshold, overflow, multi-level, and rapid-pickup behavior | active | Assets/Tests/EditMode/ExperienceCrystalTests.cs:71; Assets/Tests/EditMode/LittleRed.Tests.EditMode.asmdef:1

## Open assumptions (announced defaults)
<!-- Record any default you adopt instead of asking, so the user can veto it at the gate. -->
<!-- assumption | adopted default | rationale | reversible? -->
XP threshold | Keep fixed 100 XP per level | PlayerExperience declares this contract and the passing test pins it | yes
Crystal rewards | Keep low/medium/high at 5/8/12 XP | Prefabs and tests already agree | yes
Gauge motion | Animate at 0.35 seconds per full bar, with a minimum 0.08-second visible segment and a 0.10-second hold at full; use Time.unscaledDeltaTime | Makes the required ordering perceptible while staying independent of game time scale | yes
Level display timing | Defer only the HUD Level Text update until the presented fill reaches exactly 1.0; keep authoritative gameplay level/stat updates immediate | Satisfies the visual contract without changing game rules or combat timing | yes
Concurrent pickups | Queue XP additions and gained levels while a presentation is active instead of cancelling the current sequence | Prevents dropped, duplicated, or reordered presentation during rapid pickups | yes
Gauge art | Reuse Canvas/Exp_UI/Bar_1/Filler and Bars_Unity.png | Live Unity data proves it is already Image.Type Filled, Horizontal, left-origin | yes
Gauge color | Use the existing scene palette green RGBA approximately (0.556, 0.773, 0.290, 1), hex #8EC54A | Stage1 already uses this opaque green elsewhere; it is distinct from the current cyan, half-alpha Filler | yes
Test strategy | Deterministic EditMode ordering tests followed by Unity Play Mode QA | Existing EditMode infrastructure is established and the request is user-visible | yes

## Findings (cited - path:lines)
- The requested runtime chain already exists: ExperienceCrystalPickup.TryCollect calls PlayerExperience.AddExperience (Assets/Scripts/Item/ExperienceCrystalPickup.cs:53-68).
- PlayerExperience accumulates XP, processes one or more 100-XP level-ups through GameManager.LevelUp, retains overflow, and publishes OnExperienceChanged (Assets/Scripts/Player/PlayerExperience.cs:90-115,160-203,251-256).
- The current event order does not guarantee the requested visual order: AddExperience invokes OnExperienceAdded, ProcessLevelUp invokes GameManager.OnPlayerLevelChanged, and only afterward OnExperienceChanged publishes the final remainder (Assets/Scripts/Player/PlayerExperience.cs:90-115,160-203,241-256).
- GameManager is the current-level source and publishes OnPlayerLevelChanged (Assets/Scripts/GameManager.cs:41-59,221-247).
- UIManager subscribes to PlayerExperience.OnExperienceChanged and GameManager.OnPlayerLevelChanged, then writes Image.fillAmount and "Lv. {level}" (Assets/Scripts/UI/UIManager.cs:244-300,330-400).
- Therefore the existing HUD can change Level Text first and jump directly from the old partial fill to the post-level remainder; it may never render the full 100% frame. A presentation sequencer is required for the user's stricter visible ordering.
- Stage1 already serializes non-null experienceGauge and levelText references (Assets/Scenes/Stage1_Scene.unity:2965-2993).
- Live Unity inspection resolves experienceGauge to Canvas/Exp_UI/Bar_1/Filler and levelText to Canvas/Exp_UI/Text (TMP); the Filler is a UnityEngine.UI.Image with type=Filled, fillMethod=Horizontal, fillOrigin=Left, fillAmount=0.
- The current Filler color is cyan and semi-transparent, approximately RGBA (0.292, 1.0, 0.989, 0.482), so the visual request is not yet met (live Unity component resource for Canvas/Exp_UI/Bar_1/Filler; scene overrides at Assets/Scenes/Stage1_Scene.unity:12434-12448).
- Bar_1 already uses the grayscale Bars_Unity.png atlas, which is suitable for tinting (Assets/Free UI build package/Prefabs/Gray/Bars/Bar_1.prefab:80-106).
- Assets/Sprites/UI/gauge.png is a circular ornamental gauge and is less suitable than the already-wired horizontal Bar_1 Filler.
- Unity reflection confirms UnityEngine.UI.Image exposes writable fillAmount, fillMethod, and type properties; no new texture or custom shader is required.
- Current ExperienceCrystalTests pass 7/7, covering fixed threshold, overflow/multiple levels, prefab XP values, pickup, and monster drop; they do not directly cover UIManager updates or scene UI wiring (Assets/Tests/EditMode/ExperienceCrystalTests.cs:71-277).
- The project has only an EditMode test assembly; there is no PlayMode test assembly (Assets/Tests/EditMode/LittleRed.Tests.EditMode.asmdef:1-13).
- The Build Settings contain MainMenu and Stage1_Scene; the requested HUD exists in Stage1_Scene.
- The worktree was clean during planning, so there is no currently observed product-file collision risk.

## Decisions (with rationale)
- Do not reimplement the XP or level system. Preserve its immediate authoritative updates and add sequencing only to UIManager's displayed state.
- Subscribe UIManager to PlayerExperience.OnExperienceAdded and OnLevelGained in addition to the existing authoritative synchronization events. Mark presentation active synchronously on XP addition, defer HUD level text changes while active, and queue the gained-level values.
- Advance a small deterministic UI state machine from UIManager.Update using Time.unscaledDeltaTime. Its phases are Idle, Filling, and HoldingFull.
- For every crossed threshold, drive fillAmount to exactly 1.0, then apply the queued Level Text value, hold the full bar briefly, reset to 0, and continue with the carried remainder. Repeat the same sequence for multiple level gains.
- If another crystal is collected mid-sequence, append its XP/level changes to the queue. On disable or unbind, clear presentation state and resynchronize the gauge and Level Text from PlayerExperience and GameManager.
- Change the scene instance override, not the source Bar_1 prefab, so unrelated bars from the free UI package are not recolored.
- Expose or extract a deterministic internal presentation advance that accepts an explicit delta-time value, so EditMode tests can assert intermediate ordering without sleeps.
- Verify intermediate ordering, not only end state: Level Text stays unchanged below full; it increments when fill reaches 1.0; only after the full hold/reset does overflow appear.
- Use Unity Play Mode manual QA driven by the agent for the real trigger, event binding, left-to-right green fill, threshold ordering, overflow, and rapid-pickup behavior; do not add a new PlayMode test assembly solely for this presentation change.

## Scope IN
- Assets/Scripts/UI/UIManager.cs presentation state, XP/level event binding, queued threshold sequencing, and authoritative resynchronization.
- Assets/Scenes/Stage1_Scene.unity experience Filler color/alpha override only.
- A focused Assets/Tests/EditMode/ExperienceProgressUITests.cs regression suite for exact-threshold, overflow, multi-level, and rapid-consecutive-pickup ordering.
- Existing ExperienceCrystalTests execution.
- Unity Play Mode QA using ExpCrystal pickups for 95+5, 95+17, multi-level, and consecutive-pickup scenarios.
- Console/compilation checks and visual evidence capture during implementation QA.

## Scope OUT (Must NOT have)
- No replacement of PlayerExperience/GameManager architecture.
- No change to authoritative level-up timing, the 100-XP threshold, 5/8/12 crystal rewards, level stat growth, or monster drop configuration.
- No new raster art, shader, material, tween package, or third-party dependency.
- No global modification of Assets/Free UI build package/Prefabs/Gray/Bars/Bar_1.prefab.
- No XP numbers inside the bar, level-up VFX/audio, persistence/save-system work, or HUD redesign.
- No new PlayMode test assembly.

## Open questions
None blocking. The announced timing defaults are reversible and can be vetoed at approval.

## Approval gate
status: approved-and-planned
brief: Preserve gameplay XP logic, add a UI-only sequencer for fill-to-100% then Level Text then overflow, queue multi-level and rapid pickups, recolor only the Stage1 Filler override to opaque #8EC54A, and verify exact visual ordering with deterministic EditMode tests plus real Play Mode QA.
next_action: Plan created at .omo/plans/experience-gauge-level-ui.md. User chooses separate execution via $start-work or optional high-accuracy plan review; do not implement in this planning session.
<!-- When exploration is exhausted and unknowns are answered, set status: awaiting-approval. -->
<!-- That durable record is the loop guard: on a later turn read it and resume at the gate instead of re-running exploration. -->
