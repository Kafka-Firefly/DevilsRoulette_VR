# Devil's Roulette VR Prototype

A focused Meta Quest 2 interaction prototype made with Unity 6.6 and XR Interaction Toolkit 3.6.1. It explores a small part of *Devil's Roulette -The Pit-* through direct object handling, uncertain outcomes, and clear sensory feedback.

For the assessment 2 submission map (what to hand in, how to build, what is still missing), see [SUBMISSION.md](SUBMISSION.md).

## Prototype

Open `Assets/Scenes/ThePit.unity` to view the main scene. The player can pick up a low-poly shotgun, aim at a target, and activate the trigger. A randomized six-chamber round contains one live round and five empty chambers by default.

- `Assets/Scripts/ShotgunController.cs` handles grabbing, trigger activation, randomized chambers, target raycasts, audio, muzzle flash, haptics, and round progression.
- `Assets/Scripts/RouletteRoundDisplay.cs` builds a tilted status placard standing on the table. It shows the round, chamber progress, target hits, the last result, and the controller inputs.
- `Assets/Scripts/TargetHitReceiver.cs` handles the hit response: ring flash, a bullet mark in front of the plate, an impact puff, and a spatial metallic clank.
- `Assets/Scripts/GrabAffordance.cs` warms up the shotgun while a controller hovers it, then plays a short handle click and a haptic pulse on grab and release.
- `Assets/Scripts/GunRecovery.cs` returns the shotgun to the table if it is dropped out of reach - there is no locomotion in the prototype, so a gun under the table would otherwise end the session.
- `Assets/Scripts/PitLampFlicker.cs` adds a subtle intensity drift to the work lamp above the table.

### Feedback model

| Event | Visual | Audio | Haptic |
|---|---|---|---|
| Controller hovers the gun | Base colour warms up | - | - |
| Gun grabbed / released | Highlight clears | Short handle click | 0.28 / 0.12 pulse |
| Empty chamber | Placard shows the chamber count | Dry click | 0.25, 0.07 s |
| Live round | Muzzle flash, recoil, ring flash, bullet mark | Shot, then a metallic clank at the plate | 0.8, 0.16 s |
| Round complete | Placard reads `ROUND COMPLETE` | - | - |
| Gun dropped out of reach | Gun returns to the table after 2.5 s | - | - |

## Scene layout

The room is 6.2 x 7.2 m with a 3.2 m ceiling. The player stands at the origin facing +Z.

- Table: 1.5 x 0.95 m top at 0.83 m, walnut with a burgundy felt inlay and brass trim.
- Target: 3.3 m away, steel face with cream scoring ring on a stand, backed by a 1.7 m steel backstop plate.
- Lighting: one shadowed warm spot over the table (`Table Lamp Light`), one spot in front of the target, a weak red emergency point in the back-left corner, and a dim cool fill from the right. Four lights total, one with shadows.
- Post: a global Volume (`Assets/DevilsRoulette/Art/Post/Pit Post.asset`) with ACES tonemapping, mild bloom, vignette, and colour adjustments.
- Set dressing: steel wainscot with rivets, floor grout grid, firing line, drain grate, ceiling I-beams, air duct, wall pipes and valve, steel door, service panel, chain and hook, crate stack, steel drum, chair, spent shell casing, dust motes.

## Art pass

`Assets/Editor/PitArt/` rebuilds the environment procedurally.

- `ThePitArtPass.cs` - menu `Devils Roulette > Apply The Pit Art Pass`. It deletes what it owns and rebuilds it, so it is safe to run repeatedly. Gameplay objects keep their components; only their visuals and colliders are rebuilt.
- `PitArtAssets.cs` - bakes procedural concrete, steel, and wood albedo + normal maps, then the URP Lit materials that use them (`Assets/DevilsRoulette/Art/Textures`, `.../Materials`).
- `PitArtPreview.cs` - renders six fixed camera angles into `<project>/ArtPreviews`. Only used by the batch entry point.

Batch usage:

```
Unity.exe -batchmode -quit -projectPath <project> ^
  -executeMethod ThePitArtPass.ApplyAndRender ^
  -logFile <log>
```

The pass also edits both URP assets (`Performance URP Config`, `Quality URP Config`) because the shipped settings disabled everything except ambient light:

- additional lights: Per Pixel (4 per object)
- HDR: on
- local/additional light shadows: on, 1024 atlas
- shadow distance: 12 m

Without those, the spot lights in the scene do not light anything.

`CreateThePitPrototype.cs` and `ThePitModelingPass.cs` are the original generators. They are superseded by the art pass. The hidden `Modeling Pass V2` marker object stays in the scene on purpose: it is what stops the old modelling pass from running again on editor load.

## Demo video

`Demo_ThePit.mp4` (2:20, 1600x900, 30 fps) was rendered straight out of the project with a scripted take.

