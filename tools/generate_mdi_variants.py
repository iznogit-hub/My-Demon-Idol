#!/usr/bin/env python3
"""Generate additive My Demon Idol character variants.

Writes only under Assets/Prefabs/Characters/Variants/ plus folder metas.
Does not modify existing prefabs, materials, or textures.

Primary assets are the YAML prefab variants. Unity can rebuild them with
Demon Idol/Characters/Rebuild Variant Prefabs From Recipe.
"""
from __future__ import annotations

import json
import re
import uuid
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path("/workspace")
ASSETS = ROOT / "Assets"
OUT = ASSETS / "Prefabs" / "Characters" / "Variants"
NS = uuid.UUID("c3a7e1b9-4d2f-4a68-b5e0-d7c81f6a4902")
TOON = "7a8b9c0d1e2f3a4b5c6d7e8f9a0b1c2d"
SCRIPT = "c3a7e1b94d2f4a68b5e0d7c81f6a4902"
BASE_FBX = "Assets/AnimeGirls/BaseCharacter/BaseCharacter/Sourcefiles/BaseCharacter.fbx"
HAIR_MESH = {1: (4300004, "Hair1"), 2: (4300002, "Hair2"), 3: (4300006, "Hair3"), 4: (4300012, "Hair4"), 5: (4300014, "Hair5")}
SKIN_UC = "Assets/unity-chan!/Unity-chan! Model/Art/UnityChanShader/Texture/skin_01.tga"
MAKEUP = "Assets/AnimeGirls/BaseCharacter/BaseCharacter/Sourcefiles/Tex/Tex_Face_makeup.png"

SKINS = [
    (0.99, 0.84, 0.76),
    (0.93, 0.73, 0.61),
    (0.84, 0.64, 0.50),
    (0.58, 0.40, 0.32),
    (0.99, 0.89, 0.86),
    (0.90, 0.70, 0.58),
]

ACC_PATH = {
    "sword": "Assets/MDI_Characters/Shared/Accessories/Accessory_Sword.fbx",
    "greatsword": "Assets/MDI_Characters/Shared/Accessories/Accessory_Greatsword.fbx",
    "dagger": "Assets/MDI_Characters/Shared/Accessories/Accessory_Dagger.fbx",
    "shield": "Assets/MDI_Characters/Shared/Accessories/Accessory_Shield.fbx",
    "bow": "Assets/MDI_Characters/Shared/Accessories/Accessory_Bow.fbx",
    "quiver": "Assets/MDI_Characters/Shared/Accessories/Accessory_Quiver.fbx",
    "hat": "Assets/MDI_Characters/Shared/Accessories/Accessory_MageHat.fbx",
    "mask": "Assets/MDI_Characters/Shared/Accessories/Accessory_Mask.fbx",
}
ACC_XFORM = {
    "sword": ("rightHand", (0.0, 0.02, 0.06), (90.0, 0.0, 90.0), (1.0, 1.0, 1.0)),
    "greatsword": ("rightHand", (0.0, 0.03, 0.08), (90.0, 0.0, 90.0), (1.05, 1.05, 1.05)),
    "dagger": ("rightHand", (0.0, 0.01, 0.04), (90.0, 0.0, 90.0), (0.8, 0.8, 0.8)),
    "shield": ("leftHand", (0.0, 0.02, 0.05), (0.0, 80.0, 0.0), (1.0, 1.0, 1.0)),
    "bow": ("leftHand", (0.0, 0.05, 0.02), (0.0, 0.0, 80.0), (1.0, 1.0, 1.0)),
    "quiver": ("spine", (0.06, 0.12, -0.14), (0.0, 0.0, 10.0), (1.0, 1.0, 1.0)),
    "hat": ("head", (0.0, 0.15, 0.02), (0.0, 0.0, 0.0), (1.0, 1.0, 1.0)),
    "mask": ("head", (0.0, 0.05, 0.08), (0.0, 0.0, 0.0), (1.0, 1.0, 1.0)),
}
PROFILES = {
    "angry": ["Fcl_ALL_Angry", "Fcl_BRW_Angry", "Fcl_EYE_Angry", "Fcl_MTH_Angry", "MTH_ANG2", "EYE_ANG2", "BLW_ANG2", "angry_face", "Angry", "angry", "brow_anger_L", "brow_anger_R"],
    "smile": ["Fcl_ALL_Joy", "Fcl_BRW_Joy", "Fcl_EYE_Joy", "MTH_SMILE1", "EYE_SMILE1", "BLW_SMILE1", "face_smile_BS", "Smile", "slight_smile", "smile", "Joy"],
    "fun": ["Fcl_ALL_Fun", "Fcl_BRW_Fun", "Fcl_EYE_Fun", "smile_eyes", "MTH_SMILE2", "EYE_SMILE2", "Fun", "shy"],
    "sad": ["Fcl_ALL_Sorrow", "Fcl_BRW_Sorrow", "Fcl_EYE_Sorrow", "face_sad_BS", "brow_sad_L", "brow_sad_R", "Sorrow", "sad", "Sad", "MTH_SAP", "EYE_SAP", "BLW_SAP"],
    "surprise": ["Fcl_ALL_Surprised", "Fcl_BRW_Surprised", "Fcl_EYE_Surprised", "MouthOpen", "surprise", "MTH_CONF", "EYE_CONF", "BLW_CONF", "scream"],
    "fangs": ["spike_teeth", "spike_teeth3", "spike_teet2"],
    "trouble": ["Trouble"],
    "ahh": ["Ahh"],
    "bigahh": ["Big Ahh"],
    "brow": ["eyeBrow Height"],
    "browup": ["brow_Up_L", "brow_Up_R"],
    "eyeclose": ["EyeClose"],
}
UC_BUCKET = {
    "uwagi": "jacket", "uwagi_BK": "jacket", "button": "trim",
    "shirts_sode": "shirt", "shirts_sode_BK": "shirt", "Shirts": "skirt",
    "hair_accce": "trim", "hairband": "ribbon", "Leg": "legs",
    "tail": "hair", "tail_bottom": "hair", "hair_frontside": "hair", "hair_front": "hair",
    "EYE_DEF": "face", "MTH_DEF": "face", "head_back": "face",
    "BLW_DEF": "keep", "EL_DEF": "keep", "cheek": "keep", "eye_base_old": "keep",
    "skin": "skin", "eye_R_old": "eye", "eye_L_old": "eye",
}


def guid_for(name: str) -> str:
    return uuid.uuid5(NS, name).hex


def fid_for(name: str) -> int:
    n = uuid.uuid5(NS, "fid:" + name).int % (2**62)
    return n if n >= 10**12 else n + 10**12


def V(name, base, desc, root, cloth, hair, eye, skin, **kw):
    kw.update(name=name, base=base, desc=desc, root=root, cloth=cloth, hair=hair, eye=eye, skin=skin)
    kw.setdefault("disable", ())
    kw.setdefault("scales", ())
    kw.setdefault("expr", "smile")
    kw.setdefault("gear", ())
    kw.setdefault("hair_n", 0)
    kw.setdefault("eye_n", 0)
    kw.setdefault("makeup", False)
    kw.setdefault("notes", "")
    kw.setdefault("hair_sat", 1.45)
    kw.setdefault("hair_val", 1.0)
    kw.setdefault("hair_mul", (1, 1, 1))
    kw.setdefault("cloth_sat", 1.4)
    kw.setdefault("cloth_val", 1.0)
    kw.setdefault("cloth_mul", (1, 1, 1))
    return kw


