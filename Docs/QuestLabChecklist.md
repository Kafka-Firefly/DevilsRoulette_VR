# Quest sideload and capture checklist

For a machine with no headset: everything below happens in one lab session with a TUS Quest 2 or Quest 3.

## Before the session

- `Builds/ThePit.apk` copied onto the lab PC (or a USB stick).
- Quest charged, and **Developer Mode** enabled for the account (Meta Horizon app -> Devices -> Developer Mode).
- USB-C data cable (charge-only cables do not work).
- On the lab PC: Android platform-tools (`adb`) â€?the Unity install already ships one at
  `C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe`.

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
