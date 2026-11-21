# Test complete moveset mapping for all 25 fighters
import os

ROSTER = [
    ("C01", "Akio", "Martial Art Animations", "Fighter Pack Bundle"),
    ("C02", "Aika", "Martial Art Animations", "Fighter Pack Bundle"),
    ("C04", "UnityChan", "Fighter Pack Bundle", "Martial Art Animations"),
    ("C05", "Akane", "Warrior Bundle 3", "Human Melee Animations"),
    ("C06", "Cultist", "Human Spell Casting Animations", "Warrior Bundle 1"),
    ("C07", "StellarWitch", "Human Spell Casting Animations", "Martial Art Animations"),
    ("C08", "SanpakuChan", "Warrior Bundle 1", "Martial Art Animations"),
    ("C09", "PicoChan", "Fighter Pack Bundle", "Martial Art Animations"),
    ("C10", "Reika", "Human Melee Animations", "Warrior Bundle 2"),
    ("C11", "Casual1", "Fighter Pack Bundle", "Martial Art Animations"),
    ("C12", "Fina", "Martial Art Animations", "Fighter Pack Bundle"),
    ("C13", "SapphiArtchan", "Human Spell Casting Animations", "Martial Art Animations"),
    ("C14", "Muryotaisu", "Warrior Bundle 2", "Human Melee Animations"),
    ("SK02", "Galahad", "Warrior Bundle 2", "Human Melee Animations"),
    ("SK03", "Vex", "Warrior Bundle 3", "Warrior Bundle 1"),
    ("SK05", "LordMorvath", "Warrior Bundle 2", "Human Spell Casting Animations"),
    ("SK06", "CommanderRen", "Human Melee Animations", "Warrior Bundle 3"),
    ("SK08", "Torgrim", "Warrior Bundle 3", "Dwarf Animations"),
    ("EX01", "Hugo", "Warrior Bundle 1", "Fighter Pack Bundle"),
    ("EX02", "Granite", "Warrior Bundle 3", "Warrior Bundle 1"),
    ("EX03", "Viper", "Warrior Bundle 3", "Warrior Bundle 1"),
    ("EX04", "OfficerK", "Warrior Bundle 1", "Martial Art Animations"),
    ("EX05", "Kira", "Warrior Bundle 2", "Warrior Bundle 3"),
    ("EX06", "Sentry", "Warrior Bundle 3", "Human Melee Animations"),
    ("EX07", "Lyra", "Warrior Bundle 1", "Human Spell Casting Animations"),
]