def variants():
    return [
        V("UnityChan_V1_ShortHairDuelist", "C04_UnityChan",
          "She wears her hair short: twin tails, tail tips, clips and head ribbons are off. Crimson jacket, black shirt, gold trim, fair skin, blue eyes, angry face, sword in her right hand.",
          1.0, 4, 18, 210, 0, disable=("tail", "tail_bottom", "hair_accce", "hairband"), expr="angry", gear=("sword",),
          notes="Per-garment copies of the shared body atlas recolor the jacket, shirt, skirt and legs separately."),
        V("UnityChan_V2_JacketlessScholar", "C04_UnityChan",
          "Jacket, vest back and buttons are off, so the shirt and skirt read on their own. Navy and gold, honey hair, a mage hat, and a calm smile. Slightly shorter.",
          0.96, 222, 36, 205, 1, disable=("uwagi", "uwagi_BK", "button"), expr="smile", gear=("hat",),
          scales=(("Hair", 1.08),)),
        V("UnityChan_V3_RibbonKnight", "C04_UnityChan",
          "Ribbons and twin tails are larger. White-and-gold uniform, warm eyes, sword and shield. Taller than the base.",
          1.05, 46, 42, 28, 4, expr="smile", gear=("sword", "shield"),
          scales=(("Hair", 1.16), ("Ribbon", 1.42))),
        V("UnityChan_V4_SleevelessNight", "C04_UnityChan",
          "Sleeves and the head ribbon are off. Violet-and-black clothes, near-black hair, red eyes, a half mask and a dagger.",
          1.0, 276, 280, 350, 2, disable=("shirts_sode", "shirts_sode_BK", "hairband"), expr="angry", gear=("dagger", "mask"),
          hair_sat=0.35, hair_val=0.55, hair_mul=(0.55, 0.45, 0.7)),
        V("UnityChan_V5_PetiteArcher", "C04_UnityChan",
          "Petite archer. Teal hair worn longer, forest clothes, green eyes, bow and quiver.",
          0.92, 132, 168, 145, 1, expr="fun", gear=("bow", "quiver"), scales=(("Hair", 1.28),)),
        V("PicoChan_V1_StreetMechanic", "C09_PicoChan",
          "Street clothes: jacket, goggles, bag robot and the four hair tails are off. Orange shirt, pink bob, angry expression, sword.",
          1.0, 18, 336, 205, 1, disable=("jacket_base", "jacket_sub", "jacket_fastener", "jacket_fastener_top", "goggle_base", "goggle_belt", "goggle_lens", "picolabo_base", "picolabo_metal", "hair_tail_01", "hair_tail_02", "hair_tail_03", "hair_tail_04"),
          expr="angry", gear=("sword",)),
        V("PicoChan_V2_HoodedPilot", "C09_PicoChan",
          "Hooded jacket stays. Goggles and the bag robot are off. Ice-blue clothes, blue hair, mage hat. A little shorter.",
          0.94, 208, 214, 198, 0, disable=("goggle_base", "goggle_belt", "goggle_lens", "picolabo_base", "picolabo_metal"),
          expr="smile", gear=("hat",), scales=(("Hair", 1.1),)),
        V("PicoChan_V3_BagCompanion", "C09_PicoChan",
          "Jacket off so the shirt shows. Goggles and a larger bag robot stay. Rose clothes, rose hair, cheerful face, dagger.",
          1.03, 332, 348, 300, 4, disable=("jacket_base", "jacket_sub", "jacket_fastener", "jacket_fastener_top"),
          expr="fun", gear=("dagger",), scales=(("picolabo", 1.35), ("Bag", 1.35))),
        V("PicoChan_V4_ShortHairKnight", "C09_PicoChan",
          "Hair tails off, jacket on in white and gold. Sword and shield. Tallest Pico variant.",
          1.06, 44, 32, 212, 2, disable=("hair_tail_01", "hair_tail_02", "hair_tail_03", "hair_tail_04"),
          expr="angry", gear=("sword", "shield")),
        V("PicoChan_V5_MageTinkerer", "C09_PicoChan",
          "Jacket, goggles and bag are off. Long purple hair, violet shirt, mage hat. Shortest Pico variant.",
          0.92, 278, 268, 250, 5, disable=("jacket_base", "jacket_sub", "jacket_fastener", "jacket_fastener_top", "goggle_base", "goggle_belt", "goggle_lens", "picolabo_base", "picolabo_metal"),
          expr="smile", gear=("hat",), scales=(("Hair", 1.22),)),
        V("Casual1_V1_Hair1SmileFencer", "C11_Casual1",
          "Hair 1, shipped eye colour 2, smile, warm casual outfit, fair skin, sword. One of the five shipped hairstyles.",
          1.0, 16, 24, 210, 0, expr="smile", gear=("sword",), hair_n=1, eye_n=2),
        V("Casual1_V2_Hair2FangedRogue", "C11_Casual1",
          "Hair 2, shipped eye colour 3, angry face with fangs, black-red outfit, darker hair, dagger.",
          0.95, 356, 4, 350, 3, expr="angry+fangs", gear=("dagger",), hair_n=2, eye_n=3,
          hair_sat=0.7, hair_val=0.45, hair_mul=(0.4, 0.15, 0.18), cloth_val=0.72),
        V("Casual1_V3_Hair3TiredMage", "C11_Casual1",
          "Hair 3, shipped eye colour 4, sad expression, mint outfit, mage hat.",
          1.0, 156, 142, 150, 4, expr="sad", gear=("hat",), hair_n=3, eye_n=4),
        V("Casual1_V4_Hair4TallArcher", "C11_Casual1",
          "Hair 4, shipped eye colour 2 with a cooler grade, surprised expression, sky outfit, bow and quiver. Tallest Casual variant.",
          1.07, 204, 198, 200, 0, expr="surprise", gear=("bow", "quiver"), hair_n=4, eye_n=2),
        V("Casual1_V5_Hair5MaskedGuard", "C11_Casual1",
          "Hair 5, shipped eye colour 3, cheerful eyes, plum outfit, half mask and shield.",
          0.98, 292, 286, 280, 2, expr="fun", gear=("mask", "shield"), hair_n=5, eye_n=3),
        V("Casual1_V6_Hair4MakeupChampion", "C11_Casual1",
          "Hair 4 again, but with the shipped makeup face, eye colour 4, fangs, sand-and-gold outfit and a greatsword. Reads apart from the tall archer.",
          1.03, 36, 40, 30, 5, expr="angry+fangs", gear=("greatsword",), hair_n=4, eye_n=4, makeup=True),
        V("Akio_V1_CrimsonFencer", "C01_Akio",
          "One-piece school body, so parts cannot be toggled. Crimson bottoms, auburn hair scaled up, blue eyes, angry face, sword. Slightly shorter.",
          0.97, 2, 16, 212, 0, expr="angry", gear=("sword",), scales=(("Hair", 1.22),),
          notes="Single mesh. Distinction is palette, hair-bone scale, height, face preset and weapon."),
        V("Akio_V2_NightScholar", "C01_Akio",
          "Violet uniform, silver-lilac hair worn smaller, red eyes, sorrowful face, mage hat and dagger.",
          1.0, 268, 270, 348, 2, expr="sad", gear=("hat", "dagger"), scales=(("Hair", 0.84),),
          hair_sat=0.28, hair_val=0.95, hair_mul=(0.78, 0.75, 0.9)),
        V("Akio_V3_GroveArcher", "C01_Akio",
          "Olive uniform, warm brown hair, green eyes, cheerful face, bow and quiver. Taller.",
          1.06, 92, 28, 130, 1, expr="fun", gear=("bow", "quiver")),
        V("Akio_V4_GoldCaptain", "C01_Akio",
          "White-and-gold uniform, blonde hair, amber eyes, bright smile, sword and shield. Tallest Akio variant.",
          1.08, 44, 46, 32, 4, expr="smile", gear=("sword", "shield"), scales=(("Hair", 1.1),)),
        V("Aika_V1_RoseIdol", "C02_Aika",
          "Rose uniform and rose hair, blue eyes, smile, dagger. Shortest Aika variant. Separate from Akio's crimson fencer by hue and weapon.",
          0.94, 338, 346, 205, 4, expr="smile", gear=("dagger",), scales=(("Hair", 1.18),)),
        V("Aika_V2_FrostMage", "C02_Aika",
          "Ice-blue uniform, pale silver hair, blue eyes, surprised face, mage hat.",
          1.03, 196, 205, 215, 0, expr="surprise", gear=("hat",),
          hair_sat=0.22, hair_val=1.05, hair_mul=(0.86, 0.9, 1.0)),
        V("Aika_V3_OliveArcher", "C02_Aika",
          "Moss uniform, dark-blonde hair, green eyes, cheerful face, bow and quiver.",
          1.0, 78, 42, 140, 1, expr="fun", gear=("bow", "quiver")),
        V("Aika_V4_WineGuard", "C02_Aika",
          "Wine uniform, black-red hair, red eyes, angry face, sword and shield. Tall.",
          1.06, 352, 358, 2, 3, expr="angry", gear=("sword", "shield"),
          hair_sat=0.55, hair_val=0.42, hair_mul=(0.45, 0.16, 0.2)),
        V("Akane_V1_PastelWitch", "C05_Akane",
          "Petite pastel witch. Lavender clothes, pink hair, violet eyes, cheerful face, mage hat.",
          0.96, 286, 322, 292, 4, expr="fun", gear=("hat",), scales=(("Hair", 1.2),)),
        V("Akane_V2_InkDuelist", "C05_Akane",
          "Black-and-red clothes, black hair, red eyes, angry face, sword.",
          1.0, 0, 350, 355, 2, expr="angry", gear=("sword",),
          cloth_val=0.55, hair_sat=0.4, hair_val=0.35, hair_mul=(0.35, 0.12, 0.14)),
        V("Akane_V3_SunnyArcher", "C05_Akane",
          "Marigold clothes, honey hair, amber eyes, smile, bow and quiver. Tallest Akane variant.",
          1.08, 38, 42, 34, 0, expr="smile", gear=("bow", "quiver")),
        V("Akane_V4_MossSquire", "C05_Akane",
          "Moss clothes, brown hair, green eyes, sad face, shield.",
          1.02, 108, 26, 135, 1, expr="sad", gear=("shield",)),
        V("Cultist_V1_WhiteCantor", "C06_Cultist",
          "The robe is one mesh, so the silhouette stays. This one is a pale gold-white robe, honey hair, blue eyes, smile, half mask.",
          1.0, 46, 40, 210, 0, expr="smile", gear=("mask",),
          cloth_sat=0.35, cloth_val=1.25, cloth_mul=(1.15, 1.08, 0.92)),
        V("Cultist_V2_CrimsonRite", "C06_Cultist",
          "Crimson robe, black-red hair, red eyes, angry face, dagger. Slightly shorter.",
          0.96, 2, 358, 350, 1, expr="angry", gear=("dagger",),
          hair_sat=0.5, hair_val=0.4, hair_mul=(0.4, 0.14, 0.16)),
        V("Cultist_V3_VerdantOracle", "C06_Cultist",
          "Green robe, teal hair, green eyes, cheerful face, mage hat. Taller.",
          1.05, 136, 164, 145, 4, expr="fun", gear=("hat",), scales=(("Hair", 1.12),)),
        V("Cultist_V4_NightBow", "C06_Cultist",
          "Deep blue robe, silver-blue hair, blue eyes, sad face, bow and quiver.",
          1.0, 228, 220, 210, 2, expr="sad", gear=("bow", "quiver"),
          hair_sat=0.3, hair_val=0.9, hair_mul=(0.7, 0.78, 0.95)),
        V("StellarWitch_V1_UnrobedDuelist", "C07_StellarWitch",
          "Hat, robe and scarf are off, leaving blouse and drawers. Red hair, angry face has no blend shapes on this rig, sword.",
          1.02, 6, 12, 208, 0, disable=("hat", "robe", "scart"), expr="angry", gear=("sword",),
          notes="StellarWitch ships no blend shapes, so the expression field is stored but does not move the face."),
        V("StellarWitch_V2_NightOracle", "C07_StellarWitch",
          "Full robe and hat, violet cloth, lilac hair, half mask. Hair bones slightly larger.",
          0.96, 274, 292, 280, 2, expr="sad", gear=("mask",), scales=(("Hair", 1.12), ("hat", 1.15))),
        V("StellarWitch_V3_ScarflessArcher", "C07_StellarWitch",
          "Scarf off, hat kept, teal robe, teal hair, bow and quiver.",
          1.0, 168, 176, 160, 4, disable=("scart",), expr="fun", gear=("bow", "quiver"), scales=(("hat", 1.22),)),
        V("StellarWitch_V4_TaillessKnight", "C07_StellarWitch",
          "Back tail and earring off. Gold-white robe, blonde hair, sword and shield. Tallest witch variant.",
          1.07, 44, 42, 30, 1, disable=("backtail", "earling"), expr="smile", gear=("greatsword", "shield")),
        V("SanpakuChan_V1_PinkBlade", "C08_SanpakuChan",
          "Pink clothes and hair, blue eyes, sword. Hair bones enlarged. The tail is not a separate renderer, so it cannot be toggled.",
          0.98, 332, 340, 210, 0, expr="smile", gear=("sword",), scales=(("Hair", 1.2), ("tail", 1.15)),
          notes="No blend shapes on this model. Tail mesh is part of the body."),
        V("SanpakuChan_V2_BlueMage", "C08_SanpakuChan",
          "Blue clothes, pale blue hair, mage hat.",
          1.0, 214, 200, 220, 4, expr="fun", gear=("hat",),
          hair_sat=0.55, hair_mul=(0.75, 0.85, 1.05)),
        V("SanpakuChan_V3_InkMask", "C08_SanpakuChan",
          "Black-red clothes, dark hair, red eyes, half mask and dagger. Slightly taller.",
          1.04, 358, 350, 348, 2, expr="angry", gear=("mask", "dagger"),
          cloth_val=0.6, hair_sat=0.4, hair_val=0.4, hair_mul=(0.3, 0.12, 0.14)),
        V("SanpakuChan_V4_SeaArcher", "C08_SanpakuChan",
          "Sea-green clothes and hair, bow and quiver. Shortest Sanpaku variant.",
          0.94, 174, 162, 155, 1, expr="sad", gear=("bow", "quiver")),
        V("Reika_V1_HatlessSnow", "C10_Reika",
          "Holiday hat off. Pale ice dress, silver-blue hair, blue eyes, open mouth, sword. Shorter. This rig has no smile shape.",
          0.95, 205, 210, 208, 0, disable=("Hat_mesh.001",), expr="ahh", gear=("sword",),
          cloth_sat=0.45, cloth_val=1.2, cloth_mul=(0.9, 0.95, 1.08),
          hair_sat=0.25, hair_val=1.05, hair_mul=(0.82, 0.88, 1.0)),
        V("Reika_V2_CrimsonCarol", "C10_Reika",
          "Hat stays. Crimson dress, dark red hair, red eyes, raised brows, dagger.",
          1.0, 356, 358, 350, 1, expr="brow", gear=("dagger",),
          hair_sat=0.7, hair_val=0.5, hair_mul=(0.55, 0.16, 0.18)),
        V("Reika_V3_HollyArcher", "C10_Reika",
          "Hat off. Holly-green dress, brown-green hair, green eyes, a wider open mouth, bow and quiver. Tallest Reika variant.",
          1.06, 128, 48, 140, 4, disable=("Hat_mesh.001",), expr="bigahh", gear=("bow", "quiver")),
        V("Reika_V4_MidnightBell", "C10_Reika",
          "Hat stays. Midnight violet dress, violet hair, violet eyes, sad face, half mask.",
          1.02, 272, 286, 280, 2, expr="sad", gear=("mask",)),
        V("Fina_V1_CoralSprinter", "C12_Fina",
          "Back hair off for a shorter silhouette. Coral top, warm hair, blue eyes, smile, sword.",
          1.0, 12, 22, 210, 0, disable=("HairBack",), expr="smile", gear=("sword",),
          notes="Hiding HairBack assumes the front and mid hair still cover the scalp. Confirm in Unity."),
        V("Fina_V2_BobDuelist", "C12_Fina",
          "Back and mid hair off, leaving the front hair. Blue clothes, cool hair, angry face, dagger. Shorter.",
          0.96, 216, 206, 215, 4, disable=("HairBack", "HairMid"), expr="angry", gear=("dagger",),
          notes="A bob only if HairFront covers the crown. This is unverified without a render."),
        V("Fina_V3_FullHairArcher", "C12_Fina",
          "All three hair pieces stay. Green street clothes, green-brown hair, bow and quiver. Taller.",
          1.05, 118, 70, 140, 1, expr="fun", gear=("bow", "quiver"), scales=(("Hair", 1.08),)),
        V("Fina_V4_SleevelessKnight", "C12_Fina",
          "The top mesh named topic is off. Gold accents, sword-length greatsword and shield, angry face.",
          1.03, 42, 36, 30, 2, disable=("topic",), expr="angry", gear=("greatsword", "shield"),
          notes="topic is the only top-like renderer. If it is not the outer shirt, the torso underneath still shows."),
        V("SapphiArtchan_V1_ClassRibbon", "C13_SapphiArtchan",
          "Chequer collar and back-skirt tiles off. Rose uniform, warm hair, blue eyes, smile, sword. Rig is Generic, so humanoid retargets will not play.",
          1.0, 344, 24, 210, 0, disable=("Colar_Tile", "BackSkirt_Tile"), expr="smile", gear=("sword",),
          notes="Generic rig. Variants change looks only; they do not add a humanoid avatar."),
        V("SapphiArtchan_V2_MechKnight", "C13_SapphiArtchan",
          "Skirts off so the mech legs and boots carry the silhouette. Blue-and-gold, sword and shield. Tallest.",
          1.06, 214, 40, 205, 1, disable=("Skirt", "Skirt_Inside", "BackSkirt_Inside", "BackSkirt_Tile"),
          expr="angry", gear=("sword", "shield")),
        V("SapphiArtchan_V3_SkirtMage", "C13_SapphiArtchan",
          "Mech covers and outer mech shells off, skirts stay. Orchid clothes, pink hair, mage hat. Shorter.",
          0.97, 304, 328, 300, 4, disable=("Mech_Cover_L", "Mech_Cover_R", "Mech_Outer_L", "Mech_Outer_R"),
          expr="browup", gear=("hat",), scales=(("Hair", 1.12),)),
        V("SapphiArtchan_V4_LiteArcher", "C13_SapphiArtchan",
          "Tile pieces off. Sea-green clothes, bow and quiver. Shortest Sapphi variant.",
          0.94, 162, 150, 155, 2, disable=("Colar_Tile", "BackSkirt_Tile"), expr="sad", gear=("bow", "quiver")),
        V("Muryotaisu_V1_HaloKnight", "C14_Muryotaisu",
          "Coat and halo stay. The halo bone is scaled up. Gold-white coat, blonde hair, amber eyes, smile, sword.",
          1.0, 44, 42, 32, 0, expr="smile", gear=("sword",), scales=(("Angel", 1.38), ("Ring", 1.38))),
        V("Muryotaisu_V2_CoatlessDuelist", "C14_Muryotaisu",
          "Coat off. The body renderer remains. Black-red hair, red eyes, troubled face, dagger. Shorter. This rig has no angry shape.",
          0.95, 4, 8, 350, 1, disable=("Coat",), expr="trouble", gear=("dagger",),
          cloth_val=0.7, hair_sat=0.55, hair_val=0.42, hair_mul=(0.4, 0.14, 0.16),
          notes="Coat off relies on the separate Body mesh. Confirm the torso reads as dressed."),
        V("Muryotaisu_V3_RinglessMage", "C14_Muryotaisu",
          "Halo off. Violet coat and hair, mage hat, mouth open. Tallest Muryotaisu variant.",
          1.05, 276, 292, 280, 4, disable=("AngelRing",), expr="surprise", gear=("hat",)),
        V("Muryotaisu_V4_GroveArcher", "C14_Muryotaisu",
          "Green coat, green-brown hair, green eyes, eyes closed, bow and quiver. Halo stays at normal size. This rig has no sad shape.",
          1.02, 130, 78, 142, 2, expr="eyeclose", gear=("bow", "quiver")),
    ]