- `Assets/PitDemo/PitDemoDirector.cs` - timeline of camera keys, gun poses, six chamber shots, muzzle flash, recoil and shake. It renders frames synchronously in Edit mode from the real gameplay systems, so the chamber logic, status placard and impact marks in the video are the real ones.
- `Assets/Editor/PitDemo/PitDemoCapture.cs` - batch entry point (`PitDemoCapture.Run`, `RunProbe`, `RunTest`).
- `Tools/DemoVideo/build_demo.py` - synthesizes the sound design (drone, gunshot, clicks, impact ping), draws the bilingual captions and end card with Pillow, and muxes everything with ffmpeg.

Re-render the frames:

```
Unity.exe -batchmode -quit -projectPath <project> ^
  -executeMethod PitDemoCapture.Run ^
  -logFile <log>
```

Frames land in the path in `PitDemoDirector.DefaultOutput` (JPEG, `frame_00000.jpg` upwards, plus `events.csv`). Then run `python build_demo.py` after pointing `BASE` at that folder. The whole take renders in about 2 minutes on a desktop CPU.

### Gameplay hooks added for the demo

These are runtime-safe and are also useful for tests:

- `ShotgunController.Fire()`, `ShotgunController.Initialize()`, `ForceLiveChamber(int)` and the `ShotResolved` event.
- `TargetHitReceiver.Initialize()`, `OnShot(Vector3 worldPoint)`, `ClearHit()`. Hits now leave a bullet mark on the plate and spawn a short impact puff.
- `RouletteRoundDisplay.Build()`; the placard is narrower, tilted, and its feedback line wraps automatically.

## Controls

Use the right controller's grip to pick up the shotgun and its trigger to fire. After all six chambers have been used, release and pick up the shotgun to begin another randomized round. Aim at the target to register hits.

### Desktop simulation in the Unity Editor

The XR Device Simulator starts automatically in Play mode in the Editor. Click the Game view to give it keyboard and mouse focus.

- Hold the right mouse button and move the mouse to look around.
- Press `Y` to drive the right controller (or hold `Space`); press `T` for the left controller (or hold `Left Shift`).
- Move the mouse to move the active device; press `R` to switch between rotating and translating.
- `G` grips or releases the simulated controller; left click pulls its trigger.
- `W A S D` moves the active device, `Q`/`E` move it vertically, `Tab` cycles devices, `V` resets, `\` locks the cursor.

The simulator is enabled only in the Editor. It supports checking the interaction flow and input feedback, but it cannot validate Quest performance, headset comfort, or real controller haptics.

## Recording a take without controllers

1. Open `Assets/Scenes/ThePit.unity`, set the Game view to Fixed Resolution 1920x1080 and tick Maximize On Play.
2. Press Play, click the Game view, press `V` to reset the simulation pose, then `\` to lock the cursor.
3. Capture with Xbox Game Bar (`Win+Alt+R`, set capture to 60 fps), OBS window capture, or the Unity Recorder package (1080p60, no window chrome).
4. Collapse the simulator's control panel with its own arrow button before recording to keep it out of frame.

Record several short takes (pick up, aim, fire, look at the chamber readout) and cut them together; the simulation pose is mouse-driven, so long unbroken camera moves look shaky.

## Project setup

- Unity Editor: `6000.6.7f1`
- XR Interaction Toolkit: `3.6.1`
- OpenXR: `1.18.0`
- Meta OpenXR: `2.6.1`
- Android target: ARM64, IL2CPP, API 34
- Meta Quest Support and Quest 2 are enabled in the Android OpenXR settings.
- URP: `Performance URP Config` (Low quality level) with additional lights, HDR, and local shadows enabled by the art pass.

## Build

1. Open this project with Unity `6000.6.7f1`.
2. Open `Assets/Scenes/ThePit.unity`.
3. Select Android in Build Profiles and confirm that `ThePit` is the first enabled scene.
4. Build an APK after installing Android Build Support, Android SDK & NDK Tools, and OpenJDK for this Unity version.

## Scope and known limits

This is a single-player interaction study. It does not implement the original game's multiplayer, bluffing, cards, or social turn-taking. The gun, room, and props are editable low-poly primitives with procedural materials, and the sound effects are generated at runtime. A live round aimed at the player is only reported as a game event and does not apply damage.

- The scene is deliberately dark. If a take needs more luminance, raise `Table Lamp Light` intensity (currently 150) or the post exposure (currently 0.40) instead of adding more lights.
- Bloom and the shadowed spot are the two most expensive parts on Quest 2. Turning bloom off in the Volume profile is the first thing to try if the headset build drops frames.
- The shotgun still starts a couple of centimetres above the felt and settles on the first physics frames.
- Quest 2 performance and controller feel still need to be checked on the headset before presenting them as tested results.
