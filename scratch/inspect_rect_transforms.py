import re

scene_path = r"d:\GitHub\PRU\PRU\Assets\Scenes\2D5_Scene.unity"
with open(scene_path, "r", encoding="utf-8", errors="ignore") as f:
    content = f.read()

rect_ids = ['926437254', '998596326', '646022122']
names = ["Plane", "Plane_Player", "Plane_Enemy"]
for idx, rid in enumerate(rect_ids):
    pattern = rf"--- !u!224 &{rid}\s*RectTransform:"
    match = re.search(pattern, content)
    if match:
        start_idx = match.start()
        trans_chunk = content[start_idx:start_idx+1500]
        pos_match = re.search(r"m_LocalPosition:\s*{x:\s*([^,]+),\s*y:\s*([^,]+),\s*z:\s*([^}]+)}", trans_chunk)
        rot_match = re.search(r"m_LocalRotation:\s*{x:\s*([^,]+),\s*y:\s*([^,]+),\s*z:\s*([^,]+),\s*w:\s*([^}]+)}", trans_chunk)
        scale_match = re.search(r"m_LocalScale:\s*{x:\s*([^,]+),\s*y:\s*([^,]+),\s*z:\s*([^}]+)}", trans_chunk)
        
        print(f"RectTransform for {names[idx]} (ID: {rid}):")
        if pos_match:
            print(f"  LocalPosition: {pos_match.group(1)}, {pos_match.group(2)}, {pos_match.group(3)}")
        if rot_match:
            print(f"  LocalRotation: {rot_match.group(1)}, {rot_match.group(2)}, {rot_match.group(3)}, {rot_match.group(4)}")
        if scale_match:
            print(f"  LocalScale: {scale_match.group(1)}, {scale_match.group(2)}, {scale_match.group(3)}")