def build_guid_map():
    mapping = {}
    for meta in ASSETS.rglob("*.meta"):
        try:
            head = meta.read_text(errors="ignore")[:180]
        except OSError:
            continue
        m = re.search(r"^guid: ([0-9a-f]+)", head, re.M)
        if m:
            mapping[m.group(1)] = str(meta)[:-5]
    return mapping


def parse_prefab(path: Path):
    text = path.read_text(errors="ignore")
    objects = {}
    for chunk in re.split(r"\n--- !u!", text):
        m = re.match(r"(\d+) &(-?\d+)", chunk)
        if not m:
            continue
        objects[m.group(2)] = (int(m.group(1)), chunk)
    gos, xforms, renderers = {}, {}, []
    for oid, (typ, chunk) in objects.items():
        if typ == 1:
            name = re.search(r"\n  m_Name: (.*)", chunk)
            comps = re.findall(r"component: \{fileID: (-?\d+)\}", chunk)
            gos[oid] = {"name": name.group(1).strip() if name else "", "comps": comps}
        elif typ == 4:
            go = re.search(r"m_GameObject: \{fileID: (-?\d+)\}", chunk)
            father = re.search(r"m_Father: \{fileID: (-?\d+)\}", chunk)
            scale = re.search(r"m_LocalScale: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}", chunk)
            xforms[oid] = {
                "go": go.group(1) if go else None,
                "father": father.group(1) if father else "0",
                "scale": tuple(float(x) for x in scale.groups()) if scale else (1, 1, 1),
            }
    comp_owner = {}
    for gid, go in gos.items():
        for c in go["comps"]:
            comp_owner[c] = gid
    renderer_gos = set()
    for oid, (typ, chunk) in objects.items():
        if typ not in (23, 137):
            continue
        go = re.search(r"m_GameObject: \{fileID: (-?\d+)\}", chunk)
        gid = go.group(1) if go else None
        renderer_gos.add(gid)
        mats = []
        msec = re.search(r"m_Materials:\n((?:  - .*\n)+)", chunk)
        if msec:
            for line in msec.group(1).splitlines():
                mm = re.search(r"guid: ([0-9a-f]+)", line)
                if mm:
                    mats.append(mm.group(1))
        mesh = re.search(r"m_Mesh: \{fileID: (-?\d+), guid: ([0-9a-f]+)", chunk)
        renderers.append({
            "id": oid, "go": gid, "name": gos.get(gid, {}).get("name", ""),
            "mats": mats, "mesh": mesh.groups() if mesh else None,
        })
    root_tf = None
    root_go = None
    pref_name = path.stem
    candidates = []
    for tid, tf in xforms.items():
        if tf["father"] in ("0", None) and tf["go"] in gos:
            candidates.append((tid, tf["go"], gos[tf["go"]]["name"]))
    for tid, gid, name in candidates:
        if name == pref_name or root_tf is None:
            root_tf, root_go = tid, gid
            if name == pref_name:
                break
    return {
        "gos": gos, "xforms": xforms, "renderers": renderers,
        "renderer_gos": renderer_gos, "root_tf": root_tf, "root_go": root_go,
        "root_scale": xforms[root_tf]["scale"], "root_name": gos[root_go]["name"],
    }


