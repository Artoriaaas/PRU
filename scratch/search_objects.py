import re

scene_path = r"d:\GitHub\PRU\PRU\Assets\Scenes\2D5_Scene.unity"
with open(scene_path, "r", encoding="utf-8", errors="ignore") as f:
    content = f.read()

names = re.findall(r"m_Name:\s*(.*)", content)
print(f"Total GameObject names found: {len(names)}")
for name in sorted(set(names)):
    print(f" - {name}")
