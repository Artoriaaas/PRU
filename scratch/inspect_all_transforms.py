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
                print(f"=====================================")
                print(f"GameObject: {go_name} (ID: {go_id.group(1)})")
                comp_matches = re.findall(r"-\s*component:\s*{\s*fileID:\s*(\d+)\s*}", chunk)
                for file_id in comp_matches:
                    trans_match = re.search(rf"--- !u!(4|224) &{file_id}\b", content)
                    if trans_match:
                        type_name = "Transform" if trans_match.group(1) == "4" else "RectTransform"
                        trans_chunk = content[trans_match.start():trans_match.start()+1500]
                        
                        # Grab all positional/rotational fields we care about
                        pos_match = re.search(r"m_LocalPosition:\s*{x:\s*([^,]+),\s*y:\s*([^,]+),\s*z:\s*([^}]+)}", trans_chunk)
                        rot_match = re.search(r"m_LocalRotation:\s*{x:\s*([^,]+),\s*y:\s*([^,]+),\s*z:\s*([^,]+),\s*w:\s*([^}]+)}", trans_chunk)
                        scale_match = re.search(r"m_LocalScale:\s*{x:\s*([^,]+),\s*y:\s*([^,]+),\s*z:\s*([^}]+)}", trans_chunk)
                        euler_match = re.search(r"m_LocalEulerAnglesHint:\s*{x:\s*([^,]+),\s*y:\s*([^,]+),\s*z:\s*([^}]+)}", trans_chunk)
                        anchored_match = re.search(r"m_AnchoredPosition:\s*{x:\s*([^,]+),\s*y:\s*([^}]+)}", trans_chunk)
                        
                        print(f"  Component: {type_name} &{file_id}")
                        if pos_match: print(f"    m_LocalPosition: {{x: {pos_match.group(1)}, y: {pos_match.group(2)}, z: {pos_match.group(3)}}}")
                        if anchored_match: print(f"    m_AnchoredPosition: {{x: {anchored_match.group(1)}, y: {anchored_match.group(2)}}}")
                        if euler_match: print(f"    m_LocalEulerAnglesHint: {{x: {euler_match.group(1)}, y: {euler_match.group(2)}, z: {euler_match.group(3)}}}")
                        if scale_match: print(f"    m_LocalScale: {{x: {scale_match.group(1)}, y: {scale_match.group(2)}, z: {scale_match.group(3)}}}")