def read_guid(asset_path: str) -> str:
    text = (ROOT / (asset_path + ".meta")).read_text(errors="ignore")
    return re.search(r"^guid: ([0-9a-f]+)", text, re.M).group(1)


def parse_mat(path: str):
    text = Path(path).read_text(errors="ignore")
    name = re.search(r"\n  m_Name: (.*)", text)
    base = re.search(r"_BaseMap:\n\s+m_Texture: \{fileID: (-?\d+), guid: ([0-9a-f]+)", text)
    return {
        "name": name.group(1).strip() if name else Path(path).stem,
        "text": text,
        "base_guid": base.group(2) if base else None,
    }


def bucket_for(pref, rname, matname):
    mn = matname.lower()
    rn = (rname or "").lower()
    if any(k in mn for k in ("highlight", "eyewhite", "eye_white", "eyelash", "eyeline", "eyeliner", "reflection", "cheek", "mouth", "eyebrow")):
        return "keep"
    if "brow" in mn and "hair" not in mn:
        return "keep"
    if "reflect" in mn:
        return "keep"
    if pref == "C04_UnityChan" and rname in UC_BUCKET:
        forced = UC_BUCKET[rname]
        if forced == "keep":
            return "keep"
        if "eye" in mn and forced not in ("eye", "hair"):
            return "eye"
        return forced
    if pref == "C09_PicoChan":
        if "eye" in mn and "face" not in rn:
            return "eye"
        if rn.startswith("hair"):
            return "hair"
        if rn.startswith("jacket"):
            return "jacket"
        if rn.startswith("goggle"):
            return "goggle"
        if rn.startswith("boot"):
            return "boots"
        if rn.startswith("picolabo"):
            return "bag"
        if "pants" in rn:
            return "legs"
        if rn.startswith("clothe"):
            return "shirt"
        if rn.startswith("body"):
            return "skin"
        if rn.startswith("face"):
            return "eye" if "eye" in mn and "face" not in mn else "face"
    if pref == "C07_StellarWitch":
        if rn == "pupil" or "pupil" in mn:
            return "eye"
        if "hair" in rn or rn in ("backtail", "fronthair", "earling", "hairaccessory"):
            return "hair"
        if rn == "hat":
            return "hat"
        if rn == "robe":
            return "jacket"
        if rn == "scart":
            return "ribbon"
        if rn in ("blouse", "drawers", "tights"):
            return "shirt"
        if rn == "shoes":
            return "boots"
        if rn in ("face", "hand"):
            return "skin"
    if pref == "C12_Fina":
        if rn.startswith("hair"):
            return "hair"
        if rn == "pants":
            return "legs"
        if rn in ("topic", "torso"):
            return "shirt"
        if rn == "sneakers":
            return "boots"
        if rn == "face":
            return "eye" if "eye" in mn else "face"
        if rn in ("body", "arms", "legs", "hands", "feets"):
            return "skin"
    if pref == "C10_Reika":
        if "hat" in rn:
            return "hat"
        if "dress" in rn:
            return "jacket"
        if "hair" in rn:
            return "hair"
        if "face" in rn:
            return "eye" if mn.strip() == "eye" or mn.startswith("eye") else "face"
    if pref == "C14_Muryotaisu":
        if "angel" in rn:
            return "trim"
        if rn == "coat":
            return "jacket"
        if rn == "hair":
            return "hair"
        if rn == "eye":
            return "eye"
        if "lash" in rn or "light" in rn:
            return "keep"
        if rn == "face":
            return "face"
        if rn == "body":
            return "skin"
    if pref == "C13_SapphiArtchan":
        if "hair" in rn:
            return "hair"
        if rn.startswith("mech"):
            return "boots"
        if "skirt" in rn:
            return "skirt"
        if rn in ("tie", "belt", "button", "colar_tile", "legacc"):
            return "trim"
        if rn == "eyes" or "eye" in mn:
            return "eye"
        if rn in ("brow",) or "teeth" in rn or rn == "tongue":
            return "keep"
        if rn in ("face", "faceflat"):
            return "face"
        if rn in ("legs", "arms", "tummy", "boots", "cloth", "clothouter"):
            return "shirt"
    if "iris" in mn or "pupil" in mn or (re.search(r"\beye\b", mn) and "brow" not in mn):
        return "eye"
    if "shoe" in mn:
        return "boots"
    if "hair" in mn or "hair" in rn:
        return "hair"
    if "skin" in mn:
        return "face" if "face" in mn else "skin"
    if "face" in mn and "cloth" not in mn:
        return "face"
    if "cloth" in mn or "top" in mn or "dress" in mn or "bottom" in mn:
        return "cloth"
    return "cloth"


def palette_of(v):
    cloth = v["cloth"]
    hair = v["hair"]
    eye = v["eye"]
    skin = {"mode": "skin", "tone": SKINS[v["skin"] % len(SKINS)], "blend": 0.8}
    face = {"mode": "skin", "tone": SKINS[v["skin"] % len(SKINS)], "blend": 0.38}
    def hue(h, sat, val, mul):
        return {"mode": "hue", "hue": h % 360, "sat": sat, "val": val, "mul": mul}
    c = hue(cloth, v["cloth_sat"], v["cloth_val"], v["cloth_mul"])
    return {
        "cloth": c,
        "jacket": hue(cloth + 6, v["cloth_sat"], v["cloth_val"], v["cloth_mul"]),
        "shirt": hue(cloth + 28, v["cloth_sat"], v["cloth_val"], v["cloth_mul"]),
        "skirt": hue(cloth - 16, v["cloth_sat"], v["cloth_val"], v["cloth_mul"]),
        "legs": hue(cloth + 40, v["cloth_sat"] * 0.9, v["cloth_val"], v["cloth_mul"]),
        "boots": hue(cloth + 18, v["cloth_sat"], v["cloth_val"] * 0.85, (0.7, 0.7, 0.75)),
        "ribbon": hue(hair, 1.3, 1.0, (1, 1, 1)),
        "trim": hue(cloth + 50, 1.2, 1.05, (1.1, 0.95, 0.7)),
        "hat": hue(cloth + 12, v["cloth_sat"], v["cloth_val"], v["cloth_mul"]),
        "goggle": hue(200, 0.6, 0.9, (0.6, 0.75, 0.9)),
        "bag": hue(cloth - 24, v["cloth_sat"], v["cloth_val"], v["cloth_mul"]),
        "hair": hue(hair, v["hair_sat"], v["hair_val"], v["hair_mul"]),
        "eye": hue(eye, 1.55, 1.05, (1, 1, 1)),
        "skin": skin,
        "face": face,
    }


MASK_CHANNEL = {
    "hair": "hair", "skin": "skin", "face": "skin", "eye": "primary",
    "jacket": "primary", "shirt": "secondary", "skirt": "primary", "legs": "secondary",
    "boots": "secondary", "ribbon": "secondary", "trim": "primary", "hat": "primary",
    "goggle": "primary", "bag": "secondary", "cloth": "primary",
}


def is_safe_bone(name: str) -> bool:
    n = name.lower()
    if n in ("head", "character1_head", "armature", "root"):
        return False
    if any(b in n for b in ("finger", "wrist", "hand", "arm", "leg", "spine", "chest", "neck", "hips", "thigh", "foot", "toe", "shoulder", "pelvis", "forearm", "shin")):
        return False
    return any(g in n for g in ("hair", "ribbon", "hood", "goggle", "picolabo", "bag", "angel", "hat", "bang", "braid", "ponytail", "earling", "skirt", "tail"))


