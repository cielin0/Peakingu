from PIL import Image, ImageDraw
import os

w, h = 32, 48
img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
draw = ImageDraw.Draw(img)

# Wooden / metallic pole
pole_x = 8
for y in range(4, 46):
    draw.line([(pole_x, y), (pole_x + 1, y)], fill=(120, 135, 160, 255))
draw.ellipse([pole_x - 2, 2, pole_x + 3, 7], fill=(255, 215, 0, 255), outline=(180, 150, 0, 255))

# Golden Fish Flag
flag_x = pole_x + 2
flag_y = 6
# Flag body
draw.polygon([
    (flag_x, flag_y),
    (flag_x + 18, flag_y + 6),
    (flag_x + 12, flag_y + 13),
    (flag_x + 18, flag_y + 20),
    (flag_x, flag_y + 14)
], fill=(255, 195, 30, 255), outline=(200, 130, 10, 255))

# Fish silhouette on the flag
draw.ellipse([flag_x + 3, flag_y + 4, flag_x + 11, flag_y + 10], fill=(255, 245, 180, 255))
draw.polygon([(flag_x + 10, flag_y + 7), (flag_x + 13, flag_y + 4), (flag_x + 13, flag_y + 10)], fill=(255, 245, 180, 255))

# Snow base
draw.ellipse([2, 42, 28, 47], fill=(220, 240, 255, 255), outline=(140, 180, 215, 255))

out_path = r"c:\Users\dubcr\Downloads\Peakingu-main\Peakingu-main\Assets\Sprites\LevelGoal_Flag.png"
os.makedirs(os.path.dirname(out_path), exist_ok=True)
img.save(out_path)
print(f"Goal flag sprite saved to {out_path}")
