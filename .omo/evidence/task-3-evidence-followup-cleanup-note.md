Evidence-only follow-up cleanup note

During evidence release-state capture, Unity had unsaved in-memory camera/Cinemachine drift from another editor interaction. A scene save temporarily serialized those unrelated camera changes. I removed only that drift and restored Stage1 to the pre-follow-up Todo 3 final diff:
- removed `CinemachineBrain` component fileID 519420033 from Main Camera
- removed root `CM_PlayerCamera` object fileID 857896410 / Transform 857896414
- restored Main Camera orthographic size to 5
- restored pre-existing camera root-move local position to `{x: 0.82577, y: -0.35315794, z: -10}`
- preserved XP Filler overrides for target fileID 114361196886809056

Verification:
- `task-3-post-cleanup-stage1-diff.patch` SHA256 matches `task-3-final-stage1-diff.patch` SHA256: 7D492DBDDEC1947CD0BF770EFCB810D6EC0AE5765D720986B35CA4C8CC3031C5
- `rg` found no unwanted markers: 519420033, 857896410, 857896414, CM_PlayerCamera, orthographic size 5.920632, local position x 1.0592777
- Unity release state after external reload: sceneDirty=False, console entries=0, editor ready=true
