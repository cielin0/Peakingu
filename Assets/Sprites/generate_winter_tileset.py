from PIL import Image, ImageDraw
import os

W = 32
H = 32

# Palette
C_OUTLINE = (18, 30, 52, 255)       # Darkest slate navy outline
C_ROCK_DARK = (32, 54, 88, 255)     # Deep glacial slate
C_ROCK_MID = (48, 80, 126, 255)     # Medium ice rock
C_ROCK_LIGHT = (76, 122, 185, 255)  # Crystal slate highlight
C_SNOW_WHITE = (252, 254, 255, 255) # Pure snow top
C_SNOW_SHADE = (218, 234, 248, 255) # Snow volume shading
C_SNOW_BLUE = (170, 202, 230, 255)  # Snow shadow contact
C_ICE_DRIP = (195, 230, 255, 255)   # Hanging icicles
C_ICE_TIP = (245, 252, 255, 255)    # Icicle tip highlight

def draw_rock_interior(draw, x0, y0, x1, y1):
    # Base fill
    draw.rectangle([x0, y0, x1, y1], fill=C_ROCK_DARK)
    # Slate texture patterns
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            if (x * 7 + y * 13) % 17 < 5:
                draw.point((x, y), fill=C_ROCK_MID)
            elif (x * 11 + y * 5) % 23 < 3:
                draw.point((x, y), fill=C_ROCK_LIGHT)

# 1. borde_sup (Top edge with snow cap)
def make_borde_sup():
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    # Rock base from y=0 to y=23
    draw_rock_interior(d, 0, 8, 31, 31)
    # Snow layer: fluffy waving snow on top (y=0 to 9)
    # Note: in PIL y=0 is TOP of image!
    # Snow surface
    for x in range(32):
        snow_depth = 8 + int(1.5 * ((x * 3) % 5 == 0) - int((x * 7) % 4 == 0))
        # White top
        for y in range(0, 4):
            d.point((x, y), fill=C_SNOW_WHITE)
        # Snow body
        for y in range(4, snow_depth - 2):
            d.point((x, y), fill=C_SNOW_WHITE if (x + y) % 3 != 0 else C_SNOW_SHADE)
        # Snow shade underside
        for y in range(snow_depth - 2, snow_depth):
            d.point((x, y), fill=C_SNOW_SHADE)
        d.point((x, snow_depth), fill=C_SNOW_BLUE)
        d.point((x, snow_depth + 1), fill=C_OUTLINE)
    return im

# 2. esquina_sup_izq (Top-left corner)
def make_esquina_sup_izq():
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    # Draw rock in bounds
    for y in range(32):
        for x in range(32):
            # Curved top-left corner
            if x + y >= 8 and x >= 4:
                d.point((x, y), fill=C_ROCK_DARK)
                if (x * 7 + y * 13) % 17 < 5:
                    d.point((x, y), fill=C_ROCK_MID)
                elif (x * 11 + y * 5) % 23 < 3:
                    d.point((x, y), fill=C_ROCK_LIGHT)
    # Left rock outline
    for y in range(9, 32):
        d.point((4, y), fill=C_OUTLINE)
        d.point((5, y), fill=C_ROCK_LIGHT) # Cold rim light
    # Snow curving on corner
    for x in range(4, 32):
        snow_depth = 8 + int(1.5 * ((x * 3) % 5 == 0))
        y_start = max(0, 8 - x)
        for y in range(y_start, snow_depth):
            if y < y_start + 3:
                d.point((x, y), fill=C_SNOW_WHITE)
            elif y < snow_depth - 2:
                d.point((x, y), fill=C_SNOW_SHADE)
            else:
                d.point((x, y), fill=C_SNOW_BLUE)
        d.point((x, snow_depth), fill=C_OUTLINE)
    # Snow drip on left edge
    d.line([(4, 4), (4, 9)], fill=C_SNOW_WHITE)
    d.line([(3, 5), (3, 8)], fill=C_OUTLINE)
    return im

# 3. esquina_sup_der (Top-right corner)
def make_esquina_sup_der():
    im = make_esquina_sup_izq()
    return im.transpose(Image.FLIP_LEFT_RIGHT)

