import math
import struct
import wave
import os
import random

SAMPLE_RATE = 22050

def write_wav(filepath, samples, sample_rate=SAMPLE_RATE):
    os.makedirs(os.path.dirname(filepath), exist_ok=True)
    with wave.open(filepath, 'w') as wav:
        wav.setnchannels(1)        # Mono
        wav.setsampwidth(2)        # 16-bit
        wav.setframerate(sample_rate)
        
        # Clamp and pack samples
        raw_data = bytearray()
        for s in samples:
            val = int(max(-1.0, min(1.0, s)) * 32767)
            raw_data.extend(struct.pack('<h', val))
        wav.writeframes(raw_data)

def lerp(a, b, t):
    return a + (b - a) * t

def sine(phase):
    return math.sin(phase * 2 * math.pi)

def square(phase, duty=0.5):
    p = phase % 1.0
    return 1.0 if p < duty else -1.0

def triangle(phase):
    p = phase % 1.0
    return 4.0 * abs(p - 0.5) - 1.0

# 1. SFX Jump (Rising pitch chirp)
def gen_sfx_jump():
    duration = 0.16
    n = int(SAMPLE_RATE * duration)
    samples = []
    phase = 0.0
    for i in range(n):
        t = i / n
        freq = lerp(160, 620, t ** 0.8)
        phase += freq / SAMPLE_RATE
        env = (1.0 - t) ** 0.5
        s = (0.7 * square(phase, 0.4) + 0.3 * sine(phase)) * env * 0.7
        samples.append(s)
    return samples

# 2. SFX Land (Soft snow/ice thud)
def gen_sfx_land():
    duration = 0.12
    n = int(SAMPLE_RATE * duration)
    samples = []
    phase = 0.0
    for i in range(n):
        t = i / n
        freq = lerp(130, 45, t)
        phase += freq / SAMPLE_RATE
        env = (1.0 - t) ** 2.0
        noise = (random.random() * 2.0 - 1.0) * 0.3
        s = (sine(phase) * 0.7 + noise) * env * 0.7
        samples.append(s)
    return samples

# 3. SFX Coin (High 2-tone crystalline chime)
def gen_sfx_coin():
    duration = 0.28
    n = int(SAMPLE_RATE * duration)
    samples = []
    phase = 0.0
    split = int(SAMPLE_RATE * 0.08)
    for i in range(n):
        if i < split:
            t = i / split
            freq = 987.77 # B5
            env = 1.0 - t * 0.2
        else:
            t = (i - split) / (n - split)
            freq = 1318.51 # E6
            env = (1.0 - t) ** 1.8
        phase += freq / SAMPLE_RATE
        s = (sine(phase) * 0.8 + sine(phase * 2) * 0.2) * env * 0.75
        samples.append(s)
    return samples

# 4. SFX Button Click (Crisp UI click)
def gen_sfx_button_click():
    duration = 0.04
    n = int(SAMPLE_RATE * duration)
    samples = []
    phase = 0.0
    for i in range(n):
        t = i / n
        freq = lerp(1400, 700, t)
        phase += freq / SAMPLE_RATE
        env = (1.0 - t) ** 3.0
        s = sine(phase) * env * 0.85
        samples.append(s)
    return samples

# 5. SFX Purchase Success (Triumphant 4-note arpeggio)
def gen_sfx_purchase_success():
    notes = [523.25, 659.25, 783.99, 1046.50] # C5, E5, G5, C6
    note_dur = 0.09
    samples = []
    for idx, freq in enumerate(notes):
        n = int(SAMPLE_RATE * (note_dur * 1.5 if idx == 3 else note_dur))
        phase = 0.0
        for i in range(n):
            t = i / n
            phase += freq / SAMPLE_RATE
            env = (1.0 - t) ** (1.2 if idx == 3 else 0.8)
            s = (0.7 * square(phase, 0.3) + 0.3 * sine(phase)) * env * 0.65
            samples.append(s)
    return samples

# 6. SFX Purchase Fail (Low dual-buzz)
def gen_sfx_purchase_fail():
    duration = 0.28
    n = int(SAMPLE_RATE * duration)
    samples = []
    phase1 = 0.0
    phase2 = 0.0
    for i in range(n):
        t = i / n
        # Two pulses
        pulse = 1.0 if (t < 0.45 or (t > 0.55 and t < 0.95)) else 0.0
        phase1 += 150.0 / SAMPLE_RATE
        phase2 += 165.0 / SAMPLE_RATE # dissonance
        s = (square(phase1, 0.5) * 0.5 + square(phase2, 0.5) * 0.5) * pulse * 0.65
        samples.append(s)
    return samples

