"""Builds the demo video: synthesized sound design + caption overlays + ffmpeg encode."""

import os
import subprocess
import wave

import numpy as np
from PIL import Image, ImageDraw, ImageFont

SR = 44100
WIDTH, HEIGHT = 1600, 900
FPS = 30
DURATION = 140.0

BASE = r"C:\Users\faifa\Documents\deepseek-harness\default-workspace\pit-art\video"
FRAMES = os.path.join(BASE, "frames")
OUT = os.path.join(BASE, "ThePit_Demo.mp4")

FONT_CN = r"C:\Windows\Fonts\msyh.ttc"
FONT_EN = r"C:\Windows\Fonts\segoeui.ttf"
FONT_BOLD = r"C:\Windows\Fonts\segoeuib.ttf"

FFMPEG = (r"C:\Users\faifa\.dsh\dsh-runtimes\dsh-primary-runtime\dependencies\python"
          r"\Lib\site-packages\imageio_ffmpeg\binaries\ffmpeg-win-x86_64-v7.1.exe")

SHOT_TIMES = [58.0, 61.5, 65.0, 74.0, 77.5, 81.0]
LIVE_SHOT = 65.0


def lowpass(x, cutoff, order=2):
    spec = np.fft.rfft(x)
    freqs = np.fft.rfftfreq(len(x), 1.0 / SR)
    spec *= 1.0 / (1.0 + (freqs / cutoff) ** order)
    return np.fft.irfft(spec, len(x))


def gunshot(dur=1.6, seed=0):
    rng = np.random.default_rng(seed)
    n = int(dur * SR)
    t = np.arange(n) / SR
    noise = rng.normal(0, 1, n) * np.exp(-t * 24)
    body = np.sin(2 * np.pi * 58 * t) * np.exp(-t * 6.5)
    crack = np.sin(2 * np.pi * 1900 * t) * np.exp(-t * 70)
    snap = rng.normal(0, 1, n) * np.exp(-t * 140)
    dry = 0.85 * noise + 0.55 * body + 0.30 * crack + 0.45 * snap

    ir_n = int(0.45 * SR)
    ir = rng.normal(0, 1, ir_n) * np.exp(-np.arange(ir_n) / SR * 8.5)
    ir = lowpass(ir, 4500)
    ir /= np.max(np.abs(ir)) + 1e-9
    tail = np.convolve(dry, ir)[:n] * 0.06

    sig = dry + tail
    return sig / (np.max(np.abs(sig)) + 1e-9)


def click(seed=1):
    rng = np.random.default_rng(seed)
    n = int(0.09 * SR)
    t = np.arange(n) / SR
    noise = rng.normal(0, 1, n) * np.exp(-t * 120)
    metal = np.sin(2 * np.pi * 2100 * t) * np.exp(-t * 70)
    sig = 0.55 * noise + 0.45 * metal
    return sig / (np.max(np.abs(sig)) + 1e-9)


def ping():
    n = int(0.55 * SR)
    t = np.arange(n) / SR
    sig = (
        np.sin(2 * np.pi * 1180 * t) * np.exp(-t * 12)
        + 0.6 * np.sin(2 * np.pi * 1760 * t) * np.exp(-t * 16)
        + 0.35 * np.sin(2 * np.pi * 2630 * t) * np.exp(-t * 22)
    )
    return sig / (np.max(np.abs(sig)) + 1e-9)


def boom(dur=2.2, freq=38.0):
    n = int(dur * SR)
    t = np.arange(n) / SR
    sig = np.sin(2 * np.pi * freq * t) * np.exp(-t * 1.6)
    sig += 0.35 * np.sin(2 * np.pi * freq * 2.02 * t) * np.exp(-t * 2.4)
    return sig / (np.max(np.abs(sig)) + 1e-9)


def add(buffer, sig, at, gain=1.0, pan=0.0):
    start = int(at * SR)
    if start >= len(buffer[0]):
        return
    end = min(len(buffer[0]), start + len(sig))
    chunk = sig[: end - start] * gain
    angle = (pan + 1.0) * np.pi / 4.0
    buffer[0][start:end] += chunk * np.cos(angle)
    buffer[1][start:end] += chunk * np.sin(angle)