# 4. borde_izq (Left wall)
def make_borde_izq():
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    draw_rock_interior(d, 4, 0, 31, 31)
    for y in range(32):
        d.point((3, y), fill=C_OUTLINE)
        d.point((4, y), fill=C_OUTLINE)
        d.point((5, y), fill=C_ROCK_LIGHT) # Frost highlight
    return im

# 5. relleno (Solid center rock)
def make_relleno():
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    draw_rock_interior(d, 0, 0, 31, 31)
    return im

# 6. borde_der (Right wall)
def make_borde_der():
    im = make_borde_izq()
    return im.transpose(Image.FLIP_LEFT_RIGHT)

# 7. borde_inf (Bottom edge with hanging icicles)
def make_borde_inf():
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    # Rock body
    draw_rock_interior(d, 0, 0, 31, 23)
    # Bottom rock outline
    for x in range(32):
        d.point((x, 23), fill=C_OUTLINE)
    # Hanging icicles
    icicle_positions = [(3, 5), (7, 8), (12, 4), (16, 7), (21, 5), (25, 8), (29, 4)]
    for x_center, length in icicle_positions:
        for dy in range(length):
            y = 23 + dy
            if y < 32:
                d.point((x_center, y), fill=C_ICE_DRIP)
                if dy == length - 1:
                    d.point((x_center, y), fill=C_ICE_TIP)
                if dy < length - 2:
                    d.point((x_center - 1, y), fill=C_ROCK_DARK)
                    d.point((x_center + 1, y), fill=C_ROCK_DARK)
    return im

# 8. esquina_inf_izq (Bottom-left corner)
def make_esquina_inf_izq():
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    # Draw rock leaving corner curved
    for y in range(32):
        for x in range(32):
            if x >= 4 and (31 - y) + (x - 4) >= 6:
                d.point((x, y), fill=C_ROCK_DARK)
                if (x * 7 + y * 13) % 17 < 5:
                    d.point((x, y), fill=C_ROCK_MID)
                elif (x * 11 + y * 5) % 23 < 3:
                    d.point((x, y), fill=C_ROCK_LIGHT)
    # Left wall outline
    for y in range(0, 26):
        d.point((3, y), fill=C_OUTLINE)
        d.point((4, y), fill=C_OUTLINE)
    # Bottom icicle
    for dy in range(6):
        if 24 + dy < 32:
            d.point((10, 24 + dy), fill=C_ICE_DRIP)
            d.point((18, 24 + dy), fill=C_ICE_DRIP)
    return im

# 9. esquina_inf_der (Bottom-right corner)
def make_esquina_inf_der():
    im = make_esquina_inf_izq()
    return im.transpose(Image.FLIP_LEFT_RIGHT)

def main():
    out_dir = r"c:\Users\dubcr\Downloads\Peakingu-main\Peakingu-main\Assets\Sprites\Tiles"
    os.makedirs(out_dir, exist_ok=True)
    
    tiles = {
        "esquina_sup_izq": make_esquina_sup_izq(),
        "borde_sup":       make_borde_sup(),
        "esquina_sup_der": make_esquina_sup_der(),
        "borde_izq":       make_borde_izq(),
        "relleno":         make_relleno(),
        "borde_der":       make_borde_der(),
        "esquina_inf_izq": make_esquina_inf_izq(),
        "borde_inf":       make_borde_inf(),
        "esquina_inf_der": make_esquina_inf_der(),
    }
    
    # Save 9 individual PNGs
    for name, im in tiles.items():
        p = os.path.join(out_dir, f"{name}.png")
        im.save(p)
        print(f"Saved: {p}")
        
    # Also save combined 3x3 tileset image (96 x 96)
    sheet = Image.new("RGBA", (96, 96), (0, 0, 0, 0))
    grid = [
        ["esquina_sup_izq", "borde_sup", "esquina_sup_der"],
        ["borde_izq",       "relleno",   "borde_der"],
        ["esquina_inf_izq", "borde_inf", "esquina_inf_der"]
    ]
    for row_idx, row in enumerate(grid):
        for col_idx, name in enumerate(row):
            sheet.paste(tiles[name], (col_idx * 32, row_idx * 32))
            
    sheet_path = os.path.join(out_dir, "Winter_Tileset_3x3.png")
    sheet.save(sheet_path)
    print(f"Saved tileset sheet: {sheet_path}")

if __name__ == "__main__":
    main()
