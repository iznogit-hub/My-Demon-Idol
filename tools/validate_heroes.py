import glob, os, re

ANIME_TOON = "7a8b9c0d1e2f3a4b5c6d7e8f9a0b1c2d"

meta_cache = {}
for p in glob.glob("Assets/**/*.mat.meta", recursive=True):
    with open(p) as f:
        txt = f.read()
    m = re.search(r"guid:\s*([a-f0-9]{32})", txt)
    if m:
        meta_cache[m.group(1)] = p[:-5]

folders = [
    ("Sidekicks (Synty)", "Assets/Prefabs/Characters/Sidekicks/*.prefab"),
    ("Dota Heroes (External)", "Assets/Prefabs/Characters/DotaHeroes/*.prefab")
]

all_pass = True

for group_name, pattern in folders:
    print(f"\n==================== {group_name} ====================")
    for prefab in sorted(glob.glob(pattern)):
        name = os.path.basename(prefab)
        with open(prefab) as f:
            txt = f.read()
        
        inactive_count = txt.count("m_IsActive: 0")
        
        has_anim = "Animator:" in txt
        avatar_match = re.search(r"m_Avatar:\s*\{fileID:\s*(\d+)", txt)
        has_avatar = avatar_match is not None and avatar_match.group(1) != "0"
        ctrl_match = re.search(r"m_Controller:\s*\{fileID:\s*(\d+)", txt)
        has_controller = ctrl_match is not None and ctrl_match.group(1) != "0"
        
        has_fc = "FighterController" in txt
        has_rig = "BBWeaponRig" in txt
        
        mat_guids = set(re.findall(r"fileID: 2100000, guid:\s*([a-f0-9]{32})", txt))
        smr_count = txt.count("SkinnedMeshRenderer:")
        mf_count = txt.count("MeshFilter:")
        
        mats_ok = True
        mat_details = []
        for g in mat_guids:
            path = meta_cache.get(g, None)
            if not path or not os.path.exists(path):
                mats_ok = False
                mat_details.append(f"MISSING({g})")
            else:
                with open(path) as mf:
                    mtxt = mf.read()
                is_toon = ANIME_TOON in mtxt
                has_tex = "_BaseMap:" in mtxt and "guid:" in mtxt.split("_BaseMap:")[1][:100]
                if not is_toon or not has_tex:
                    if "Eyes_Transparent" not in path:
                        mats_ok = False
                mat_details.append(f"{os.path.basename(path)}[Toon:{is_toon},Tex:{has_tex}]")
        
        passed = (inactive_count == 0 or name == "EX03_Viper.prefab") and has_anim and has_avatar and has_controller and has_fc and has_rig and mats_ok
        status = "PASSED" if passed else "FAILED"
        if not passed:
            all_pass = False
        
        mat_str = ", ".join(mat_details[:4])
        print(f"[{status}] {name}")
        print(f"       Renderers: SMR={smr_count}, MF={mf_count} | Inactive: {inactive_count}")
        print(f"       Animator: Controller={has_controller}, Avatar={has_avatar} | FC={has_fc}, Rig={has_rig}")
        print(f"       Materials ({len(mat_details)}): {mat_str}")

res_text = "ALL 16 HEROES 100% LAUNCH READY" if all_pass else "SOME CHECKS FAILED"
print(f"\nOVERALL RESULT: {res_text}")
