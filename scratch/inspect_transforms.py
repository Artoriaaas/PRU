import re

scene_path = r"d:\GitHub\PRU\PRU\Assets\Scenes\2D5_Scene.unity"
with open(scene_path, "r", encoding="utf-8", errors="ignore") as f:
    content = f.read()

# Let's find GameObjects with their names and their corresponding transforms
chunks = content.split("--- !u!1 &")
for chunk in chunks:
    # regex match names starting with Plane or Quad
    name_match = re.search(r"m_Name:\s*((?:Plane|Quad|BattlefieldLayout|PlayerGrid|EnemyGrid).*)\b", chunk)
    if name_match:
        go_name = name_match.group(1).strip()
        go_id = re.search(r"^(\d+)", chunk)
        if go_id:
            print(f"GameObject: {go_name} (ID: {go_id.group(1)})")
            # Let's find its Transform component
            comp_matches = re.findall(r"-\s*component:\s*{\s*fileID:\s*(\d+)\s*}", chunk)
            for file_id in comp_matches:
                trans_pattern = f"--- !u!4 &{file_id}\\s*Transform:"
                trans_match = re.search(trans_pattern, content)
                if trans_match:
                    # extract localPosition
                    trans_chunk = content[trans_match.start():trans_match.start()+1500]
                    pos_match = re.search(r"m_LocalPosition:\s*{x:\s*([^,]+),\s*y:\s*([^,]+),\s*z:\s*([^}]+)}", trans_chunk)
                    rot_match = re.search(r"m_LocalRotation:\s*{x:\s*([^,]+),\s*y:\s*([^,]+),\s*z:\s*([^,]+),\s*w:\s*([^}]+)}", trans_chunk)
                    scale_match = re.search(r"m_LocalScale:\s*{x:\s*([^,]+),\s*y:\s*([^,]+),\s*z:\s*([^}]+)}", trans_chunk)
                    father_match = re.search(r"m_Father:\s*{\s*fileID:\s*(\d+)\s*}", trans_chunk)
                    
                    if pos_match:
                        print(f"  LocalPosition: {pos_match.group(1)}, {pos_match.group(2)}, {pos_match.group(3)}")
                    if rot_match:
                        print(f"  LocalRotation: {rot_match.group(1)}, {rot_match.group(2)}, {rot_match.group(3)}, {rot_match.group(4)}")
                    if scale_match:
                        print(f"  LocalScale: {scale_match.group(1)}, {scale_match.group(2)}, {scale_match.group(3)}")
                    if father_match:
                        print(f"  Father FileID: {father_match.group(1)}")
