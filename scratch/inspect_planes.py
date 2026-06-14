import re

scene_path = r"d:\GitHub\PRU\PRU\Assets\Scenes\2D5_Scene.unity"
with open(scene_path, "r", encoding="utf-8", errors="ignore") as f:
    content = f.read()

plane_names = ["Plane", "Plane_Player", "Plane_Enemy"]
for p_name in plane_names:
    # Find block
    pattern = rf"GameObject:\s*\n\s*m_ObjectHideFlags:[^\n]*\n[^\n]*\n[^\n]*\n[^\n]*\n\s*serializedVersion: 6\n\s*m_Component:\n((?:\s*-\s*component:\s*{{\s*fileID:\s*\d+\s*}}\n?)+)\s*m_Layer:[^\n]*\n\s*m_Name:\s*{p_name}\b"
    match = re.search(pattern, content)
    if match:
        print(f"Found {p_name} GameObject:")
        comp_text = match.group(1)
        comp_ids = re.findall(rf"fileID:\s*(\d+)", comp_text)
        print(f"  Component IDs: {comp_ids}")
        for cid in comp_ids:
            # Look for cid in content
            cid_pattern = rf"--- !u!(\d+) &{cid}\b"
            cid_match = re.search(cid_pattern, content)
            if cid_match:
                type_id = cid_match.group(1)
                # print first 5 lines of this block
                start_idx = cid_match.start()
                block_lines = content[start_idx:start_idx+600].split("\n")
                print(f"  --- Component !u!{type_id} &{cid} ---")
                for line in block_lines[:8]:
                    print(f"    {line}")
    else:
        print(f"Could not find GameObject block for {p_name}")
