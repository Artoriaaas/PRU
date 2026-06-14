import os

project_dir = r"d:\GitHub\PRU\PRU"
for root, dirs, files in os.walk(project_dir):
    for file in files:
        if file.endswith(".cs"):
            # print relative path
            rel = os.path.relpath(os.path.join(root, file), project_dir)
            print(rel)
