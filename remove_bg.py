from PIL import Image
import numpy as np
import os

files = [
    "Assets/Resources/1_tro_choi_moi.png",
    "Assets/Resources/2_cai_dat.png",
    "Assets/Resources/3_thoat.png",
    "Assets/Resources/4_nha_san_xuat.png",
    "Assets/Resources/5_tiep_tuc.png",
]

# Threshold - pixels "gần trắng" sẽ bị transparent
WHITE_THRESHOLD = 240
# Mức mờ dần ở vùng biên (anti-alias)
EDGE_SOFT = 20

for rel_path in files:
    path = os.path.join(os.path.dirname(__file__), rel_path)
    img = Image.open(path).convert("RGBA")
    data = np.array(img, dtype=np.float32)

    r, g, b, a = data[:,:,0], data[:,:,1], data[:,:,2], data[:,:,3]

    # Tính độ "trắng" của từng pixel
    whiteness = np.minimum(np.minimum(r, g), b)  # pixel trắng khi R,G,B đều cao

    # Tạo alpha mask:
    # - pixel >= WHITE_THRESHOLD → hoàn toàn transparent (alpha = 0)
    # - pixel trong khoảng (WHITE_THRESHOLD - EDGE_SOFT, WHITE_THRESHOLD) → mờ dần
    # - pixel < WHITE_THRESHOLD - EDGE_SOFT → giữ nguyên alpha
    new_alpha = np.clip(
        (WHITE_THRESHOLD - whiteness) / EDGE_SOFT * 255,
        0, 255
    )

    # Chỉ áp dụng cho pixel mà alpha gốc còn cao (không bị trong sẵn)
    data[:,:,3] = np.where(a > 10, new_alpha, 0)

    result = Image.fromarray(data.astype(np.uint8), "RGBA")
    result.save(path)
    print(f"Done: {rel_path}")

print("\nAll images processed!")