def check_fighter(fid, name, p, s):
    # Determine moves list
    used_clips = {}
    collisions = []
    
    def add_move(mId, clip, is_attack=True):
        if not is_attack:
            return
        # check air/ground pair
        if mId.startswith("j.") and mId in ["j.A", "j.B", "j.C", "j.D"]:
            ground = "5" + mId[2:]
            if used_clips.get(clip) == ground:
                return # permitted
        if clip in used_clips:
            collisions.append(f"Move {mId} reuses clip [{clip}] already used by [{used_clips[clip]}]")
        else:
            used_clips[clip] = mId

    # Striker
    if p in ["Martial Art Animations", "Fighter Pack Bundle"] or fid in ["C01", "C02", "C04", "C09", "C11", "C12", "EX04"]:
        add_move("5A", "Atk_P_1")
        add_move("5B", "Atk_P_2")
        add_move("5C", "Atk_P_3")
        add_move("5D", "Atk_K_3")
        add_move("2A", "Atk_K_1")
        add_move("2B", "Atk_K_2")
        add_move("2C", "Grab_Break_A")
        add_move("2D", "Grab_Break_B")
        add_move("6A", "Quickstep_F")
        add_move("6B", "Quickstep_B")
        add_move("6C", "Dodge_F")
        add_move("3C", "Dodge_B")
        add_move("j.A", "Atk_P_1")
        add_move("j.B", "Atk_P_2")
        add_move("j.C", "Atk_P_3")
        add_move("j.D", "Atk_K_3")
        add_move("BC", "Grab_Start")
        add_move("6BC", "Grab_Hit_A")
        add_move("4BC", "Grab_Hit_B")
        add_move("j.BC", "HumanM@AttackShield01")
        add_move("236A", "Female@Punch")
        add_move("236B", "Heavy@Punch")
        add_move("236C", "Berserker@Punch")
        add_move("214A", "Male@Punch")
        add_move("623B", "Karate_Roundhouse")
        add_move("22C", "Karate_Palm")
        add_move("236236C", "Karate_Jab")
        if fid in ["C01", "C02", "C04", "C09", "C12"]:
            # extra moves
            add_move("5D_2", "Brute_Hook")
            add_move("5D_3", "Brute_Uppercut")
            add_move("5D_4", "Brute_DoubleSlam")
            add_move("6D", "Ninja_Slash1")
            add_move("632146D", "Ninja_Slash2")
            add_move("OD_EA", "Ninja_Cross")
            add_move("214B", "Warrior@Punch")
    elif "Spell" in p or fid in ["C06", "C07", "C13", "SK05", "EX07"]:
        add_move("5A", "HumanF@MagicAttackDirect1H01_R")
        add_move("5B", "HumanF@MagicAttackDirect1H01_L")
        add_move("5C", "HumanF@MagicAttackDirect2H01")
        add_move("5D", "HumanF@MagicAttackDirectCast01")
        add_move("2A", "HumanF@CastingEnter01")
        add_move("2B", "HumanF@CastingIdle01")
        add_move("2C", "HumanF@CastingExit01")
        add_move("2D", "HumanF@MagicAttackCall1H01_R")
        add_move("6A", "HumanF@MagicAttackCall1H01_L")
        add_move("6B", "HumanF@MagicAttackCall2H01")
        add_move("6C", "HumanF@MagicAttackCallCast01")
        add_move("3C", "HumanF@MagicAttackOmni01")
        add_move("j.A", "HumanF@MagicAttackDirect1H01_R")
        add_move("j.B", "HumanF@MagicAttackDirect1H01_L")
        add_move("j.C", "HumanF@MagicAttackDirect2H01")
        add_move("j.D", "HumanF@MagicAttackDirectCast01")
        add_move("BC", "HumanF@AttackShield01")
        add_move("6BC", "HumanM@AttackShield01")
        add_move("4BC", "HumanF@Attack1H01_R")
        add_move("j.BC", "HumanM@Attack1H01_R")
        add_move("236A", "Sorceress_Burst")
        add_move("236B", "Sorceress_Wave")
        add_move("236C", "Sorceress_Nova")
        add_move("214A", "Mage_StaffPoke")
        add_move("623B", "Mage_OrbCast")
        add_move("22C", "Mage_Eruption")
        add_move("236236C", "HumanM@MagicAttackOmni01")
        if fid in ["C06", "C07"]:
            add_move("5D_2", "Ninja_Slash1")
            add_move("5D_3", "Ninja_Slash2")
            add_move("5D_4", "Ninja_Cross")
            add_move("6D", "Karate_Roundhouse")
            add_move("632146D", "Karate_Jab")
            add_move("OD_EA", "Brute_DoubleSlam")
            add_move("214B", "Brute_Hook")
    else:
        # Weapons
        pfx = "Swordsman"
        if "Spear" in p or fid in ["C05", "EX06"]: pfx = "Spearman"
        elif "Hammer" in p or fid in ["SK08", "EX02"]: pfx = "Hammer"
        elif "Knight" in p or fid in ["SK02"]: pfx = "Knight"
        elif "2Handed" in p or fid in ["C14"]: pfx = "TwoHanded"
        elif "Archer" in p or fid in ["EX05"]: pfx = "Archer"
        elif "Crossbow" in p or fid in ["C11"]: pfx = "Crossbow"
        elif "Brute" in p or fid in ["SK03", "EX01"]: pfx = "Brute"
        elif "Ninja" in p or fid in ["C08"]: pfx = "Ninja"

        s1, s2, s3 = f"{pfx}_1", f"{pfx}_2", f"{pfx}_3"
        add_move("5A", s1)
        add_move("5B", s2)
        add_move("5C", s3)
        add_move("5D", "HumanF@AttackPolearm01")
        add_move("2A", "HumanF@Attack1H01_R")
        add_move("2B", "HumanF@Attack1H01_L")
        add_move("2C", "HumanF@Attack2H01")
        add_move("2D", "HumanM@AttackPolearm01")
        add_move("6A", "HumanM@Attack1H01_R")
        add_move("6B", "HumanM@Attack1H01_L")
        add_move("6C", "HumanM@Attack2H01")
        add_move("3C", "HumanF@CombatIdle01_atk")
        add_move("j.A", s1)
        add_move("j.B", s2)
        add_move("j.C", s3)
        add_move("j.D", "HumanF@AttackPolearm01_air")
        add_move("BC", "HumanM@AttackShield01")
        add_move("6BC", "HumanF@CombatIdle1H01")
        add_move("4BC", "HumanM@CombatIdle1H01")
        add_move("j.BC", "HumanM@CombatDamage01")
        add_move("236A", "TwoHanded_Sweep")
        add_move("236B", "TwoHanded_Slash")
        add_move("236C", "TwoHanded_Slam")
        add_move("214A", "Knight_Slash")
        add_move("623B", "Knight_ShieldBash")
        add_move("22C", "Knight_Cleave")
        add_move("236236C", "HumanF@MagicAttackOmni01")
        if fid in ["C08", "C10"]:
            add_move("5D_2", "Archer_QuickDraw")
            add_move("5D_3", "Archer_PowerShot")
            add_move("5D_4", "Archer_Volley")
            add_move("6D", "Crossbow_Snapshot")
            add_move("632146D", "Crossbow_Burst")
            add_move("OD_EA", "Crossbow_Scatter")
            add_move("214B", "Hammer_Swing")

    return collisions

all_ok = True
for fid, name, p, s in ROSTER:
    col = check_fighter(fid, name, p, s)
    if col:
        print(f"FAILED {fid} {name}: {col}")
        all_ok = False
    else:
        print(f"OK {fid} {name}")

if all_ok:
    print("ALL 25 FIGHTERS PASS ZERO COLLISIONS!")
