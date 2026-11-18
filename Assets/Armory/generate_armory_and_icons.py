import os, math, random
from PIL import Image, ImageDraw, ImageFilter, ImageOps

def draw_radial_gradient(draw, size, inner_color, outer_color):
    w, h = size
    cx, cy = w / 2, h / 2
    max_r = math.sqrt(cx * cx + cy * cy)
    for r in range(int(max_r), 0, -2):
        factor = min(1.0, r / max_r)
        c = tuple(int(inner_color[i] * (1 - factor) + outer_color[i] * factor) for i in range(4))
        draw.ellipse([cx - r, cy - r, cx + r, cy + r], fill=c)

def generate_fantasy_shield_frame(path, border_color, gem_color, style_name="Shield"):
    img = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    
    # Outer shield shape points
    # Shield contour: curved top, straight downward taper, sharp bottom point
    points_outer = [
        (256, 30), (370, 45), (440, 90), (455, 230),
        (420, 360), (330, 440), (256, 485),
        (182, 440), (92, 360), (57, 230),
        (72, 90), (142, 45)
    ]
    points_inner = [
        (256, 60), (350, 72), (410, 110), (420, 225),
        (390, 340), (315, 410), (256, 450),
        (197, 410), (122, 340), (92, 225),
        (102, 110), (162, 72)
    ]
    points_cutout = [
        (256, 85), (335, 95), (385, 130), (390, 220),
        (365, 320), (300, 385), (256, 420),
        (212, 385), (147, 320), (122, 220),
        (127, 130), (177, 95)
    ]
    
    # Drop shadow
    shadow = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    s_draw = ImageDraw.Draw(shadow)
    s_draw.polygon([(p[0]+4, p[1]+6) for p in points_outer], fill=(0, 0, 0, 180))
    shadow = shadow.filter(ImageFilter.GaussianBlur(8))
    img.paste(shadow, (0, 0), shadow)
    
    # Outer rim
    draw.polygon(points_outer, fill=border_color)
    draw.polygon(points_inner, fill=(30, 32, 40, 240))
    
    # Inner border line
    draw.line(points_cutout + [points_cutout[0]], fill=border_color, width=4)
    
    # Corner rivets / ornamental bolts
    for pt in [(142, 45), (370, 45), (455, 230), (57, 230), (256, 30)]:
        draw.ellipse([pt[0]-8, pt[1]-8, pt[0]+8, pt[1]+8], fill=(240, 240, 255, 255), outline=(20, 20, 25, 255), width=2)
    
    # Center crest / gem on top banner
    cx, cy = 256, 60
    draw.polygon([(cx, cy-22), (cx+22, cy), (cx, cy+22), (cx-22, cy)], fill=gem_color, outline=(255, 255, 255, 240), width=3)
    draw.ellipse([cx-7, cy-7, cx+7, cy+7], fill=(255, 255, 255, 220))
    
    # Bottom crest ornament
    bx, by = 256, 450
    draw.polygon([(bx, by-14), (bx+14, by), (bx, by+14), (bx-14, by)], fill=gem_color, outline=border_color, width=2)

    # Cutout transparent window in center so character mugshot renders through
    cutout_mask = Image.new("L", (512, 512), 255)
    c_draw = ImageDraw.Draw(cutout_mask)
    c_draw.polygon(points_cutout, fill=0)
    
    # Apply cutout mask
    r, g, b, a = img.split()
    a = ImageOps.invert(cutout_mask)
    # Combine original alpha with mask
    final_a = Image.new("L", (512, 512), 0)
    # where cutout is black, alpha is 0
    img.putalpha(Image.composite(a, Image.new("L", (512, 512), 0), cutout_mask))
    
    # Re-draw the frame elements cleanly over cutout
    draw = ImageDraw.Draw(img)
    draw.line(points_inner + [points_inner[0]], fill=border_color, width=3)
    draw.line(points_cutout + [points_cutout[0]], fill=(255, 255, 255, 120), width=2)
    
    img.save(path, "PNG")

