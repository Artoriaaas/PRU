import re

scene_path = r"d:\GitHub\PRU\PRU\Assets\Scenes\2D5_Scene.unity"
with open(scene_path, "r", encoding="utf-8", errors="ignore") as f:
    content = f.read()

target_names = ["Plane", "Plane_Player", "Plane_Enemy", "Quad", "Quad_Player", "Quad_Enemy"]

chunks = content.split("--- !u!1 &")
for chunk in chunks:
    name_match = re.search(r"m_Name:\s*([^\n]+)", chunk)
    if name_match:
        go_name = name_match.group(1).strip()
        if go_name in target_names:
            go_id = re.search(r"^(\d+)", chunk)
            if go_id:
                print(f"GameObject: {go_name} (ID: {go_id.group(1)})")
                comp_matches = re.findall(r"-\s*component:\s*{\s*fileID:\s*(\d+)\s*}", chunk)
                for file_id in comp_matches:
                    trans_match = re.search(rf"--- !u!(4|224) &{file_id}\b", content)
                    if trans_match:
                        type_name = "Transform" if trans_match.group(1) == "4" else "RectTransform"
                        trans_chunk = content[trans_match.start():trans_match.start()+1500]
                        pos_match = re.search(r"m_LocalPosition:\s*{x:\s*([^,]+),\s*y:\s*([^,]+),\s*z:\s*([^}]+)}", trans_chunk)
                        rot_match = re.search(r"m_LocalRotation:\s*{x:\s*([^,]+),\s*y:\s*([^,]+),\s*z:\s*([^,]+),\s*w:\s*([^}]+)}", trans_chunk)
                        scale_match = re.search(r"m_LocalScale:\s*{x:\s*([^,]+),\s*y:\s*([^,]+),\s*z:\s*([^}]+)}", trans_chunk)
                        euler_match = re.search(r"m_LocalEulerAnglesHint:\s*{x:\s*([^,]+),\s*y:\s*([^,]+),\s*z:\s*([^}]+)}", trans_chunk)
                        
                        pos_str = f"{pos_match.group(1)}, {pos_match.group(2)}, {pos_match.group(3)}" if pos_match else "unknown"
                        euler_str = f"{euler_match.group(1)}, {euler_match.group(2)}, {euler_match.group(3)}" if euler_match else "unknown"
                        scale_str = f"{scale_match.group(1)}, {scale_match.group(2)}, {scale_match.group(3)}" if scale_match else "unknown"
                        
                        print(f"  --- {type_name} &{file_id} ---")
                        print(f"    Position: {pos_str}")
                        print(f"    Rotation (Euler): {euler_str}")
                        print(f"    Scale: {scale_str}")
