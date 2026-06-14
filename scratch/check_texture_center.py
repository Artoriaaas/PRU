import os
from PIL import Image

image_path = r"d:\GitHub\PRU\PRU\Assets\Textures\MagicCircle.png"

if not os.path.exists(image_path):
    print("Texture file not found at " + image_path)
    exit()

try:
    img = Image.open(image_path)
    img = img.convert("RGBA")
    width, height = img.size
    
    # Let's find the bounding box of non-transparent pixels (alpha > 10)
    left = width
    right = 0
    top = height
    bottom = 0
    
    for y in range(height):
        for x in range(width):
            r, g, b, a = img.getpixel((x, y))
            if a > 10:
                if x < left: left = x
                if x > right: right = x
                if y < top: top = y
                if y > bottom: bottom = y
                
    if right >= left and bottom >= top:
        center_x = (left + right) / 2.0
        center_y = (top + bottom) / 2.0
        canvas_center_x = width / 2.0
        canvas_center_y = height / 2.0
        print(f"Image dimensions: {width}x{height}")
        print(f"Bounding box: Left={left}, Right={right}, Top={top}, Bottom={bottom}")
        print(f"Content Center: ({center_x}, {center_y})")
        print(f"Canvas Center: ({canvas_center_x}, {canvas_center_y})")
        print(f"Offset: X={center_x - canvas_center_x}, Y={center_y - canvas_center_y}")
    else:
        print("No opaque pixels found!")
except Exception as e:
    print("Error reading image:", e)