def build_audio(path):
    n = int(DURATION * SR)
    t = np.arange(n) / SR

    drone = (
        np.sin(2 * np.pi * 47.5 * t)
        + 0.6 * np.sin(2 * np.pi * 71.2 * t)
        + 0.35 * np.sin(2 * np.pi * 95.0 * t)
    )
    slow = 0.62 + 0.38 * np.sin(2 * np.pi * 0.055 * t)
    envelope = np.clip(t / 6.0, 0, 1) * np.clip((DURATION - t) / 6.0, 0, 1)
    room = lowpass(np.random.default_rng(7).normal(0, 1, n), 320, 2)
    room /= np.max(np.abs(room)) + 1e-9

    ambient = 0.030 * drone * slow * envelope + 0.012 * room * envelope
    mix = [np.zeros(n), np.zeros(n)]
    mix[0] += ambient
    mix[1] += ambient

    add(mix, boom(), 1.0, 0.32)
    add(mix, boom(2.6, 33.0), 126.5, 0.28)

    for index, shot_time in enumerate(SHOT_TIMES):
        if abs(shot_time - LIVE_SHOT) < 0.01:
            add(mix, gunshot(seed=11), shot_time, 0.95, pan=0.15)
            add(mix, ping(), shot_time + 0.05, 0.22, pan=-0.25)
        else:
            add(mix, click(seed=20 + index), shot_time, 0.5, pan=0.1)

    for at in (19.0, 21.5, 24.0):
        n2 = int(0.25 * SR)
        tt = np.arange(n2) / SR
        step = np.sin(2 * np.pi * 70 * tt) * np.exp(-tt * 22)
        add(mix, step / (np.max(np.abs(step)) + 1e-9), at, 0.10, pan=-0.1)

    stereo = np.stack(mix, axis=1)
    stereo = np.tanh(stereo * 2.0)
    peak = np.max(np.abs(stereo)) + 1e-9
    stereo = stereo / peak * 0.95

    pcm = (stereo * 32767).astype(np.int16)
    with wave.open(path, "wb") as handle:
        handle.setnchannels(2)
        handle.setsampwidth(2)
        handle.setframerate(SR)
        handle.writeframes(pcm.tobytes())
    return path


def font(path, size):
    return ImageFont.truetype(path, size)