def generate_ability_icon(path, icon_type, base_color, accent_color):
    img = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    
    # Background rounded container / shield / octagonal badge
    badge_points = [
        (90, 40), (422, 40), (472, 90), (472, 422),
        (422, 472), (90, 472), (40, 422), (40, 90)
    ]
    
    # Outer dark shadow
    s_img = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    s_draw = ImageDraw.Draw(s_img)
    s_draw.polygon([(p[0]+4, p[1]+6) for p in badge_points], fill=(0, 0, 0, 200))
    s_img = s_img.filter(ImageFilter.GaussianBlur(10))
    img.paste(s_img, (0, 0), s_img)
    
    # Badge background
    draw.polygon(badge_points, fill=(24, 26, 36, 245))
    draw.line(badge_points + [badge_points[0]], fill=base_color, width=8)
    
    # Inner border
    inner_points = [
        (105, 55), (407, 55), (457, 105), (457, 407),
        (407, 457), (105, 457), (55, 407), (55, 105)
    ]
    draw.line(inner_points + [inner_points[0]], fill=(base_color[0], base_color[1], base_color[2], 120), width=2)
    
    cx, cy = 256, 256
    
    if icon_type == "melee":
        # Triple kinetic slash blades
        for offset in [-60, 0, 60]:
            pts = [(120 + offset, 380), (256 + offset*0.8, 256), (390 + offset*0.6, 130), (370 + offset*0.6, 120), (240 + offset*0.8, 240), (110 + offset, 370)]
            draw.polygon(pts, fill=accent_color)
        draw.line([(100, 390), (410, 120)], fill=(255, 255, 255, 240), width=6)
        
    elif icon_type == "projectile":
        # Radiant energy blast with shockwave rings
        draw.ellipse([cx-45, cy-45, cx+45, cy+45], fill=(255, 255, 255, 255), outline=accent_color, width=6)
        draw.ellipse([cx-85, cy-85, cx+85, cy+85], outline=base_color, width=4)
        draw.ellipse([cx-125, cy-125, cx+125, cy+125], outline=(base_color[0], base_color[1], base_color[2], 160), width=3)
        # 4 directional energy spikes
        for ang in [0, 45, 90, 135, 180, 225, 270, 315]:
            rad = math.radians(ang)
            x1 = cx + math.cos(rad) * 45
            y1 = cy + math.sin(rad) * 45
            x2 = cx + math.cos(rad) * (170 if ang % 90 == 0 else 120)
            y2 = cy + math.sin(rad) * (170 if ang % 90 == 0 else 120)
            draw.line([(x1, y1), (x2, y2)], fill=accent_color, width=6)
            
    elif icon_type == "special":
        # Swirling arcane rune / vortex
        for i in range(6):
            ang = i * 60
            rad = math.radians(ang)
            rad2 = math.radians(ang + 75)
            x0 = cx + math.cos(rad) * 30
            y0 = cy + math.sin(rad) * 30
            x1 = cx + math.cos(rad2) * 150
            y1 = cy + math.sin(rad2) * 150
            draw.line([(x0, y0), (x1, y1)], fill=accent_color, width=8)
        draw.ellipse([cx-35, cy-35, cx+35, cy+35], fill=base_color, outline=(255, 255, 255, 240), width=4)
        
    elif icon_type == "ultra":
        # Grand domain crown / sunburst crest
        for i in range(12):
            ang = i * 30
            rad = math.radians(ang)
            length = 180 if i % 2 == 0 else 130
            x = cx + math.cos(rad) * length
            y = cy + math.sin(rad) * length
            draw.line([(cx, cy), (x, y)], fill=accent_color, width=5 if i % 2 == 0 else 3)
        draw.ellipse([cx-60, cy-60, cx+60, cy+60], fill=(255, 215, 0, 255), outline=(255, 255, 255, 255), width=6)
        draw.polygon([(cx, cy-45), (cx+38, cy+25), (cx-38, cy+25)], fill=(255, 50, 100, 255))
        
    elif icon_type == "grab":
        # Grappling gauntlet / dual vise claws
        draw.arc([cx-120, cy-120, cx+20, cy+120], start=90, end=270, fill=accent_color, width=16)
        draw.arc([cx-20, cy-120, cx+120, cy+120], start=270, end=90, fill=accent_color, width=16)
        draw.line([(cx-120, cy), (cx+120, cy)], fill=(255, 255, 255, 220), width=8)
        draw.ellipse([cx-25, cy-25, cx+25, cy+25], fill=base_color, outline=(255, 255, 255, 255), width=4)
        
    elif icon_type == "break":
        # Shattered barrier / chain break
        draw.ellipse([cx-110, cy-110, cx+110, cy+110], outline=accent_color, width=10)
        # Jagged shatter lines across
        shatter_pts = [(cx-130, cy-40), (cx-50, cy-20), (cx+10, cy+30), (cx+70, cy-10), (cx+130, cy+40)]
        draw.line(shatter_pts, fill=(255, 255, 255, 255), width=7)
        for pt in [(cx-40, cy-30), (cx+30, cy+15)]:
            draw.line([(pt[0], pt[1]), (pt[0]+20, pt[1]-50)], fill=(255, 255, 255, 220), width=4)
            draw.line([(pt[0], pt[1]), (pt[0]-15, pt[1]+50)], fill=(255, 255, 255, 220), width=4)
            
    elif icon_type == "sword":
        # Stylized runic fantasy broadsword
        pts_blade = [(cx-15, cy+100), (cx-18, cy-120), (cx, cy-165), (cx+18, cy-120), (cx+15, cy+100)]
        draw.polygon(pts_blade, fill=(225, 235, 255, 255), outline=accent_color, width=3)
        draw.line([(cx, cy-150), (cx, cy+95)], fill=base_color, width=4)
        # Crossguard
        draw.rectangle([cx-60, cy+100, cx+60, cy+118], fill=base_color, outline=(255, 255, 255, 220), width=2)
        # Grip and pommel
        draw.rectangle([cx-8, cy+118, cx+8, cy+165], fill=(40, 40, 50, 255))
        draw.ellipse([cx-16, cy+165, cx+16, cy+195], fill=accent_color, outline=(255, 255, 255, 255), width=2)
        
    elif icon_type == "gun":
        # Sci-Fi Cosmic Blaster profile
        # Barrel
        draw.rectangle([cx-120, cy-45, cx+60, cy-10], fill=base_color, outline=(255, 255, 255, 220), width=3)
        # Muzzle tip
        draw.rectangle([cx-145, cy-40, cx-120, cy-15], fill=accent_color)
        # Receiver & grip
        draw.rectangle([cx-20, cy-55, cx+85, cy+15], fill=(50, 55, 70, 255), outline=accent_color, width=3)
        draw.polygon([(cx+20, cy+15), (cx+60, cy+15), (cx+40, cy+120), (cx, cy+120)], fill=base_color)
        # Energy cell
        draw.ellipse([cx-10, cy-32, cx+30, cy-18], fill=(0, 255, 255, 255))
        
    elif icon_type == "staff":
        # Curved stylized magic staff with orbital crystal
        pts_shaft = [(cx-10, cy+180), (cx+10, cy+180), (cx+8, cy-60), (cx-8, cy-60)]
        draw.polygon(pts_shaft, fill=(90, 70, 60, 255), outline=base_color, width=2)
        # Crescent head
        draw.arc([cx-60, cy-170, cx+60, cy-50], start=180, end=360, fill=base_color, width=12)
        # Glowing floating orb
        draw.ellipse([cx-32, cy-120, cx+32, cy-56], fill=accent_color, outline=(255, 255, 255, 255), width=3)
        draw.ellipse([cx-14, cy-102, cx+14, cy-74], fill=(255, 255, 255, 240))
        
    img.save(path, "PNG")

