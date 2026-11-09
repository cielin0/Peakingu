from PIL import Image, ImageDraw
import os

# Create 192 x 160 image (6 columns of 32x32, 5 rows)
# Row 0: Idle (4 frames)
# Row 1: Run (6 frames)
# Row 2: Jump (3 frames)
# Row 3: Fall (3 frames)
# Row 4: Dash (4 frames)

W, H = 32, 32
COLS = 6
ROWS = 5
img = Image.new("RGBA", (COLS * W, ROWS * H), (0, 0, 0, 0))

# Colors
COLOR_OUTLINE = (18, 24, 46, 255)      # Deep navy outline
COLOR_BODY_DARK = (42, 68, 128, 255)    # Navy blue body
COLOR_BODY_LIGHT = (72, 118, 204, 255)  # Cyan-blue highlight
COLOR_BELLY = (235, 245, 255, 255)      # White-cyan belly
COLOR_BEAK = (255, 170, 34, 255)        # Orange beak
COLOR_FEET = (230, 130, 20, 255)        # Dark orange feet
COLOR_EYE = (15, 20, 35, 255)           # Eye pupil
COLOR_CYBORG_ARM = (180, 195, 215, 255) # Metallic silver arm
COLOR_CYBORG_DARK = (100, 120, 150, 255)# Shadow metallic
COLOR_CYBORG_GLOW = (0, 220, 255, 255)  # Neon cyan LED joint