def caption(path, title, subtitle):
    img = Image.new("RGBA", (WIDTH, HEIGHT), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    title_font = font(FONT_BOLD, 34)
    sub_font = font(FONT_EN, 23)

    measured = max(
        draw.textbbox((0, 0), title, font=title_font)[2],
        draw.textbbox((0, 0), subtitle, font=sub_font)[2],
    )
    box_w = measured + 66
    box_h = 118
    x, y = 78, HEIGHT - 196

    draw.rounded_rectangle([x, y, x + box_w, y + box_h], radius=8, fill=(8, 9, 11, 190))
    draw.rectangle([x, y, x + 5, y + box_h], fill=(176, 30, 22, 235))

    draw.text((x + 30, y + 20), title, font=title_font, fill=(238, 230, 214, 255))
    draw.text((x + 32, y + 70), subtitle, font=sub_font, fill=(170, 165, 155, 255))
    img.save(path)


def title_card(path):
    img = Image.new("RGBA", (WIDTH, HEIGHT), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    draw.rectangle([0, 0, WIDTH, HEIGHT], fill=(4, 5, 7, 168))
    big = font(FONT_BOLD, 92)
    small = font(FONT_EN, 27)

    text = "DEVIL'S ROULETTE"
    box = draw.textbbox((0, 0), text, font=big)
    draw.text(((WIDTH - (box[2] - box[0])) / 2, 330), text, font=big, fill=(235, 226, 208, 255))

    line_y = 462
    draw.rectangle([WIDTH / 2 - 150, line_y, WIDTH / 2 + 150, line_y + 2], fill=(150, 30, 22, 220))

    sub = "THE PIT  \u00b7  VR INTERACTION PROTOTYPE"
    box = draw.textbbox((0, 0), sub, font=small)
    draw.text(((WIDTH - (box[2] - box[0])) / 2, 486), sub, font=small, fill=(178, 172, 160, 255))
    img.save(path)


def end_card(path):
    img = Image.new("RGBA", (WIDTH, HEIGHT), (0, 0, 0, 255))
    draw = ImageDraw.Draw(img)
    big = font(FONT_BOLD, 72)
    mid = font(FONT_CN, 32)
    small = font(FONT_EN, 22)

    text = "THE PIT"
    box = draw.textbbox((0, 0), text, font=big)
    draw.text(((WIDTH - (box[2] - box[0])) / 2, 300), text, font=big, fill=(233, 224, 206, 255))

    line_y = 408
    draw.rectangle([WIDTH / 2 - 120, line_y, WIDTH / 2 + 120, line_y + 2], fill=(150, 30, 22, 220))

    lines = [
        ("A VR interaction prototype built in Unity",
         mid, 452, (200, 193, 180, 255)),
        ("Unity 6.6  \u00b7  XR Interaction Toolkit 3.6.1  \u00b7  Meta Quest 2 target",
         small, 520, (150, 145, 136, 255)),
        ("Desktop capture  \u00b7  1600 x 900  \u00b7  30 fps",
         small, 558, (120, 116, 110, 255)),
    ]
    for content, used_font, y, color in lines:
        box = draw.textbbox((0, 0), content, font=used_font)
        draw.text(((WIDTH - (box[2] - box[0])) / 2, y), content, font=used_font, fill=color)

    img.save(path)


def main():
    audio_path = os.path.join(BASE, "score.wav")
    build_audio(audio_path)
    print("audio:", audio_path)

    overlays = []
    title_path = os.path.join(BASE, "ov_title.png")
    title_card(title_path)
    overlays.append((title_path, 1.2, 7.0))

    specs = [
        ("An interaction study inspired by Devil's Roulette",
         "Rebuilt in Unity 6 with the XR Interaction Toolkit", 8.0, 14.5),
        ("One key interaction: grab, aim, fire",
         "A single mechanic, rebuilt from the original", 18.0, 25.5),
        ("GRIP to pick up  \u00b7  TRIGGER to fire",
         "The grab pose is set by an attach transform", 30.0, 38.0),
        ("Six chambers, one live round",
         "The order is randomised every round", 51.0, 57.5),
        ("Empty chamber: a click and a haptic tick",
         "Feedback confirms the shot without a bang", 58.5, 64.0),
        ("Live round: the target takes the hit",
         "Muzzle flash, impact spark, permanent mark", 65.5, 72.0),
        ("Round complete, chambers reset",
         "A world-space placard reports the state", 84.0, 91.0),
        ("The impact stays on the plate",
         "Marks accumulate across rounds", 112.0, 119.5),
    ]

    for index, (title, subtitle, start, end) in enumerate(specs):
        path = os.path.join(BASE, "ov_cap_%02d.png" % index)
        caption(path, title, subtitle)
        overlays.append((path, start, end))

    end_path = os.path.join(BASE, "ov_end.png")
    end_card(end_path)
    overlays.append((end_path, 126.5, DURATION + 1))

    inputs = ["-framerate", str(FPS), "-i", os.path.join(FRAMES, "frame_%05d.jpg")]
    for path, _, _ in overlays:
        inputs += ["-i", path]
    inputs += ["-i", audio_path]

    filters = []
    # Fade the rendered take first so overlays (title, end card) stay readable.
    filters.append("[0:v]fade=in:st=0:d=1.0,fade=out:st=124.5:d=2.0[vbase]")
    current = "vbase"
    for i, (_, start, end) in enumerate(overlays, start=1):
        label = "v%d" % i
        filters.append("[%s][%d:v]overlay=0:0:enable='between(t,%.2f,%.2f)'[%s]"
                       % (current, i, start, end, label))
        current = label

    filters.append("[%s]null[vout]" % current)
    audio_index = len(overlays) + 1
    filters.append("[%d:a]afade=in:st=0:d=1.5,afade=out:st=%d:d=3.0[aout]"
                   % (audio_index, DURATION - 5))

    command = (
        [FFMPEG, "-y", "-loglevel", "error"]
        + inputs
        + ["-filter_complex", ";".join(filters)]
        + ["-map", "[vout]", "-map", "[aout]"]
        + ["-c:v", "libx264", "-preset", "medium", "-crf", "19", "-pix_fmt", "yuv420p",
           "-c:a", "aac", "-b:a", "192k", "-shortest", OUT]
    )

    print("encoding:", OUT)
    subprocess.run(command, check=True)
    print("done:", OUT, os.path.getsize(OUT))


if __name__ == "__main__":
    main()