def rgb_to_hsv(rgb):
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    maxc = np.maximum(np.maximum(r, g), b)
    minc = np.minimum(np.minimum(r, g), b)
    d = maxc - minc
    s = np.where(maxc > 1e-8, d / np.maximum(maxc, 1e-8), 0.0)
    is_r = (maxc == r) & (d > 1e-8)
    is_g = (maxc == g) & (d > 1e-8) & ~is_r
    is_b = (d > 1e-8) & ~is_r & ~is_g
    rc = np.where(d > 1e-8, (maxc - r) / np.maximum(d, 1e-8), 0)
    gc = np.where(d > 1e-8, (maxc - g) / np.maximum(d, 1e-8), 0)
    bc = np.where(d > 1e-8, (maxc - b) / np.maximum(d, 1e-8), 0)
    h = np.zeros_like(maxc)
    h = np.where(is_r, np.mod(bc - gc, 6), h)
    h = np.where(is_g, 2 + rc - bc, h)
    h = np.where(is_b, 4 + gc - rc, h)
    return (h / 6.0) % 1.0, s, maxc


def hsv_to_rgb(h, s, v):
    h6 = h * 6.0
    i = np.mod(np.floor(h6).astype(np.int32), 6)
    f = h6 - np.floor(h6)
    p = v * (1 - s)
    q = v * (1 - f * s)
    t = v * (1 - (1 - f) * s)
    out = np.zeros(h.shape + (3,), np.float32)
    cols = ((v, t, p), (q, v, p), (p, v, t), (p, q, v), (t, p, v), (v, p, q))
    for idx, (c0, c1, c2) in enumerate(cols):
        m = i == idx
        out[..., 0] = np.where(m, c0, out[..., 0])
        out[..., 1] = np.where(m, c1, out[..., 1])
        out[..., 2] = np.where(m, c2, out[..., 2])
    return out


def recolor(im: Image.Image, style: dict) -> Image.Image:
    arr = np.asarray(im).astype(np.float32) / 255.0
    rgb = arr[..., :3]
    alpha = arr[..., 3]
    if style["mode"] == "skin":
        tone = np.array(style["tone"], np.float32)
        lum = rgb[..., 0] * 0.299 + rgb[..., 1] * 0.587 + rgb[..., 2] * 0.114
        tl = float(np.dot(tone, np.array([0.299, 0.587, 0.114])))
        target = lum[..., None] * (tone / max(tl, 1e-4))
        blend = float(style["blend"])
        rgb = np.clip(rgb * (1 - blend) + np.clip(target, 0, 1) * blend, 0, 1)
    else:
        h, s, v = rgb_to_hsv(rgb)
        colored = s > 0.05
        h = np.where(colored, (style["hue"] % 360) / 360.0, h)
        s = np.clip(np.where(colored, s * style["sat"], s), 0, 1)
        v = np.clip(v * style["val"], 0, 1)
        rgb = np.clip(hsv_to_rgb(h, s, v) * np.array(style["mul"], np.float32), 0, 1)
    out = np.clip(np.concatenate([rgb, alpha[..., None]], axis=-1) * 255.0, 0, 255).astype(np.uint8)
    return Image.fromarray(out, "RGBA")


def load_rgba(path: Path, max_size: int) -> Image.Image:
    im = Image.open(path).convert("RGBA")
    w, h = im.size
    scale = min(1.0, max_size / max(w, h))
    if scale < 1:
        im = im.resize((max(1, int(w * scale)), max(1, int(h * scale))), Image.Resampling.LANCZOS)
    return im


def texture_meta(guid: str, alpha: bool, size: int) -> str:
    return f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  fileIDToRecycleName: {{}}
  externalObjects: {{}}
  serializedVersion: 4
  mipmaps:
    mipMapMode: 0
    enableMipMap: 1
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
  isReadable: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: {size}
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 0
    wrapV: 0
    wrapW: 0
  nPOTScale: 1
  lightmap: 0
  compressionQuality: 50
  spriteMode: 0
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: {1 if alpha else 0}
  alphaIsTransparency: {1 if alpha else 0}
  spriteTessellationDetail: -1
  textureType: 0
  textureShape: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  platformSettings:
  - buildTarget: DefaultTexturePlatform
    maxTextureSize: {size}
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    androidETC2FallbackOverride: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
  spritePackingTag: 
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def folder_meta(guid: str) -> str:
    return f"""fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def text_meta(guid: str) -> str:
    return f"""fileFormatVersion: 2
guid: {guid}
TextScriptImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def prefab_meta(guid: str) -> str:
    return f"""fileFormatVersion: 2
guid: {guid}
PrefabImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def mat_meta(guid: str) -> str:
    return f"""fileFormatVersion: 2
guid: {guid}
NativeFormatImporter:
  externalObjects: {{}}
  mainObjectFileID: 2100000
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def ensure_dirs():
    for rel in ("", "Materials", "Textures", "Prefabs", "Previews"):
        p = OUT / rel
        p.mkdir(parents=True, exist_ok=True)
        meta = Path(str(p) + ".meta")
        if not meta.exists():
            meta.write_text(folder_meta(guid_for("folder:" + str(p.relative_to(ROOT)))))
    char_dir = ASSETS / "Scripts" / "Characters"
    char_dir.mkdir(parents=True, exist_ok=True)
    meta = Path(str(char_dir) + ".meta")
    if not meta.exists():
        meta.write_text(folder_meta(guid_for("folder:Assets/Scripts/Characters")))


def shape_names(data: bytes, suffix: str):
    needle = suffix.encode("ascii")
    found = []
    seen = set()
    start = 0
    while True:
        i = data.find(needle, start)
        if i < 0:
            break
        j = i
        k = i + len(needle)
        while j > 0 and 32 < data[j - 1] < 127:
            j -= 1
        while k < len(data) and 32 < data[k] < 127:
            k += 1
        name = data[j:k].decode("ascii", "ignore")
        start = i + 1
        if not name or name in seen or len(name) > 96:
            continue
        exact = name == suffix or name.endswith("." + suffix) or name.endswith("_" + suffix) or name.endswith(" " + suffix)
        if not exact:
            continue
        seen.add(name)
        found.append(name)
        if suffix not in seen and suffix != name:
            seen.add(suffix)
            found.append(suffix)
        if len(found) >= 4:
            break
    return found


def pick_shapes(fbx_bytes: bytes, expr: str):
    found = []
    seen = set()
    for part in expr.split("+"):
        got = 0
        weight = 100.0 if part == "fangs" else 75.0
        for suffix in PROFILES.get(part, []):
            for name in shape_names(fbx_bytes, suffix):
                if name in seen:
                    continue
                seen.add(name)
                found.append({"shapeName": name, "weight": weight})
                got += 1
                if got >= 2:
                    break
            if got >= 2:
                break
    return found


def set_color(text, key, rgb):
    r, g, b = rgb
    repl = f"{key}: {{r: {r:.4f}, g: {g:.4f}, b: {b:.4f}, a: 1}}"
    if re.search(rf"{re.escape(key)}: \{{", text):
        return re.sub(rf"{re.escape(key)}: \{{[^}}]+\}}", repl, text, count=1)
    return text.replace("  m_BuildTextureStacks:", f"    - {repl}\n  m_BuildTextureStacks:", 1)


def grade_for(style):
    if style["mode"] == "skin":
        t = style["tone"]
        return tuple(0.55 + 0.45 * c for c in t)
    # mild multiply so the mask channel is visible without wiping the texture
    mul = style["mul"]
    return tuple(min(1.35, 0.72 + 0.28 * c) for c in mul)


def shadow_for(style):
    if style["mode"] == "skin":
        t = style["tone"]
        return tuple(max(0.15, c * 0.55) for c in t)
    h = (style["hue"] % 360) / 360.0
    rgb = hsv_to_rgb(np.array(h), np.array(0.35), np.array(0.45))
    return tuple(float(x) for x in rgb)


def clone_material(src_text, new_name, tex_guid, mask_guid, style, mask_name):
    text = src_text
    head, sep, tail = text.partition("--- !u!21")
    if sep:
        tail = re.sub(r"\n  m_Name: .*", f"\n  m_Name: {new_name}", tail, count=1)
        text = head + sep + tail
    else:
        text = re.sub(r"\n  m_Name: .*", f"\n  m_Name: {new_name}", text, count=1)
    text = re.sub(
        r"m_Shader: \{fileID: -?\d+, guid: [0-9a-f]+, type: 3\}",
        f"m_Shader: {{fileID: 4800000, guid: {TOON}, type: 3}}",
        text,
        count=1,
    )
    base = re.search(r"_BaseMap:\n\s+m_Texture: \{fileID: (-?\d+), guid: ([0-9a-f]+)", text)
    if base:
        text = text.replace(base.group(2), tex_guid)
    else:
        block = f"{{fileID: 2800000, guid: {tex_guid}, type: 3}}"
        text = text.replace("_BaseMap:\n        m_Texture: {fileID: 0}", f"_BaseMap:\n        m_Texture: {block}", 1)
        text = text.replace("_MainTex:\n        m_Texture: {fileID: 0}", f"_MainTex:\n        m_Texture: {block}", 1)
    text = re.sub(
        r"(_TintMask:\n\s+m_Texture:) \{fileID: [^}]+\}",
        rf"\1 {{fileID: 2800000, guid: {mask_guid}, type: 3}}",
        text,
        count=1,
    )
    if "_UniformDesat:" in text:
        text = re.sub(r"_UniformDesat: [-+0-9.]+", "_UniformDesat: 0", text)
    else:
        text = text.replace("m_Floats:\n", "m_Floats:\n    - _UniformDesat: 0\n", 1)
    white = (1, 1, 1)
    text = set_color(text, "_BaseColor", white)
    text = set_color(text, "_UniformTint", white)
    text = set_color(text, "_AccentTint", white)
    grade = grade_for(style)
    text = set_color(text, "_SkinTint", grade if mask_name == "skin" else white)
    text = set_color(text, "_HairTint", grade if mask_name == "hair" else white)
    text = set_color(text, "_PrimaryTint", grade if mask_name == "primary" else white)
    text = set_color(text, "_SecondaryTint", grade if mask_name == "secondary" else white)
    text = set_color(text, "_ShadowColor", shadow_for(style))
    return text