def draw_pingu(draw, ox, oy, pose="idle", frame=0):
    # Base offsets based on animation
    bob_y = 0
    tilt_x = 0
    arm_angle = 0
    leg_l_y = 0
    leg_r_y = 0
    
    if pose == "idle":
        bob_y = 1 if frame in [1, 2] else 0
    elif pose == "run":
        bob_y = 1 if frame in [1, 4] else (0 if frame in [0, 3] else -1)
        tilt_x = 1 if frame in [1, 2] else (-1 if frame in [4, 5] else 0)
        leg_l_y = -2 if frame in [1, 2] else (1 if frame in [4, 5] else 0)
        leg_r_y = 1 if frame in [1, 2] else (-2 if frame in [4, 5] else 0)
    elif pose == "jump":
        bob_y = -3
    elif pose == "fall":
        bob_y = 1
    elif pose == "dash":
        bob_y = 4
        tilt_x = 3

    cx = ox + 16 + tilt_x
    cy = oy + 17 + bob_y

    # Feet
    if pose != "dash":
        draw.rectangle([cx - 7, oy + 28 + leg_l_y, cx - 2, oy + 30 + leg_l_y], fill=COLOR_FEET, outline=COLOR_OUTLINE)
        draw.rectangle([cx + 2, oy + 28 + leg_r_y, cx + 7, oy + 30 + leg_r_y], fill=COLOR_FEET, outline=COLOR_OUTLINE)
    else:
        # Dash feet tucked back
        draw.rectangle([cx - 13, cy + 2, cx - 9, cy + 5], fill=COLOR_FEET, outline=COLOR_OUTLINE)

    # Body ellipse / shape
    if pose == "dash":
        # Torpedo horizontal body
        draw.ellipse([cx - 10, cy - 6, cx + 11, cy + 8], fill=COLOR_BODY_DARK, outline=COLOR_OUTLINE)
        # Belly horizontal
        draw.ellipse([cx - 5, cy - 2, cx + 8, cy + 6], fill=COLOR_BELLY)
        # Eye
        draw.rectangle([cx + 6, cy - 2, cx + 8, cy], fill=COLOR_EYE)
        # Beak forward
        draw.polygon([(cx + 10, cy), (cx + 15, cy + 2), (cx + 10, cy + 4)], fill=COLOR_BEAK, outline=COLOR_OUTLINE)
        # Cyborg thruster jet on tail
        draw.rectangle([cx - 12, cy - 1, cx - 8, cy + 3], fill=COLOR_CYBORG_ARM, outline=COLOR_OUTLINE)
        draw.point((cx - 10, cy + 1), fill=COLOR_CYBORG_GLOW)
        # Thruster particle flame
        flame_len = 3 + (frame % 3) * 2
        draw.rectangle([cx - 12 - flame_len, cy, cx - 13, cy + 2], fill=COLOR_CYBORG_GLOW)
        return

    # Normal upright body
    draw.ellipse([cx - 9, cy - 11, cx + 9, cy + 11], fill=COLOR_BODY_DARK, outline=COLOR_OUTLINE)
    
    # White belly
    draw.ellipse([cx - 5, cy - 4, cx + 5, cy + 10], fill=COLOR_BELLY)

    # Face white area
    draw.ellipse([cx - 6, cy - 9, cx + 6, cy - 1], fill=COLOR_BELLY)

    # Eyes
    if pose == "idle" and frame == 3:
        # Blink
        draw.line([cx - 4, cy - 5, cx - 2, cy - 5], fill=COLOR_EYE)
        draw.line([cx + 2, cy - 5, cx + 4, cy - 5], fill=COLOR_EYE)
    elif pose == "fall":
        # Wide dizzy/surprised eyes
        draw.rectangle([cx - 4, cy - 7, cx - 1, cy - 3], fill=COLOR_EYE)
        draw.rectangle([cx + 1, cy - 7, cx + 4, cy - 3], fill=COLOR_EYE)
        draw.point((cx - 3, cy - 6), fill=(255, 255, 255, 255))
        draw.point((cx + 2, cy - 6), fill=(255, 255, 255, 255))
    else:
        # Normal eyes
        draw.rectangle([cx - 4, cy - 6, cx - 2, cy - 3], fill=COLOR_EYE)
        draw.rectangle([cx + 2, cy - 6, cx + 4, cy - 3], fill=COLOR_EYE)
        draw.point((cx - 3, cy - 5), fill=(255, 255, 255, 255))
        draw.point((cx + 3, cy - 5), fill=(255, 255, 255, 255))

    # Beak
    draw.polygon([(cx - 3, cy - 3), (cx + 3, cy - 3), (cx, cy)], fill=COLOR_BEAK, outline=COLOR_OUTLINE)

    # Left arm (flipper - regular)
    if pose in ["jump", "fall"]:
        # Flapping up
        draw.polygon([(cx - 8, cy - 5), (cx - 13, cy - 12), (cx - 6, cy - 7)], fill=COLOR_BODY_DARK, outline=COLOR_OUTLINE)
    else:
        # Resting down
        draw.polygon([(cx - 8, cy - 2), (cx - 12, cy + 4), (cx - 7, cy + 6)], fill=COLOR_BODY_DARK, outline=COLOR_OUTLINE)

    # Right arm: CYBORG ROBOTIC ARM!
    if pose in ["jump", "fall"]:
        # Robotic arm raised high with glowing fist
        draw.rectangle([cx + 7, cy - 9, cx + 12, cy - 4], fill=COLOR_CYBORG_ARM, outline=COLOR_OUTLINE)
        draw.rectangle([cx + 10, cy - 14, cx + 13, cy - 8], fill=COLOR_CYBORG_DARK, outline=COLOR_OUTLINE)
        draw.point((cx + 9, cy - 6), fill=COLOR_CYBORG_GLOW)
    elif pose == "run":
        # Swinging arm
        arm_off = 3 if frame in [0, 1, 2] else -3
        draw.rectangle([cx + 7, cy - 2 + arm_off, cx + 11, cy + 4 + arm_off], fill=COLOR_CYBORG_ARM, outline=COLOR_OUTLINE)
        draw.point((cx + 8, cy + arm_off), fill=COLOR_CYBORG_GLOW)
        draw.rectangle([cx + 9, cy + 3 + arm_off, cx + 12, cy + 6 + arm_off], fill=COLOR_CYBORG_DARK, outline=COLOR_OUTLINE)
    else:
        # Standard idle cyborg arm resting on side
        draw.rectangle([cx + 7, cy - 2, cx + 11, cy + 5], fill=COLOR_CYBORG_ARM, outline=COLOR_OUTLINE)
        draw.point((cx + 8, cy), fill=COLOR_CYBORG_GLOW)
        draw.rectangle([cx + 8, cy + 4, cx + 11, cy + 7], fill=COLOR_CYBORG_DARK, outline=COLOR_OUTLINE)

draw = ImageDraw.Draw(img)

# Row 0: Idle (4 frames)
for f in range(4):
    draw_pingu(draw, f * W, 0 * H, "idle", f)

# Row 1: Run (6 frames)
for f in range(6):
    draw_pingu(draw, f * W, 1 * H, "run", f)

# Row 2: Jump (3 frames)
for f in range(3):
    draw_pingu(draw, f * W, 2 * H, "jump", f)

# Row 3: Fall (3 frames)
for f in range(3):
    draw_pingu(draw, f * W, 3 * H, "fall", f)

# Row 4: Dash (4 frames)
for f in range(4):
    draw_pingu(draw, f * W, 4 * H, "dash", f)

out_path = r"c:\Users\dubcr\Downloads\Peakingu-main\Peakingu-main\Assets\Sprites\Pingu_Spritesheet.png"
img.save(out_path)
print(f"Pingu spritesheet saved successfully at: {out_path}")
