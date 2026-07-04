from PIL import Image
import os

files = [
    "Assets/Resources/1_tro_choi_moi.png",
    "Assets/Resources/2_cai_dat.png",
    "Assets/Resources/3_thoat.png",
    "Assets/Resources/4_nha_san_xuat.png",
    "Assets/Resources/5_tiep_tuc.png",
]

TARGET_W = 1840
TARGET_H = 384

for rel_path in files:
    path = os.path.join(os.path.dirname(__file__), rel_path)
    img = Image.open(path).convert("RGBA")

    # Bước 1: Crop về đúng vùng có nội dung (bỏ transparent padding)
    bbox = img.getbbox()
    if bbox:
        img = img.crop(bbox)

    ow, oh = img.size
    print(f"  Content size: {ow}x{oh}")

    # Bước 2: Scale fill (max scale) để lấp đầy target, rồi crop giữa
    scale = max(TARGET_W / ow, TARGET_H / oh)
    new_w = int(ow * scale)
    new_h = int(oh * scale)
    resized = img.resize((new_w, new_h), Image.LANCZOS)

    # Crop giữa
    left   = (new_w - TARGET_W) // 2
    top    = (new_h - TARGET_H) // 2
    right  = left + TARGET_W
    bottom = top  + TARGET_H
    final  = resized.crop((left, top, right, bottom))

    final.save(path)
    print(f"Done: {rel_path}  (content {ow}x{oh} -> {TARGET_W}x{TARGET_H})")

print("\nAll images re-unified correctly!")