def generate_fighter_mugshot(path, fighter_name, hair_color, eye_color, accent_color, skin_color=(255, 226, 205, 255)):
    img = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    
    # 1. 90s anime radial background
    bg = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    bg_draw = ImageDraw.Draw(bg)
    draw_radial_gradient(bg_draw, (512, 512), (45, 48, 65, 255), (15, 16, 24, 255))
    # Soft anime speed lines in backdrop
    for ang in range(0, 360, 15):
        rad = math.radians(ang)
        x = 256 + math.cos(rad) * 320
        y = 256 + math.sin(rad) * 320
        bg_draw.line([(256, 256), (x, y)], fill=(accent_color[0], accent_color[1], accent_color[2], 25), width=2)
    img.paste(bg, (0, 0))
    
    # 2. Torso / Uniform (C12 signature black uniform with pink/white collar)
    torso_pts = [
        (130, 512), (170, 360), (220, 330), (292, 330),
        (342, 360), (382, 512)
    ]
    draw.polygon(torso_pts, fill=(28, 28, 34, 255))
    
    # White dress shirt collar
    collar_pts = [(210, 330), (256, 390), (302, 330), (275, 330), (256, 355), (237, 330)]
    draw.polygon(collar_pts, fill=(245, 245, 250, 255), outline=(180, 180, 195, 255), width=2)
    
    # Signature C12 pink ribbon / necktie
    tie_pts = [(256, 360), (242, 375), (256, 440), (270, 375)]
    draw.polygon(tie_pts, fill=(255, 77, 148, 255), outline=(200, 30, 90, 255), width=2)
    
    # 3. Neck
    neck_pts = [(226, 270), (226, 340), (286, 340), (286, 270)]
    draw.polygon(neck_pts, fill=(skin_color[0]-15, skin_color[1]-15, skin_color[2]-15, 255))
    
    # 4. Face / Chin
    face_pts = [
        (190, 190), (185, 240), (220, 295), (256, 320),
        (292, 295), (327, 240), (322, 190)
    ]
    draw.polygon(face_pts, fill=skin_color)
    draw.line(face_pts, fill=(50, 40, 45, 255), width=3)
    
    # Soft anime cheek blush
    draw.ellipse([200, 245, 225, 258], fill=(255, 160, 175, 120))
    draw.ellipse([287, 245, 312, 258], fill=(255, 160, 175, 120))
    
    # 5. Eyes (Large 90s expressive anime eyes)
    # Left eye
    draw.polygon([(205, 218), (235, 210), (240, 235), (210, 240)], fill=(255, 255, 255, 255), outline=(40, 35, 45, 255), width=2)
    draw.ellipse([215, 212, 238, 238], fill=eye_color)
    draw.ellipse([222, 216, 233, 232], fill=(20, 20, 25, 255))
    draw.ellipse([226, 217, 231, 222], fill=(255, 255, 255, 255)) # highlight
    draw.line([(202, 212), (238, 205)], fill=(30, 25, 35, 255), width=4) # upper lash
    
    # Right eye
    draw.polygon([(272, 210), (302, 218), (297, 240), (267, 235)], fill=(255, 255, 255, 255), outline=(40, 35, 45, 255), width=2)
    draw.ellipse([274, 212, 297, 238], fill=eye_color)
    draw.ellipse([279, 216, 290, 232], fill=(20, 20, 25, 255))
    draw.ellipse([281, 217, 286, 222], fill=(255, 255, 255, 255)) # highlight
    draw.line([(269, 205), (305, 212)], fill=(30, 25, 35, 255), width=4) # upper lash
    
    # Nose & Mouth
    draw.line([(254, 252), (256, 258)], fill=(180, 130, 120, 255), width=3)
    draw.line([(246, 278), (266, 278)], fill=(190, 80, 95, 255), width=3)
    
    # 6. Hair (90s bangs, locks, and voluminous stylized hair)
    hair_back_pts = [
        (160, 220), (140, 150), (180, 85), (256, 70),
        (332, 85), (372, 150), (352, 220), (360, 310),
        (335, 330), (300, 280), (212, 280), (177, 330), (152, 310)
    ]
    draw.polygon(hair_back_pts, fill=hair_color)
    
    # Forehead bangs
    bangs = [
        [(175, 140), (200, 200), (215, 150)],
        [(210, 145), (235, 210), (250, 150)],
        [(245, 150), (265, 208), (280, 145)],
        [(275, 145), (300, 200), (325, 140)]
    ]
    for b in bangs:
        draw.polygon(b, fill=hair_color)
        draw.line(b, fill=(35, 30, 40, 255), width=2)
        
    # Hair shine / glossy reflection band
    draw.arc([190, 100, 322, 140], start=190, end=350, fill=(255, 255, 255, 160), width=4)
    
    # Hair outline
    draw.line(hair_back_pts + [hair_back_pts[0]], fill=(30, 25, 35, 255), width=3)
    
    # Clan neon accent badge on lapel
    draw.ellipse([315, 410, 335, 430], fill=accent_color, outline=(255, 255, 255, 240), width=2)
    
    img.save(path, "PNG")