# 7. SFX Dash (Whoosh burst)
def gen_sfx_dash():
    duration = 0.22
    n = int(SAMPLE_RATE * duration)
    samples = []
    phase = 0.0
    lp = 0.0
    for i in range(n):
        t = i / n
        freq = lerp(600, 150, t)
        phase += freq / SAMPLE_RATE
        noise = random.random() * 2.0 - 1.0
        lp = lp + 0.2 * (noise - lp)
        env = math.sin(t * math.pi) ** 1.5
        s = (sine(phase) * 0.4 + lp * 0.6) * env * 0.8
        samples.append(s)
    return samples

# 8. SFX Grapple Launch (Mechanical zip / anchor launch)
def gen_sfx_grapple_launch():
    duration = 0.24
    n = int(SAMPLE_RATE * duration)
    samples = []
    phase = 0.0
    for i in range(n):
        t = i / n
        freq = lerp(850, 1950, t ** 0.5)
        phase += freq / SAMPLE_RATE
        env = (1.0 - t) ** 0.7
        # Metallic FM modulation
        mod = math.sin(phase * 4 * math.pi) * 0.3
        s = (square(phase + mod, 0.25) * 0.6 + sine(phase * 2) * 0.4) * env * 0.7
        samples.append(s)
    return samples

# 9. SFX Grapple Release (Mechanical unlatch click)
def gen_sfx_grapple_release():
    duration = 0.12
    n = int(SAMPLE_RATE * duration)
    samples = []
    phase = 0.0
    for i in range(n):
        t = i / n
        # Dual sharp clicks
        click = 0.0
        if t < 0.3:
            tc = t / 0.3
            freq = lerp(1600, 400, tc)
            phase += freq / SAMPLE_RATE
            click = sine(phase) * ((1.0 - tc) ** 2.0)
        elif t > 0.4 and t < 0.8:
            tc = (t - 0.4) / 0.4
            freq = lerp(2200, 600, tc)
            phase += freq / SAMPLE_RATE
            click = sine(phase) * ((1.0 - tc) ** 2.0)
        samples.append(click * 0.75)
    return samples

# 10. SFX Footsteps (Snow/ice steps)
def gen_sfx_step(pitch_mod=1.0):
    duration = 0.09
    n = int(SAMPLE_RATE * duration)
    samples = []
    lp = 0.0
    phase = 0.0
    for i in range(n):
        t = i / n
        freq = lerp(350 * pitch_mod, 120 * pitch_mod, t)
        phase += freq / SAMPLE_RATE
        noise = random.random() * 2.0 - 1.0
        lp = lp + 0.3 * (noise - lp)
        env = (1.0 - t) ** 2.2
        s = (sine(phase) * 0.35 + lp * 0.65) * env * 0.6
        samples.append(s)
    return samples

# 11. SFX Game Over (Descending defeat motif)
def gen_sfx_game_over():
    notes = [329.63, 293.66, 261.63, 220.00] # E4, D4, C4, A3
    note_dur = 0.28
    samples = []
    for idx, freq in enumerate(notes):
        n = int(SAMPLE_RATE * note_dur)
        phase = 0.0
        for i in range(n):
            t = i / n
            phase += freq / SAMPLE_RATE
            env = (1.0 - t) ** 1.3
            s = (triangle(phase) * 0.6 + square(phase, 0.5) * 0.4) * env * 0.7
            samples.append(s)
    return samples

# 12. SFX Victory (Triumphant fanfare)
def gen_sfx_victory():
    notes = [392.00, 523.25, 659.25, 783.99] # G4, C5, E5, G5
    samples = []
    for idx, freq in enumerate(notes):
        dur = 0.35 if idx == 3 else 0.15
        n = int(SAMPLE_RATE * dur)
        phase = 0.0
        for i in range(n):
            t = i / n
            vib = math.sin(t * 12.0) * 0.02 if idx == 3 else 0.0
            phase += (freq * (1.0 + vib)) / SAMPLE_RATE
            env = (1.0 - t * 0.5) if idx == 3 else (1.0 - t * 0.3)
            s = (square(phase, 0.4) * 0.5 + triangle(phase) * 0.5) * env * 0.75
            samples.append(s)
    return samples

# 13. Music Menu (8s cozy snowy melody loop)
def gen_music_menu():
    bpm = 100
    beats = 16 # 4 bars = 9.6s approx -> let's make 8.0s
    total_dur = 8.0
    n = int(SAMPLE_RATE * total_dur)
    samples = [0.0] * n
    
    # Chord melody: Am (A3, C4, E4), F (F3, A3, C4), C (C3, E3, G3), G (G3, B3, D4)
    chords = [
        [220.0, 261.63, 329.63], # Am
        [174.61, 220.0, 261.63], # F
        [130.81, 164.81, 196.00], # C
        [196.00, 246.94, 293.66], # G
    ]
    bar_samples = n // 4
    for bar_idx, chord in enumerate(chords):
        start_samp = bar_idx * bar_samples
        # Arpeggiate chord notes
        arp_dur = bar_samples // 4
        for sub in range(4):
            freq = chord[sub % len(chord)] * 2.0 # bell pitch
            bass_freq = chord[0] * 0.5
            s_idx = start_samp + sub * arp_dur
            phase_m = 0.0
            phase_b = 0.0
            for i in range(arp_dur):
                pos = s_idx + i
                if pos >= n: break
                t = i / arp_dur
                phase_m += freq / SAMPLE_RATE
                phase_b += bass_freq / SAMPLE_RATE
                env_m = (1.0 - t) ** 1.5
                env_b = 1.0 - t * 0.4
                m_val = sine(phase_m) * env_m * 0.28
                b_val = triangle(phase_b) * env_b * 0.32
                samples[pos] += m_val + b_val
    return samples

