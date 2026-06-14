import re

scene_path = r"d:\GitHub\PRU\PRU\Assets\Scenes\2D5_Scene.unity"
with open(scene_path, "r", encoding="utf-8", errors="ignore") as f:
    content = f.read()

# Let's find SceneManagers
matches = list(re.finditer(r"m_Name:\s*SceneManagers\b", content))
if matches:
    for match in matches:
        start_idx = match.start()
        # Find backward to the start of the GameObject
        go_start_match = list(re.finditer(r"GameObject:", content[:start_idx]))
        if go_start_match:
            go_start_idx = go_start_match[-1].start()
            go_chunk = content[go_start_idx:start_idx+500]
            print("GameObject Chunk:")
            print("\n".join(go_chunk.split("\n")[:15]))
            # Find all component IDs
            comp_ids = re.findall(r"fileID:\s*(\d+)", go_chunk)
            print(f"Component IDs: {comp_ids}")
            for cid in comp_ids:
                # Find this component in content
                comp_match = re.search(r"--- !u!(\d+) &" + cid + r"\b", content)
                if comp_match:
                    type_id = comp_match.group(1)
                    comp_chunk = content[comp_match.start():comp_match.start()+1000]
                    lines = comp_chunk.split("\n")
                    print(f"\n--- Component !u!{type_id} &{cid} ---")
                    for line in lines[:15]:
                        print(f"  {line}")
else:
    print("Could not find SceneManagers GameObject")
