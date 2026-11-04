# Quest sideload and capture checklist

For a machine with no headset: everything below happens in one lab session with a TUS Quest 2 or Quest 3.

## Before the session

- `Builds/ThePit.apk` copied onto the lab PC (or a USB stick).
- Quest charged, and **Developer Mode** enabled for the account (Meta Horizon app -> Devices -> Developer Mode).
- USB-C data cable (charge-only cables do not work).
- On the lab PC: Android platform-tools (`adb`) - the Unity install already ships one at
  `C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe`
  (or the 6000.3 equivalent).

## Install and launch

```
adb devices                       # accept the "Allow USB debugging" prompt in the headset
adb install -r Builds/ThePit.apk
adb shell am start -n com.tus.devilsroulette.thepit/com.unity3d.player.UnityPlayerGameActivity
```

Unity 6 ships `UnityPlayerGameActivity`; the package name is `com.tus.devilsroulette.thepit`.

If the app opens on a black screen, check `adb logcat | findstr Unity` for OpenXR errors: the Quest 2
feature must be enabled in Project Settings > XR Plug-in Management > OpenXR (it already is in this project).

## Capture device footage

1. In the headset: **Quick Settings -> Record video** (or hold the Meta button). This captures the left eye
   plus microphone audio, which is what the brief asks for.
2. Play the prototype for 60-90 seconds: look around, pick the shotgun up, aim, fire until the live round.
3. Stop recording, then pull the file:

```
adb pull /sdcard/Oculus/VideoShots ./footage
```

4. Cut it to 2-3 minutes. Keep the on-table placard and at least one live-round hit in shot.

## User test (SUS + SSQ)

Per participant, 10-15 minutes:

1. Consent form (Week 5), then **SSQ before**.
2. Let them use the prototype for 5 minutes: pick up, aim, fire through one full round.
3. **SSQ after**, then **SUS**.
4. Note what you observed: did they find the grip without being told, did the placard get read, did they
   report arm fatigue holding the gun up, any discomfort.

Report the SUS score per participant (0-100 after the standard conversion) and the SSQ change
(pre/post total score). Three participants is enough for the brief; say clearly that it is a small sample.

## First run on the headset

Launch check:

```
adb shell am start -n com.tus.devilsroulette.thepit/com.unity3d.player.UnityPlayerGameActivity
adb logcat -s Unity | findstr /i "error exception"
```

It should open straight into VR. A 2D window means the OpenXR session did not start - check logcat for
`OpenXR` errors, and confirm the APK manifest carries `com.oculus.intent.category.VR`.

What to look at, in this order:

1. **Framerate.** It should hold 72 Hz. If it stutters, change one thing at a time:
   - `Assets/Settings/Project Configuration/Performance URP Config.asset` -> MSAA 4 -> 2
   - same asset -> Shadow Distance 12 -> 8
   - `Assets/DevilsRoulette/Art/Post/Pit Post.asset` -> Bloom intensity 0.55 -> 0.3
   - same URP asset -> Additional Lights Per Object 4 -> 2 (this one changes the look most)
2. **Grab affordance.** Hover the controller over the shotgun before gripping - the gun should warm up.
   If you cannot see it, raise `hoverGain` on `GrabAffordance` from 1.45 to 1.8.
3. **Haptics.** A short pulse on grab, a stronger one on a live round. If the grab pulse is too strong,
   lower `selectHapticAmplitude` from 0.28 to 0.15.
4. **Placard legibility.** Read the round text from about an arm's length away. If it is too small,
   raise `characterSize` in `RouletteRoundDisplay` from 0.0088 to 0.011.
5. **Recovery.** Drop the gun on the floor; it should return to the table after 2.5 s.
6. **Comfort.** Recoil is a gun kick plus haptic, not camera shake. If the sway still bothers anyone,
   drop the muzzle-light and shake terms in `ShotgunController`.
