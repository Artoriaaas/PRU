import re

scene_path = r"d:\GitHub\PRU\PRU\Assets\Scenes\2D5_Scene.unity"
with open(scene_path, "r", encoding="utf-8", errors="ignore") as f:
    content = f.read()

# Find SceneManagers block
pattern = r"GameObject:\s*\n\s*m_ObjectHideFlags:[^\n]*\n[^\n]*\n[^\n]*\n[^\n]*\n\s*serializedVersion: 6\n\s*m_Component:\n((?:\s*-\s*component:\s*{{\s*fileID:\s*\d+\s*}}\n?)+)\s*m_Layer:[^\n]*\n\s*m_Name:\s*SceneManagers\b"
match = re.search(pattern, content)
if match:
    print("Found SceneManagers GameObject:")
    comp_text = match.group(1)
    comp_ids = re.findall(r"fileID:\s*(\d+)", comp_text)
    print(f"  Component IDs: {comp_ids}")
    for cid in comp_ids:
        cid_pattern = r"--- !u!(\d+) &" + cid + r"\b"
        cid_match = re.search(cid_pattern, content)
        if cid_match:
            type_id = cid_match.group(1)
            # print script name if it is MonoBehaviour
            start_idx = cid_match.start()
            block_lines = content[start_idx:start_idx+1000].split("\n")
            print(f"  --- Component !u!{type_id} &{cid} ---")
            for line in block_lines[:12]:
                print(f"    {line}")
else:
    print("Could not find SceneManagers GameObject block")