def yaml_quote(value: str) -> str:
    return '"' + value.replace("\\", "\\\\").replace('"', '\\"') + '"'


def write_variant_prefab(v, base_guid, parsed, mat_assigns, expressions, accessories, mesh_swaps):
    mods = []
    def add(target, prop, value=None, obj=None):
        if obj is None:
            mods.append(
                f"    - target: {{fileID: {target}, guid: {base_guid}, type: 3}}\n"
                f"      propertyPath: {prop}\n"
                f"      value: {value}\n"
                f"      objectReference: {{fileID: 0}}"
            )
        else:
            mods.append(
                f"    - target: {{fileID: {target}, guid: {base_guid}, type: 3}}\n"
                f"      propertyPath: {prop}\n"
                f"      value: \n"
                f"      objectReference: {obj}"
            )
    add(parsed["root_go"], "m_Name", yaml_quote(v["name"]))
    sx, sy, sz = parsed["root_scale"]
    scale = v["root"]
    add(parsed["root_tf"], "m_LocalScale.x", f"{sx * scale:.5f}")
    add(parsed["root_tf"], "m_LocalScale.y", f"{sy * scale:.5f}")
    add(parsed["root_tf"], "m_LocalScale.z", f"{sz * scale:.5f}")
    disabled = []
    for part in v["disable"]:
        found = False
        for gid, go in parsed["gos"].items():
            if go["name"] == part:
                add(gid, "m_IsActive", "0")
                disabled.append(part)
                found = True
                break
        if not found:
            raise SystemExit(f"{v['name']} missing part {part}")
    scaled = []
    for contains, factor in v["scales"]:
        if abs(factor - 1) < 1e-3:
            continue
        for tid, tf in parsed["xforms"].items():
            go = parsed["gos"].get(tf["go"])
            if not go:
                continue
            if tf["go"] in parsed["renderer_gos"]:
                continue
            if tid == parsed["root_tf"]:
                continue
            if contains.lower() not in go["name"].lower():
                continue
            if not is_safe_bone(go["name"]):
                continue
            x, y, z = tf["scale"]
            add(tid, "m_LocalScale.x", f"{x * factor:.5f}")
            add(tid, "m_LocalScale.y", f"{y * factor:.5f}")
            add(tid, "m_LocalScale.z", f"{z * factor:.5f}")
            scaled.append(go["name"])
    for assign in mat_assigns:
        add(assign["renderer_id"], f"m_Materials.Array.data[{assign['slot']}]", obj=f"{{fileID: 2100000, guid: {assign['mat_guid']}, type: 2}}")
    for swap in mesh_swaps:
        add(swap["renderer_id"], "m_Mesh", obj=f"{{fileID: {swap['fileID']}, guid: {swap['guid']}, type: 3}}")
    inst = fid_for(v["name"] + ":instance")
    stripped = fid_for(v["name"] + ":go")
    comp = fid_for(v["name"] + ":look")
    expr_yaml = "  expressions: []\n" if not expressions else "  expressions:\n" + "".join(
        f"  - shapeName: {e['shapeName']}\n    weight: {e['weight']}\n" for e in expressions
    )
    if accessories:
        acc_yaml = "  accessories:\n"
        for a in accessories:
            acc_yaml += (
                f"  - prefab: {{fileID: 100100000, guid: {a['guid']}, type: 3}}\n"
                f"    boneSlot: {a['boneSlot']}\n"
                f"    localPosition: {{x: {a['px']}, y: {a['py']}, z: {a['pz']}}}\n"
                f"    localEulerAngles: {{x: {a['ex']}, y: {a['ey']}, z: {a['ez']}}}\n"
                f"    localScale: {{x: {a['sx']}, y: {a['sy']}, z: {a['sz']}}}\n"
            )
    else:
        acc_yaml = "  accessories: []\n"
    body = f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1001 &{inst}
PrefabInstance:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Modification:
    serializedVersion: 3
    m_TransformParent: {{fileID: 0}}
    m_Modifications:
{chr(10).join(mods)}
    m_RemovedComponents: []
    m_RemovedGameObjects: []
    m_AddedGameObjects: []
    m_AddedComponents:
    - targetCorrespondingSourceObject: {{fileID: {parsed['root_go']}, guid: {base_guid}, type: 3}}
      insertIndex: -1
      addedObject: {{fileID: {comp}}}
  m_SourcePrefab: {{fileID: 100100000, guid: {base_guid}, type: 3}}
--- !u!1 &{stripped} stripped
GameObject:
  m_CorrespondingSourceObject: {{fileID: {parsed['root_go']}, guid: {base_guid}, type: 3}}
  m_PrefabInstance: {{fileID: {inst}}}
  m_PrefabAsset: {{fileID: 0}}