# 14. Music Nivel1 (8s upbeat mountain climbing chiptune loop)
def gen_music_nivel1():
    total_dur = 8.0
    n = int(SAMPLE_RATE * total_dur)
    samples = [0.0] * n
    # Melody notes (C major scale)
    melody = [
        523.25, 587.33, 659.25, 783.99,
        659.25, 523.25, 587.33, 392.00,
        523.25, 659.25, 783.99, 1046.50,
        783.99, 659.25, 587.33, 523.25
    ]
    step_samples = n // len(melody)
    phase_lead = 0.0
    phase_bass = 0.0
    for idx, freq in enumerate(melody):
        s_idx = idx * step_samples
        bass_freq = freq * 0.25
        for i in range(step_samples):
            pos = s_idx + i
            if pos >= n: break
            t = i / step_samples
            phase_lead += freq / SAMPLE_RATE
            phase_bass += bass_freq / SAMPLE_RATE
            env_lead = (1.0 - t) ** 0.8
            lead = square(phase_lead, 0.35) * env_lead * 0.26
            bass = triangle(phase_bass) * 0.32
            samples[pos] += lead + bass
    return samples

# 15. Music Cueva (8s deep echoing ice cave ambience loop)
def gen_music_cueva():
    total_dur = 8.0
    n = int(SAMPLE_RATE * total_dur)
    samples = [0.0] * n
    # E minor ambient: E, G, B, D
    cave_arps = [
        659.25, 783.99, 987.77, 1174.66,
        987.77, 783.99, 659.25, 493.88
    ]
    step_samples = n // len(cave_arps)
    phase_drip = 0.0
    phase_drone = 0.0
    drone_freq = 82.41 # E2 deep drone
    for i in range(n):
        phase_drone += drone_freq / SAMPLE_RATE
        drone = (sine(phase_drone) + sine(phase_drone * 1.5) * 0.4) * 0.25
        samples[i] += drone

    for idx, freq in enumerate(cave_arps):
        s_idx = idx * step_samples
        for i in range(step_samples):
            pos = s_idx + i
            if pos >= n: break
            t = i / step_samples
            phase_drip += freq / SAMPLE_RATE
            env = (1.0 - t) ** 2.2
            drip = sine(phase_drip) * env * 0.3
            samples[pos] += drip
            # Cave echo (after 0.25s)
            echo_pos = pos + int(SAMPLE_RATE * 0.25)
            if echo_pos < n:
                samples[echo_pos] += drip * 0.45
    return samples

def main():
    audio_dir = r"c:\Users\dubcr\Downloads\Peakingu-main\Peakingu-main\Assets\Audio"
    os.makedirs(audio_dir, exist_ok=True)
    
    files = {
        "SFX_Jump.wav": gen_sfx_jump(),
        "SFX_Land.wav": gen_sfx_land(),
        "SFX_Coin.wav": gen_sfx_coin(),
        "SFX_ButtonClick.wav": gen_sfx_button_click(),
        "SFX_PurchaseSuccess.wav": gen_sfx_purchase_success(),
        "SFX_PurchaseFail.wav": gen_sfx_purchase_fail(),
        "SFX_Dash.wav": gen_sfx_dash(),
        "SFX_GrappleLaunch.wav": gen_sfx_grapple_launch(),
        "SFX_GrappleRelease.wav": gen_sfx_grapple_release(),
        "SFX_Step_1.wav": gen_sfx_step(1.0),
        "SFX_Step_2.wav": gen_sfx_step(0.85),
        "SFX_GameOver.wav": gen_sfx_game_over(),
        "SFX_Victory.wav": gen_sfx_victory(),
        "Music_Menu.wav": gen_music_menu(),
        "Music_Nivel1.wav": gen_music_nivel1(),
        "Music_Cueva.wav": gen_music_cueva(),
    }
    
    for filename, data in files.items():
        out_path = os.path.join(audio_dir, filename)
        write_wav(out_path, data)
        print(f"Generated: {out_path} ({len(data)} samples)")

if __name__ == "__main__":
    main()
