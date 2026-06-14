import os
import re

scripts_dir = r"d:\GitHub\PRU\PRU\Assets\Scripts"
for root, dirs, files in os.walk(scripts_dir):
    for file in files:
        if file.endswith(".cs"):
            path = os.path.join(root, file)
            with open(path, "r", encoding="utf-8", errors="ignore") as f:
                lines = f.readlines()
            for idx, line in enumerate(lines):
                if ".position" in line or ".localPosition" in line or "Instantiate" in line:
                    # ignore comments
                    if not line.strip().startswith("//"):
                        print(f"{file}:{idx+1}: {line.strip()}")
