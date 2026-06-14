import re

scene_path = r"d:\GitHub\PRU\PRU\Assets\Scenes\2D5_Scene.unity"
with open(scene_path, "r", encoding="utf-8", errors="ignore") as f:
    content = f.read()

# Let's find some PlayerPads and EnemyPads
chunks = content.split("--- !u!1 &")
for chunk in chunks:
    name_match = re.search(r"m_Name:\s*((?:PlayerPad|EnemyPad|Plane|Quad|BattlefieldLayout).*)\b", chunk)
    if name_match:
        go_name = name_match.group(1).strip()
        go_id = re.search(r"^(\d+)", chunk)
        if go_id:
            # Let's find its Transform/RectTransform component
            comp_matches = re.findall(r"-\s*component:\s*{\s*fileID:\s*(\d+)\s*}", chunk)
            for file_id in comp_matches:
                # check both Transform (!u!4) and RectTransform (!u!224)
                trans_match = re.search(rf"--- !u!(4|224) &{file_id}\b", content)
                if trans_match:
                    type_name = "Transform" if trans_match.group(1) == "4" else "RectTransform"
                    trans_chunk = content[trans_match.start():trans_match.start()+1500]
                    pos_match = re.search(r"m_LocalPosition:\s*{x:\s*([^,]+),\s*y:\s*([^,]+),\s*z:\s*([^}]+)}", trans_chunk)
                    father_match = re.search(r"m_Father:\s*{\s*fileID:\s*(\d+)\s*}", trans_chunk)
                    
                    pos_str = f"{pos_match.group(1)}, {pos_match.group(2)}, {pos_match.group(3)}" if pos_match else "unknown"
                    father_str = father_match.group(1) if father_match else "none"
                    print(f"GameObject: {go_name} (ID: {go_id.group(1)}) has component !u!{trans_match.group(1)} &{file_id}:")
                    print(f"  LocalPosition: {pos_str}")
                    print(f"  Father: {father_str}")