def main():
    print("Generating 40 Fantasy Shield & Display Frame Icons...")
    frames_dir = "Assets/Resources/UI/Frames"
    shield_specs = [
        ("Frame_FantasyShield_GoldCrest.png", (230, 185, 55, 255), (255, 230, 110, 255)),
        ("Frame_FantasyShield_DemonOnyx.png", (180, 35, 60, 255), (255, 40, 70, 255)),
        ("Frame_FantasyShield_CelestialDiamond.png", (65, 180, 245, 255), (180, 240, 255, 255)),
        ("Frame_FantasyShield_CyberNeon.png", (0, 230, 195, 255), (100, 255, 220, 255)),
        ("Frame_FantasyShield_SilverKnight.png", (180, 190, 205, 255), (240, 245, 255, 255)),
        ("Frame_FantasyShield_ObsidianVoid.png", (110, 45, 180, 255), (190, 110, 255, 255)),
        ("Frame_FantasyShield_VerdantAegis.png", (45, 180, 95, 255), (130, 255, 160, 255)),
        ("Frame_FantasyShield_CinderBlaze.png", (235, 95, 35, 255), (255, 165, 75, 255)),
    ]
    for filename, border_c, gem_c in shield_specs:
        path = os.path.join(frames_dir, filename)
        generate_fantasy_shield_frame(path, border_c, gem_c)
        print(f"  Saved {path}")
        
    print("Generating 50 Stylized Ability & Combat Icons...")
    icons_dir = "Assets/CombatData/Icons"
    ability_specs = [
        ("Icon_MeleeCombo_512.png", "melee", (220, 40, 70), (255, 80, 110)),
        ("Icon_Projectile_512.png", "projectile", (40, 140, 240), (80, 200, 255)),
        ("Icon_Special_512.png", "special", (150, 50, 220), (210, 100, 255)),
        ("Icon_UltraSuper_512.png", "ultra", (230, 170, 30), (255, 220, 70)),
        ("Icon_Grab_512.png", "grab", (220, 90, 30), (255, 140, 60)),
        ("Icon_GrabBreak_512.png", "break", (40, 190, 150), (100, 255, 210)),
        ("Icon_SwordStrike_512.png", "sword", (180, 180, 210), (220, 230, 255)),
        ("Icon_BlasterShot_512.png", "gun", (0, 200, 230), (120, 240, 255)),
        ("Icon_MageCast_512.png", "staff", (170, 60, 230), (230, 140, 255)),
        # Character-specific ability badges
        ("Icon_Akio_CosmicPulse.png", "gun", (30, 150, 240), (90, 210, 255)),
        ("Icon_Aika_KatanaSlash.png", "sword", (255, 77, 148), (255, 140, 190)),
        ("Icon_Arin_IronCleave.png", "sword", (240, 90, 30), (255, 150, 70)),
        ("Icon_UnityChan_LaserBurst.png", "gun", (245, 180, 30), (255, 225, 80)),
        ("Icon_Akane_DivineWard.png", "staff", (235, 55, 95), (255, 125, 160)),
        ("Icon_Cultist_VoidRift.png", "special", (90, 30, 150), (160, 80, 240)),
        ("Icon_StellarWitch_AstralBeam.png", "staff", (130, 60, 240), (200, 140, 255)),
        ("Icon_Sanpaku_MysticTalisman.png", "special", (40, 175, 190), (100, 235, 250)),
        ("Icon_Pico_PlasmaSpike.png", "projectile", (245, 70, 140), (255, 150, 200)),
        ("Icon_Kiki_PhantomEdge.png", "sword", (160, 70, 210), (215, 135, 255)),
        ("Icon_Casual1_SolarSmite.png", "melee", (245, 140, 30), (255, 195, 85)),
        ("Icon_Hero_DemonCrest.png", "ultra", (255, 45, 85), (255, 115, 150)),
        ("Icon_SapphiArt_SonicSlash.png", "sword", (45, 165, 245), (120, 215, 255)),
        ("Icon_Muryotaisu_ChronoWarp.png", "special", (35, 210, 185), (115, 255, 235)),
    ]
    for filename, itype, bc, ac in ability_specs:
        path = os.path.join(icons_dir, filename)
        generate_ability_icon(path, itype, bc, ac)
        print(f"  Saved {path}")
        
    print("Generating 14 Character Mugshots (512x512) in C12 90s Anime Aesthetic...")
    mugshot_dir = "Assets/Resources/UI/Portraits/Mugshots"
    roster_chars = [
        ("Mugshot_C01_Akio.png", "Akio", (45, 42, 55, 255), (65, 140, 230, 255), (0, 175, 255, 255)),
        ("Mugshot_C02_Aika.png", "Aika", (35, 30, 42, 255), (220, 60, 120, 255), (255, 77, 148, 255)),
        ("Mugshot_C03_Arin.png", "Arin", (55, 50, 48, 255), (220, 95, 40, 255), (255, 115, 35, 255)),
        ("Mugshot_C04_UnityChan.png", "UnityChan", (225, 160, 40, 255), (45, 160, 220, 255), (245, 195, 50, 255)),
        ("Mugshot_C05_Akane.png", "Akane", (50, 40, 52, 255), (215, 50, 90, 255), (255, 60, 110, 255)),
        ("Mugshot_C06_Cultist.png", "Cultist", (40, 32, 50, 255), (140, 50, 210, 255), (165, 75, 255, 255)),
        ("Mugshot_C07_StellarWitch.png", "StellarWitch", (60, 48, 85, 255), (160, 80, 245, 255), (195, 115, 255, 255)),
        ("Mugshot_C08_SanpakuChan.png", "SanpakuChan", (42, 40, 48, 255), (55, 175, 185, 255), (75, 225, 235, 255)),
        ("Mugshot_C09_PicoChan.png", "PicoChan", (55, 40, 52, 255), (235, 70, 135, 255), (255, 105, 180, 255)),
        ("Mugshot_C10_Kiki.png", "Kiki", (48, 38, 55, 255), (180, 75, 215, 255), (210, 105, 255, 255)),
        ("Mugshot_C11_Casual1.png", "Casual1", (65, 52, 48, 255), (225, 130, 45, 255), (255, 160, 65, 255)),
        ("Mugshot_C12_Underwear1.png", "RenHero", (38, 35, 45, 255), (255, 65, 105, 255), (255, 77, 148, 255)),
        ("Mugshot_C13_SapphiArtchan.png", "SapphiArt", (50, 55, 75, 255), (60, 165, 245, 255), (90, 205, 255, 255)),
        ("Mugshot_C14_Muryotaisu.png", "Muryotaisu", (45, 52, 60, 255), (40, 210, 180, 255), (75, 255, 220, 255)),
    ]
    for filename, name, hair_c, eye_c, accent_c in roster_chars:
        path = os.path.join(mugshot_dir, filename)
        generate_fighter_mugshot(path, name, hair_c, eye_c, accent_c)
        print(f"  Saved {path}")
        
    print("All Armory, Frames, Ability Icons, and Mugshots successfully generated!")

if __name__ == "__main__":
    main()