--- !u!114 &{comp}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {stripped}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {SCRIPT}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::DemonIdol.Characters.MDIVariantLook
  description: {yaml_quote(v['desc'])}
  variantId: {v['name']}
{expr_yaml}{acc_yaml}"""
    path = OUT / "Prefabs" / f"{v['name']}.prefab"
    path.write_text(body)
    Path(str(path) + ".meta").write_text(prefab_meta(guid_for("prefab:" + v["name"])))
    return disabled, scaled


def font(size, bold=False):
    path = "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf" if bold else "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"
    if Path(path).exists():
        return ImageFont.truetype(path, size)
    return ImageFont.load_default()


def wrap(draw, text, fnt, width):
    lines = []
    for para in text.split("\n"):
        words = para.split()
        cur = ""
        for w in words:
            trial = (cur + " " + w).strip()
            if draw.textlength(trial, font=fnt) <= width:
                cur = trial
            else:
                if cur:
                    lines.append(cur)
                cur = w
        lines.append(cur)
    return lines


def contact_sheet(v, thumbs, disabled, scaled, expressions, gear, notes):
    W, H = 1400, 980
    im = Image.new("RGB", (W, H), (28, 24, 36))
    d = ImageDraw.Draw(im)
    d.rectangle((0, 0, W, 78), fill=(64, 28, 78))
    d.text((24, 16), v["name"], font=font(28, True), fill=(255, 236, 250))
    d.text((24, 50), f"She is a female variant of {v['base']}   height x{v['root']}", font=font(16), fill=(230, 210, 230))
    if not thumbs:
        d.text((24, 100), "No new textures", font=font(18), fill=(220, 220, 220))
    for i, (label, img) in enumerate(thumbs[:8]):
        col, row = i % 4, i // 4
        x = 24 + col * 165
        y = 100 + row * 190
        thumb = img.copy()
        thumb.thumbnail((148, 148))
        im.paste(thumb, (x, y))
        d.rectangle((x, y, x + thumb.width, y + thumb.height), outline=(180, 140, 190))
        d.text((x, y + 152), label[:22], font=font(14), fill=(230, 220, 235))
    tx = 720
    fnt = font(16)
    blocks = [
        "Look",
        v["desc"],
        "",
        "Parts toggled off",
        ", ".join(disabled) if disabled else "None. This base has no separable piece for this look, or the pieces stay on.",
        "",
        "Resized non-humanoid bones",
        (", ".join(f"{a} x{b}" for a, b in v["scales"]) + f" ({len(scaled)} bones)") if v["scales"] else "None",
        "",
        "Expression preset",
        ", ".join(f"{e['shapeName']} {e['weight']:.0f}" for e in expressions) if expressions else "No matching blend shape on this mesh.",
        "",
        "Attachments (play mode / rebuild menu)",
        ", ".join(gear) if gear else "None",
        "",
        "Notes",
        notes or "Accessory rotations are a first guess until Unity is opened.",
    ]
    yy = 100
    for i, line in enumerate(blocks):
        use = font(18, True) if line in ("Look", "Parts toggled off", "Resized non-humanoid bones", "Expression preset", "Attachments (play mode / rebuild menu)", "Notes") else fnt
        color = (255, 214, 236) if use == font(18, True) else (230, 226, 235)
        for wrapped in wrap(d, line, use, 600):
            d.text((tx, yy), wrapped, font=use, fill=color)
            yy += 22
        if yy > H - 40:
            break
    out = OUT / "Previews" / f"{v['name']}.png"
    im.save(out, "PNG", optimize=True)
    Path(str(out) + ".meta").write_text(texture_meta(guid_for("preview:" + v["name"]), False, 1024))
    return out


def overview(rows):
    card_w, card_h = 360, 150
    cols = 4
    rows_n = (len(rows) + cols - 1) // cols
    im = Image.new("RGB", (cols * card_w + 40, rows_n * card_h + 80), (22, 18, 32))
    d = ImageDraw.Draw(im)
    d.text((20, 16), "My Demon Idol character variants", font=font(28, True), fill=(255, 230, 245))
    for i, row in enumerate(rows):
        c, r = i % cols, i // cols
        x, y = 20 + c * card_w, 64 + r * card_h
        d.rounded_rectangle((x, y, x + card_w - 16, y + card_h - 12), 10, fill=(48, 32, 58), outline=(180, 120, 170))
        d.text((x + 12, y + 10), row["name"].replace("_", "\n", 1), font=font(15, True), fill=(255, 236, 248))
        sw = row["swatch"]
        d.rectangle((x + 12, y + 78, x + 52, y + 118), fill=sw[0])
        d.rectangle((x + 58, y + 78, x + 98, y + 118), fill=sw[1])
        d.rectangle((x + 104, y + 78, x + 144, y + 118), fill=sw[2])
        d.text((x + 160, y + 86), f"x{row['root']}", font=font(18), fill=(230, 220, 235))
        d.text((x + 160, y + 110), row["gear"][:28], font=font(13), fill=(200, 190, 210))
    path = OUT / "Previews" / "Variants_Overview.png"
    im.save(path, "PNG", optimize=True)
    Path(str(path) + ".meta").write_text(texture_meta(guid_for("preview:overview"), False, 2048))
    return path


def swatch_from_style(style):
    if style["mode"] == "skin":
        rgb = tuple(int(c * 255) for c in style["tone"])
        return rgb
    h = (style["hue"] % 360) / 360.0
    rgb = hsv_to_rgb(np.array(h), np.array(min(1, style["sat"] * 0.6)), np.array(min(1, style["val"])))
    rgb = np.clip(rgb * np.array(style["mul"]), 0, 1)
    return tuple(int(float(c) * 255) for c in rgb)


def write_masks(cache):
    specs = {
        "skin": (255, 0, 0, 0),
        "hair": (0, 255, 0, 0),
        "primary": (0, 0, 255, 0),
        "secondary": (0, 0, 0, 255),
    }
    out = {}
    for name, color in specs.items():
        im = Image.new("RGBA", (64, 64), color)
        rel = f"Assets/Prefabs/Characters/Variants/Textures/TintMask_{name}.png"
        path = ROOT / rel
        im.save(path, "PNG", optimize=True)
        g = guid_for("tex:" + rel)
        Path(str(path) + ".meta").write_text(texture_meta(g, True, 64).replace("alphaIsTransparency: 1", "alphaIsTransparency: 0"))
        out[name] = g
        cache[name] = im
    return out


def main():
    print("Variant generation is retired. Assets/Prefabs/Characters/Variants is not recreated.")
    return
    ensure_dirs()
    guid_map = build_guid_map()
    all_v = variants()
    assert len(all_v) == 56, len(all_v)
    # eye colour textures from the shipped materials
    eye_src = {}
    for n, mat in ((2, "Assets/AnimeGirls/BaseCharacter/BaseCharacter/Sourcefiles/Mat/Mat_transp_Eye_color2.mat"),
                   (3, "Assets/AnimeGirls/BaseCharacter/BaseCharacter/Sourcefiles/Mat/Mat_transp_Eye_color3.mat"),
                   (4, "Assets/AnimeGirls/BaseCharacter/BaseCharacter/Sourcefiles/Mat/Mat_transp_Eye_color4.mat")):
        info = parse_mat(str(ROOT / mat))
        eye_src[n] = guid_map[info["base_guid"]]
    masks = write_masks({})
    tex_cache = {}
    mat_cache = {}
    fbx_cache = {}
    parsed_cache = {}
    recipe = []
    readme_rows = []
    overview_rows = []
    warnings = []

    def get_tex(src_path, style, max_size):
        sig = (src_path, str(sorted((k, str(val)) for k, val in style.items())), max_size)
        if sig in tex_cache:
            return tex_cache[sig]
        im = load_rgba(Path(src_path), max_size)
        painted = recolor(im, style)
        stem = Path(src_path).stem
        hue = style.get("hue", 0)
        rel = f"Assets/Prefabs/Characters/Variants/Textures/{stem}__{style['mode']}_{int(hue)}_{len(tex_cache):03d}.png"
        # stable name from guid
        rel = "Assets/Prefabs/Characters/Variants/Textures/tex_" + guid_for("img:" + str(sig))[:12] + ".png"
        path = ROOT / rel
        alpha = np.asarray(painted)[..., 3].min() < 250
        if not alpha:
            painted = painted.convert("RGB")
        painted.save(path, "PNG", optimize=True)
        g = guid_for("tex:" + rel)
        Path(str(path) + ".meta").write_text(texture_meta(g, alpha, max_size))
        tex_cache[sig] = (rel, g, painted.convert("RGBA"))
        return tex_cache[sig]

    for v in all_v:
        base_path = f"Assets/Prefabs/Characters/{v['base']}.prefab"
        if base_path not in parsed_cache:
            parsed_cache[base_path] = parse_prefab(ROOT / base_path)
        parsed = parsed_cache[base_path]
        base_guid = read_guid(base_path)
        pal = palette_of(v)
        fbx_bytes = b""
        fbx_path = None
        for r in parsed["renderers"]:
            if r["mesh"]:
                fbx_path = guid_map.get(r["mesh"][1])
                break
        if fbx_path:
            if fbx_path not in fbx_cache:
                fbx_cache[fbx_path] = Path(fbx_path).read_bytes()
            fbx_bytes = fbx_cache[fbx_path]
        expressions = pick_shapes(fbx_bytes, v["expr"]) if fbx_bytes else []
        mat_assigns = []
        thumbs = []
        seen_thumbs = set()
        for r in parsed["renderers"]:
            if r["name"] in v["disable"]:
                continue
            for slot, mat_guid in enumerate(r["mats"]):
                src_path = guid_map.get(mat_guid)
                if not src_path or not src_path.endswith((".mat", ".asset")):
                    warnings.append(f"{v['name']} unresolved mat {mat_guid}")
                    continue
                info = parse_mat(src_path)
                cat = bucket_for(v["base"], r["name"], info["name"])
                if cat == "keep" or cat not in pal:
                    continue
                style = pal[cat]
                source_img = None
                if v["base"] == "C04_UnityChan" and r["name"] == "skin":
                    source_img = str(ROOT / SKIN_UC)
                if v["base"] == "C11_Casual1" and v["hair_n"]:
                    n = v["hair_n"]
                    if r["name"] == "Hair" and slot == 0:
                        source_img = str(ROOT / f"Assets/AnimeGirls/BaseCharacter/BaseCharacter/Sourcefiles/Tex/Tex_Hair{n}_hair.png")
                    if r["name"] == "Casual1_Body" and slot == 9:
                        source_img = str(ROOT / f"Assets/AnimeGirls/BaseCharacter/BaseCharacter/Sourcefiles/Tex/Tex_Hair{n}_head.png")
                    if r["name"] == "Casual1_Body" and slot == 6 and v["eye_n"]:
                        source_img = eye_src[v["eye_n"]]
                    if r["name"] == "Casual1_Body" and slot == 7 and v["makeup"]:
                        source_img = str(ROOT / MAKEUP)
                        style = {"mode": "skin", "tone": SKINS[v["skin"] % 6], "blend": 0.22}
                if source_img is None:
                    if not info["base_guid"]:
                        continue
                    source_img = guid_map.get(info["base_guid"])
                if not source_img or not Path(source_img).exists():
                    warnings.append(f"{v['name']} missing texture for {r['name']} slot {slot}")
                    continue
                max_size = 192 if cat == "eye" else 256
                rel, tex_guid, painted = get_tex(source_img, style, max_size)
                mask_name = MASK_CHANNEL.get(cat, "primary")
                mat_key = (src_path, rel, mask_name, cat, str(style))
                if mat_key not in mat_cache:
                    mat_rel = f"Assets/Prefabs/Characters/Variants/Materials/{v['base']}_{guid_for('mat:'+str(mat_key))[:10]}.mat"
                    # include variant name so materials are not shared across looks that collided
                    mat_rel = f"Assets/Prefabs/Characters/Variants/Materials/{v['name']}__{r['name']}_{slot}.mat"
                    # sharing within a variant: if key repeats, reuse
                if mat_key not in mat_cache:
                    safe_r = re.sub(r"[^A-Za-z0-9]+", "_", r["name"])[:24]
                    mat_rel = f"Assets/Prefabs/Characters/Variants/Materials/{v['name']}__{safe_r}_{slot}_{guid_for(str(mat_key))[:6]}.mat"
                    body = clone_material(info["text"], Path(mat_rel).stem, tex_guid, masks[mask_name], style, mask_name)
                    (ROOT / mat_rel).write_text(body)
                    mg = guid_for("mat:" + mat_rel)
                    Path(str(ROOT / mat_rel) + ".meta").write_text(mat_meta(mg))
                    mat_cache[mat_key] = (mat_rel, mg)
                mat_rel, mg = mat_cache[mat_key]
                mat_assigns.append({
                    "renderer": r["name"], "renderer_id": r["id"], "slot": slot,
                    "material": mat_rel, "mat_guid": mg, "cat": cat,
                })
                label = f"{r['name'][:10]}:{cat}"
                if label not in seen_thumbs and len(thumbs) < 8:
                    seen_thumbs.add(label)
                    thumbs.append((label, painted))
        mesh_swaps = []
        if v["hair_n"]:
            file_id, mesh_name = HAIR_MESH[v["hair_n"]]
            for r in parsed["renderers"]:
                if r["name"] == "Hair":
                    mesh_swaps.append({
                        "renderer": "Hair", "renderer_id": r["id"], "fileID": file_id,
                        "guid": read_guid(BASE_FBX), "fbx": BASE_FBX, "meshName": mesh_name,
                    })
        gear = []
        accessories = []
        for key in v["gear"]:
            slot, pos, euler, scl = ACC_XFORM[key]
            path = ACC_PATH[key]
            accessories.append({
                "prefab": path, "guid": read_guid(path), "boneSlot": slot,
                "px": pos[0], "py": pos[1], "pz": pos[2],
                "ex": euler[0], "ey": euler[1], "ez": euler[2],
                "sx": scl[0], "sy": scl[1], "sz": scl[2], "key": key,
            })
            gear.append(f"{key} on {slot}")
        disabled, scaled = write_variant_prefab(v, base_guid, parsed, mat_assigns, expressions, accessories, mesh_swaps)
        note = v["notes"]
        if v["base"] == "C04_UnityChan":
            note = (note + " " if note else "") + "Unity-chan derivative: ship the UCL mark with a release build."
        contact_sheet(v, thumbs, disabled, scaled, expressions, gear, note)
        recipe.append({
            "name": v["name"],
            "basePrefab": base_path,
            "description": v["desc"],
            "rootScale": v["root"],
            "disable": list(v["disable"]),
            "scales": [{"contains": a, "factor": b} for a, b in v["scales"]],
            "materials": [{"renderer": a["renderer"], "slot": a["slot"], "material": a["material"]} for a in mat_assigns],
            "meshSwaps": [{"renderer": s["renderer"], "fbx": s["fbx"], "meshName": s["meshName"]} for s in mesh_swaps],
            "expressions": expressions,
            "accessories": [{k: a[k] for k in ("prefab", "boneSlot", "px", "py", "pz", "ex", "ey", "ez", "sx", "sy", "sz")} for a in accessories],
        })
        readme_rows.append({
            "name": v["name"], "base": v["base"], "root": v["root"], "desc": v["desc"],
            "off": ", ".join(disabled) if disabled else "—",
            "expr": ", ".join(e["shapeName"] for e in expressions) if expressions else "—",
            "gear": ", ".join(gear) if gear else "—",
            "bones": len(scaled), "notes": note, "mats": len(mat_assigns),
        })
        overview_rows.append({
            "name": v["name"], "root": v["root"], "gear": ", ".join(g.split()[0] for g in gear),
            "swatch": (swatch_from_style(pal["cloth"]), swatch_from_style(pal["hair"]), swatch_from_style(pal["skin"])),
        })
        print(f"built {v['name']} mats={len(mat_assigns)} bones={len(scaled)} expr={len(expressions)}")

    recipe_path = OUT / "variant_recipe.json"
    recipe_path.write_text(json.dumps({"variants": recipe}, indent=2))
    Path(str(recipe_path) + ".meta").write_text(text_meta(guid_for("recipe")))
    ov = overview(overview_rows)
    write_readme(readme_rows)
    # artifacts
    art = Path("/opt/cursor/artifacts/variant-previews")
    art.mkdir(parents=True, exist_ok=True)
    for p in (OUT / "Previews").glob("*.png"):
        target = art / p.name
        target.write_bytes(p.read_bytes())
    tex_bytes = sum(p.stat().st_size for p in (OUT / "Textures").glob("*.png"))
    print(f"variants={len(all_v)} textures={len(list((OUT/'Textures').glob('*.png')))} bytes={tex_bytes} mats={len(list((OUT/'Materials').glob('*.mat')))}")
    print(f"overview {ov}")
    if warnings:
        print("WARNINGS", len(warnings))
        for w in warnings[:30]:
            print(" ", w)


def write_readme(rows):
    lines = []
    lines.append("# Character variants")
    lines.append("")
    lines.append("Every variant is a girl. Names describe silhouette and palette, not a separate brand.")
    lines.append("")
    lines.append("These prefabs are **variants** of the existing fighter prefabs. Nothing in the original prefabs, materials, or textures was edited. New materials use `DemonIdol/MDI/AnimeToon` (URP).")
    lines.append("")
    lines.append("## Which files Unity should trust")
    lines.append("")
    lines.append("The YAML prefabs in `Prefabs/` are the primary assets: part toggles, root height, non-humanoid bone scale, material swaps, Casual1 hair-mesh swaps, and an `MDIVariantLook` component.")
    lines.append("")
    lines.append("`MDIVariantLook` applies blend-shape presets and parents Sword, Greatsword, Dagger, Shield, Bow, Quiver, MageHat and Mask when the variant is played. Accessory rotations are a first guess.")
    lines.append("")
    lines.append("Menu **Demon Idol/Characters/Rebuild Variant Prefabs From Recipe** reads `variant_recipe.json` and rebuilds the same variants inside Unity, nesting the accessory models as prefab instances. Use that if the YAML needs to be regenerated. Do not run it expecting different designs; the recipe matches these files.")
    lines.append("")
    lines.append("## Unity-chan licence (UCL 2.0)")
    lines.append("")
    lines.append("The five UnityChan variants are derivative works of Unity-chan. The licence files under `Documentation/UnityChanLicense2.0` were not modified. A release build that still reads as Unity-chan must show the UCL logo or licence indication where people can see it. Unity-chan's own guidance says a character changed enough that she no longer looks like Unity-chan may fall outside UCL; that call belongs to the rights holder. These variants are recolours, part toggles and accessories on the same body, so treat them as UCL derivatives and keep the mark.")
    lines.append("")
    lines.append("## Not given variants")
    lines.append("")
    lines.append("| Skipped | Why |")
    lines.append("| --- | --- |")
    lines.append("| Ren (`C03_Ren`) | Kevin Iglesias practice dummy (`Base_Ren_Anime`). Empty humanoid map, grey mannequin, not a girl to recolour into a distinct fighter. |")
    lines.append("| Nordic_Einheri, Anime_Akane, Cyborg_Girl | Same dummy family as Ren (Anime_Akane and Cyborg_Girl are byte-identical copies). They are not fighter prefabs in `Prefabs/Characters`. |")
    lines.append("| TrainingDummy_Red / Purple / Yellow | Target dummies, not roster girls. |")
    lines.append("")
    lines.append("Akio, Aika, Akane and Cultist are single-mesh bodies. They cannot toggle jackets or hair pieces. Their four variants use separate hue-shifted textures per material slot (hair, skin, eyes, clothes), height, hair-bone scale where those bones exist, expression presets, and weapons.")
    lines.append("")
    lines.append("Casual1's humanoid avatar map is still empty. A new avatar was not added: the thighs and spine are siblings under Armature, so a meta-only map cannot put the hips on the pelvis. Editing `AnimeGirl.fbx.meta` would have changed an existing file. Retargeting risk is unchanged.")
    lines.append("")
    lines.append("The shipped UnityChan skin material points at a Cultist body texture. Variant skin materials are new files made from `skin_01.tga`. The original material was left as it is.")
    lines.append("")
    lines.append("## All variants")
    lines.append("")
    lines.append("| Variant | Base | Height | Parts off | Expression | Gear | Preview |")
    lines.append("| --- | --- | --- | --- | --- | --- | --- |")
    for row in rows:
        lines.append(
            f"| `{row['name']}` | {row['base']} | {row['root']} | {row['off']} | {row['expr']} | {row['gear']} | [sheet](Previews/{row['name']}.png) |"
        )
    lines.append("")
    lines.append(f"{len(rows)} variants. Cloth / hair / skin swatches: [overview](Previews/Variants_Overview.png).")
    lines.append("")
    lines.append("## What each one changes")
    lines.append("")
    for row in rows:
        lines.append(f"### {row['name']}")
        lines.append("")
        lines.append(row["desc"])
        lines.append("")
        lines.append(f"- Height scale `{row['root']}` on the prefab root (humanoid bones are not scaled).")
        lines.append(f"- Parts off: {row['off']}.")
        lines.append(f"- New AnimeToon materials: {row['mats']}. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).")
        lines.append(f"- Safe bone scales applied: {row['bones']} transforms (hair, ribbon, tail, hat, bag, halo and similar names only).")
        lines.append(f"- Expression: {row['expr']}.")
        lines.append(f"- Gear: {row['gear']}.")
        if row["notes"]:
            lines.append(f"- Note: {row['notes']}")
        lines.append("")
    lines.append("## What was not verified")
    lines.append("")
    lines.append("Unity was not opened. These files were not imported, and the models were not rendered. Headless mesh renders were not produced: this environment has no Blender/FBX viewport, so there is no front-view beauty render. The contact sheets show the edited textures and the toggle list only.")
    lines.append("")
    lines.append("Risks to check on first import:")
    lines.append("")
    lines.append("- Prefab-variant YAML is hand-authored. If Unity drops an override, run the rebuild menu.")
    lines.append("- Accessory position and rotation will need a pass in the editor. The component only guarantees the parent bone.")
    lines.append("- Casual1 hair swaps reuse the Hair renderer's bone list. Hair 1–5 come from the same armature; if a mesh's bone order differs, the hair will deform until the rebuild menu reassigns the mesh in Unity.")
    lines.append("- Fina hair and top toggles, and Muryotaisu with the coat off, can show gaps. The sheets call those out.")
    lines.append("- Scaling hair bones changes the silhouette and does not change the humanoid avatar. Root scale changes world height for everyone parented to the prefab, including hitboxes that live on that root.")
    lines.append("- PicoChan stays on the heavy base mesh. These variants do not decimate it.")
    lines.append("- SapphiArtchan remains Generic. Looks change; retargeting does not start working.")
    lines.append("")
    path = OUT / "README.md"
    path.write_text("\n".join(lines) + "\n")
    Path(str(path) + ".meta").write_text(text_meta(guid_for("readme-variants")))


if __name__ == "__main__":
    main()
