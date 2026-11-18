# Character variants

Every variant is a girl. Names describe silhouette and palette, not a separate brand.

These prefabs are **variants** of the existing fighter prefabs. Nothing in the original prefabs, materials, or textures was edited. New materials use `DemonIdol/MDI/AnimeToon` (URP).

## Which files Unity should trust

The YAML prefabs in `Prefabs/` are the primary assets: part toggles, root height, non-humanoid bone scale, material swaps, Casual1 hair-mesh swaps, and an `MDIVariantLook` component.

`MDIVariantLook` applies blend-shape presets and parents Sword, Greatsword, Dagger, Shield, Bow, Quiver, MageHat and Mask when the variant is played. Accessory rotations are a first guess.

Menu **Demon Idol/Characters/Rebuild Variant Prefabs From Recipe** reads `variant_recipe.json` and rebuilds the same variants inside Unity, nesting the accessory models as prefab instances. Use that if the YAML needs to be regenerated. Do not run it expecting different designs; the recipe matches these files.

## Unity-chan licence (UCL 2.0)

The five UnityChan variants are derivative works of Unity-chan. The licence files under `Documentation/UnityChanLicense2.0` were not modified. A release build that still reads as Unity-chan must show the UCL logo or licence indication where people can see it. Unity-chan's own guidance says a character changed enough that she no longer looks like Unity-chan may fall outside UCL; that call belongs to the rights holder. These variants are recolours, part toggles and accessories on the same body, so treat them as UCL derivatives and keep the mark.

## Not given variants

| Skipped | Why |
| --- | --- |
| Ren (`C03_Ren`) | Kevin Iglesias practice dummy (`Base_Ren_Anime`). Empty humanoid map, grey mannequin, not a girl to recolour into a distinct fighter. |
| Nordic_Einheri, Anime_Akane, Cyborg_Girl | Same dummy family as Ren (Anime_Akane and Cyborg_Girl are byte-identical copies). They are not fighter prefabs in `Prefabs/Characters`. |
| TrainingDummy_Red / Purple / Yellow | Target dummies, not roster girls. |

Akio, Aika, Akane and Cultist are single-mesh bodies. They cannot toggle jackets or hair pieces. Their four variants use separate hue-shifted textures per material slot (hair, skin, eyes, clothes), height, hair-bone scale where those bones exist, expression presets, and weapons.

Casual1's humanoid avatar map is still empty. A new avatar was not added: the thighs and spine are siblings under Armature, so a meta-only map cannot put the hips on the pelvis. Editing `AnimeGirl.fbx.meta` would have changed an existing file. Retargeting risk is unchanged.

The shipped UnityChan skin material points at a Cultist body texture. Variant skin materials are new files made from `skin_01.tga`. The original material was left as it is.

## All variants

| Variant | Base | Height | Parts off | Expression | Gear | Preview |
| --- | --- | --- | --- | --- | --- | --- |
| `UnityChan_V1_ShortHairDuelist` | C04_UnityChan | 1.0 | tail, tail_bottom, hair_accce, hairband | MTH_ANG2, blendShape1.MTH_ANG2 | sword on rightHand | [sheet](Previews/UnityChan_V1_ShortHairDuelist.png) |
| `UnityChan_V2_JacketlessScholar` | C04_UnityChan | 0.96 | uwagi, uwagi_BK, button | MTH_SMILE1, blendShape1.MTH_SMILE1 | hat on head | [sheet](Previews/UnityChan_V2_JacketlessScholar.png) |
| `UnityChan_V3_RibbonKnight` | C04_UnityChan | 1.05 | — | MTH_SMILE1, blendShape1.MTH_SMILE1 | sword on rightHand, shield on leftHand | [sheet](Previews/UnityChan_V3_RibbonKnight.png) |
| `UnityChan_V4_SleevelessNight` | C04_UnityChan | 1.0 | shirts_sode, shirts_sode_BK, hairband | MTH_ANG2, blendShape1.MTH_ANG2 | dagger on rightHand, mask on head | [sheet](Previews/UnityChan_V4_SleevelessNight.png) |
| `UnityChan_V5_PetiteArcher` | C04_UnityChan | 0.92 | — | MTH_SMILE2, blendShape1.MTH_SMILE2 | bow on leftHand, quiver on spine | [sheet](Previews/UnityChan_V5_PetiteArcher.png) |
| `PicoChan_V1_StreetMechanic` | C09_PicoChan | 1.0 | jacket_base, jacket_sub, jacket_fastener, jacket_fastener_top, goggle_base, goggle_belt, goggle_lens, picolabo_base, picolabo_metal, hair_tail_01, hair_tail_02, hair_tail_03, hair_tail_04 | Angry, blendShape1.Angry | sword on rightHand | [sheet](Previews/PicoChan_V1_StreetMechanic.png) |
| `PicoChan_V2_HoodedPilot` | C09_PicoChan | 0.94 | goggle_base, goggle_belt, goggle_lens, picolabo_base, picolabo_metal | Joy, blendShape1.Joy | hat on head | [sheet](Previews/PicoChan_V2_HoodedPilot.png) |
| `PicoChan_V3_BagCompanion` | C09_PicoChan | 1.03 | jacket_base, jacket_sub, jacket_fastener, jacket_fastener_top | Fun, blendShape1.Fun | dagger on rightHand | [sheet](Previews/PicoChan_V3_BagCompanion.png) |
| `PicoChan_V4_ShortHairKnight` | C09_PicoChan | 1.06 | hair_tail_01, hair_tail_02, hair_tail_03, hair_tail_04 | Angry, blendShape1.Angry | sword on rightHand, shield on leftHand | [sheet](Previews/PicoChan_V4_ShortHairKnight.png) |
| `PicoChan_V5_MageTinkerer` | C09_PicoChan | 0.92 | jacket_base, jacket_sub, jacket_fastener, jacket_fastener_top, goggle_base, goggle_belt, goggle_lens, picolabo_base, picolabo_metal | Joy, blendShape1.Joy | hat on head | [sheet](Previews/PicoChan_V5_MageTinkerer.png) |
| `Casual1_V1_Hair1SmileFencer` | C11_Casual1 | 1.0 | — | slight_smile, smile | sword on rightHand | [sheet](Previews/Casual1_V1_Hair1SmileFencer.png) |
| `Casual1_V2_Hair2FangedRogue` | C11_Casual1 | 0.95 | — | angry_face, angry, spike_teeth, spike_teeth3 | dagger on rightHand | [sheet](Previews/Casual1_V2_Hair2FangedRogue.png) |
| `Casual1_V3_Hair3TiredMage` | C11_Casual1 | 1.0 | — | sad | hat on head | [sheet](Previews/Casual1_V3_Hair3TiredMage.png) |
| `Casual1_V4_Hair4TallArcher` | C11_Casual1 | 1.07 | — | surprise, scream | bow on leftHand, quiver on spine | [sheet](Previews/Casual1_V4_Hair4TallArcher.png) |
| `Casual1_V5_Hair5MaskedGuard` | C11_Casual1 | 0.98 | — | smile_eyes | mask on head, shield on leftHand | [sheet](Previews/Casual1_V5_Hair5MaskedGuard.png) |
| `Casual1_V6_Hair4MakeupChampion` | C11_Casual1 | 1.03 | — | angry_face, angry, spike_teeth, spike_teeth3 | greatsword on rightHand | [sheet](Previews/Casual1_V6_Hair4MakeupChampion.png) |
| `Akio_V1_CrimsonFencer` | C01_Akio | 0.97 | — | Fcl_ALL_Angry, Fcl_BRW_Angry | sword on rightHand | [sheet](Previews/Akio_V1_CrimsonFencer.png) |
| `Akio_V2_NightScholar` | C01_Akio | 1.0 | — | Fcl_ALL_Sorrow, Fcl_BRW_Sorrow | hat on head, dagger on rightHand | [sheet](Previews/Akio_V2_NightScholar.png) |
| `Akio_V3_GroveArcher` | C01_Akio | 1.06 | — | Fcl_ALL_Fun, Fcl_BRW_Fun | bow on leftHand, quiver on spine | [sheet](Previews/Akio_V3_GroveArcher.png) |
| `Akio_V4_GoldCaptain` | C01_Akio | 1.08 | — | Fcl_ALL_Joy, Fcl_BRW_Joy | sword on rightHand, shield on leftHand | [sheet](Previews/Akio_V4_GoldCaptain.png) |
| `Aika_V1_RoseIdol` | C02_Aika | 0.94 | — | Fcl_ALL_Joy, Fcl_BRW_Joy | dagger on rightHand | [sheet](Previews/Aika_V1_RoseIdol.png) |
| `Aika_V2_FrostMage` | C02_Aika | 1.03 | — | Fcl_ALL_Surprised, Fcl_BRW_Surprised | hat on head | [sheet](Previews/Aika_V2_FrostMage.png) |
| `Aika_V3_OliveArcher` | C02_Aika | 1.0 | — | Fcl_ALL_Fun, Fcl_BRW_Fun | bow on leftHand, quiver on spine | [sheet](Previews/Aika_V3_OliveArcher.png) |
| `Aika_V4_WineGuard` | C02_Aika | 1.06 | — | Fcl_ALL_Angry, Fcl_BRW_Angry | sword on rightHand, shield on leftHand | [sheet](Previews/Aika_V4_WineGuard.png) |
| `Akane_V1_PastelWitch` | C05_Akane | 0.96 | — | Fcl_ALL_Fun, Fcl_BRW_Fun | hat on head | [sheet](Previews/Akane_V1_PastelWitch.png) |
| `Akane_V2_InkDuelist` | C05_Akane | 1.0 | — | Fcl_ALL_Angry, Fcl_BRW_Angry | sword on rightHand | [sheet](Previews/Akane_V2_InkDuelist.png) |
| `Akane_V3_SunnyArcher` | C05_Akane | 1.08 | — | Fcl_ALL_Joy, Fcl_BRW_Joy | bow on leftHand, quiver on spine | [sheet](Previews/Akane_V3_SunnyArcher.png) |
| `Akane_V4_MossSquire` | C05_Akane | 1.02 | — | Fcl_ALL_Sorrow, Fcl_BRW_Sorrow | shield on leftHand | [sheet](Previews/Akane_V4_MossSquire.png) |
| `Cultist_V1_WhiteCantor` | C06_Cultist | 1.0 | — | Face.M_F00_000_00_Fcl_ALL_Joy, Fcl_ALL_Joy | mask on head | [sheet](Previews/Cultist_V1_WhiteCantor.png) |
| `Cultist_V2_CrimsonRite` | C06_Cultist | 0.96 | — | Face.M_F00_000_00_Fcl_ALL_Angry, Fcl_ALL_Angry | dagger on rightHand | [sheet](Previews/Cultist_V2_CrimsonRite.png) |
| `Cultist_V3_VerdantOracle` | C06_Cultist | 1.05 | — | Face.M_F00_000_00_Fcl_ALL_Fun, Fcl_ALL_Fun | hat on head | [sheet](Previews/Cultist_V3_VerdantOracle.png) |
| `Cultist_V4_NightBow` | C06_Cultist | 1.0 | — | Face.M_F00_000_00_Fcl_ALL_Sorrow, Fcl_ALL_Sorrow | bow on leftHand, quiver on spine | [sheet](Previews/Cultist_V4_NightBow.png) |
| `StellarWitch_V1_UnrobedDuelist` | C07_StellarWitch | 1.02 | hat, robe, scart | — | sword on rightHand | [sheet](Previews/StellarWitch_V1_UnrobedDuelist.png) |
| `StellarWitch_V2_NightOracle` | C07_StellarWitch | 0.96 | — | — | mask on head | [sheet](Previews/StellarWitch_V2_NightOracle.png) |
| `StellarWitch_V3_ScarflessArcher` | C07_StellarWitch | 1.0 | scart | — | bow on leftHand, quiver on spine | [sheet](Previews/StellarWitch_V3_ScarflessArcher.png) |
| `StellarWitch_V4_TaillessKnight` | C07_StellarWitch | 1.07 | backtail, earling | — | greatsword on rightHand, shield on leftHand | [sheet](Previews/StellarWitch_V4_TaillessKnight.png) |
| `SanpakuChan_V1_PinkBlade` | C08_SanpakuChan | 0.98 | — | — | sword on rightHand | [sheet](Previews/SanpakuChan_V1_PinkBlade.png) |
| `SanpakuChan_V2_BlueMage` | C08_SanpakuChan | 1.0 | — | — | hat on head | [sheet](Previews/SanpakuChan_V2_BlueMage.png) |
| `SanpakuChan_V3_InkMask` | C08_SanpakuChan | 1.04 | — | — | mask on head, dagger on rightHand | [sheet](Previews/SanpakuChan_V3_InkMask.png) |
| `SanpakuChan_V4_SeaArcher` | C08_SanpakuChan | 0.94 | — | — | bow on leftHand, quiver on spine | [sheet](Previews/SanpakuChan_V4_SeaArcher.png) |
| `Reika_V1_HatlessSnow` | C10_Reika | 0.95 | Hat_mesh.001 | Ahh | sword on rightHand | [sheet](Previews/Reika_V1_HatlessSnow.png) |
| `Reika_V2_CrimsonCarol` | C10_Reika | 1.0 | — | eyeBrow Height | dagger on rightHand | [sheet](Previews/Reika_V2_CrimsonCarol.png) |
| `Reika_V3_HollyArcher` | C10_Reika | 1.06 | Hat_mesh.001 | Big Ahh | bow on leftHand, quiver on spine | [sheet](Previews/Reika_V3_HollyArcher.png) |
| `Reika_V4_MidnightBell` | C10_Reika | 1.02 | — | Sad | mask on head | [sheet](Previews/Reika_V4_MidnightBell.png) |
| `Fina_V1_CoralSprinter` | C12_Fina | 1.0 | HairBack | Fcl_BRW_Joy, Fcl_EYE_Joy | sword on rightHand | [sheet](Previews/Fina_V1_CoralSprinter.png) |
| `Fina_V2_BobDuelist` | C12_Fina | 0.96 | HairBack, HairMid | Fcl_BRW_Angry, Fcl_EYE_Angry | dagger on rightHand | [sheet](Previews/Fina_V2_BobDuelist.png) |
| `Fina_V3_FullHairArcher` | C12_Fina | 1.05 | — | Fcl_BRW_Fun, Fcl_EYE_Fun | bow on leftHand, quiver on spine | [sheet](Previews/Fina_V3_FullHairArcher.png) |
| `Fina_V4_SleevelessKnight` | C12_Fina | 1.03 | topic | Fcl_BRW_Angry, Fcl_EYE_Angry | greatsword on rightHand, shield on leftHand | [sheet](Previews/Fina_V4_SleevelessKnight.png) |
| `SapphiArtchan_V1_ClassRibbon` | C13_SapphiArtchan | 1.0 | Colar_Tile, BackSkirt_Tile | face_smile_BS, Face_Blendshape.face_smile_BS | sword on rightHand | [sheet](Previews/SapphiArtchan_V1_ClassRibbon.png) |
| `SapphiArtchan_V2_MechKnight` | C13_SapphiArtchan | 1.06 | Skirt, Skirt_Inside, BackSkirt_Inside, BackSkirt_Tile | brow_anger_L, Brow_BS.brow_anger_L | sword on rightHand, shield on leftHand | [sheet](Previews/SapphiArtchan_V2_MechKnight.png) |
| `SapphiArtchan_V3_SkirtMage` | C13_SapphiArtchan | 0.97 | Mech_Cover_L, Mech_Cover_R, Mech_Outer_L, Mech_Outer_R | brow_Up_L, Brow_BS.brow_Up_L | hat on head | [sheet](Previews/SapphiArtchan_V3_SkirtMage.png) |
| `SapphiArtchan_V4_LiteArcher` | C13_SapphiArtchan | 0.94 | Colar_Tile, BackSkirt_Tile | face_sad_BS, Face_Blendshape.face_sad_BS | bow on leftHand, quiver on spine | [sheet](Previews/SapphiArtchan_V4_LiteArcher.png) |
| `Muryotaisu_V1_HaloKnight` | C14_Muryotaisu | 1.0 | — | Smile | sword on rightHand | [sheet](Previews/Muryotaisu_V1_HaloKnight.png) |
| `Muryotaisu_V2_CoatlessDuelist` | C14_Muryotaisu | 0.95 | Coat | Trouble | dagger on rightHand | [sheet](Previews/Muryotaisu_V2_CoatlessDuelist.png) |
| `Muryotaisu_V3_RinglessMage` | C14_Muryotaisu | 1.05 | AngelRing | MouthOpen | hat on head | [sheet](Previews/Muryotaisu_V3_RinglessMage.png) |
| `Muryotaisu_V4_GroveArcher` | C14_Muryotaisu | 1.02 | — | EyeClose | bow on leftHand, quiver on spine | [sheet](Previews/Muryotaisu_V4_GroveArcher.png) |

56 variants. Cloth / hair / skin swatches: [overview](Previews/Variants_Overview.png).

## What each one changes

### UnityChan_V1_ShortHairDuelist

She wears her hair short: twin tails, tail tips, clips and head ribbons are off. Crimson jacket, black shirt, gold trim, fair skin, blue eyes, angry face, sword in her right hand.

- Height scale `1.0` on the prefab root (humanoid bones are not scaled).
- Parts off: tail, tail_bottom, hair_accce, hairband.
- New AnimeToon materials: 15. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 0 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: MTH_ANG2, blendShape1.MTH_ANG2.
- Gear: sword on rightHand.
- Note: Per-garment copies of the shared body atlas recolor the jacket, shirt, skirt and legs separately. Unity-chan derivative: ship the UCL mark with a release build.

### UnityChan_V2_JacketlessScholar

Jacket, vest back and buttons are off, so the shirt and skirt read on their own. Navy and gold, honey hair, a mage hat, and a calm smile. Slightly shorter.

- Height scale `0.96` on the prefab root (humanoid bones are not scaled).
- Parts off: uwagi, uwagi_BK, button.
- New AnimeToon materials: 16. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 24 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: MTH_SMILE1, blendShape1.MTH_SMILE1.
- Gear: hat on head.
- Note: Unity-chan derivative: ship the UCL mark with a release build.

### UnityChan_V3_RibbonKnight

Ribbons and twin tails are larger. White-and-gold uniform, warm eyes, sword and shield. Taller than the base.

- Height scale `1.05` on the prefab root (humanoid bones are not scaled).
- Parts off: —.
- New AnimeToon materials: 19. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 30 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: MTH_SMILE1, blendShape1.MTH_SMILE1.
- Gear: sword on rightHand, shield on leftHand.
- Note: Unity-chan derivative: ship the UCL mark with a release build.

### UnityChan_V4_SleevelessNight

Sleeves and the head ribbon are off. Violet-and-black clothes, near-black hair, red eyes, a half mask and a dagger.

- Height scale `1.0` on the prefab root (humanoid bones are not scaled).
- Parts off: shirts_sode, shirts_sode_BK, hairband.
- New AnimeToon materials: 16. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 0 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: MTH_ANG2, blendShape1.MTH_ANG2.
- Gear: dagger on rightHand, mask on head.
- Note: Unity-chan derivative: ship the UCL mark with a release build.

### UnityChan_V5_PetiteArcher

Petite archer. Teal hair worn longer, forest clothes, green eyes, bow and quiver.

- Height scale `0.92` on the prefab root (humanoid bones are not scaled).
- Parts off: —.
- New AnimeToon materials: 19. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 24 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: MTH_SMILE2, blendShape1.MTH_SMILE2.
- Gear: bow on leftHand, quiver on spine.
- Note: Unity-chan derivative: ship the UCL mark with a release build.

### PicoChan_V1_StreetMechanic

Street clothes: jacket, goggles, bag robot and the four hair tails are off. Orange shirt, pink bob, angry expression, sword.

- Height scale `1.0` on the prefab root (humanoid bones are not scaled).
- Parts off: jacket_base, jacket_sub, jacket_fastener, jacket_fastener_top, goggle_base, goggle_belt, goggle_lens, picolabo_base, picolabo_metal, hair_tail_01, hair_tail_02, hair_tail_03, hair_tail_04.
- New AnimeToon materials: 20. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 0 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: Angry, blendShape1.Angry.
- Gear: sword on rightHand.

### PicoChan_V2_HoodedPilot

Hooded jacket stays. Goggles and the bag robot are off. Ice-blue clothes, blue hair, mage hat. A little shorter.

- Height scale `0.94` on the prefab root (humanoid bones are not scaled).
- Parts off: goggle_base, goggle_belt, goggle_lens, picolabo_base, picolabo_metal.
- New AnimeToon materials: 28. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 31 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: Joy, blendShape1.Joy.
- Gear: hat on head.

### PicoChan_V3_BagCompanion

Jacket off so the shirt shows. Goggles and a larger bag robot stay. Rose clothes, rose hair, cheerful face, dagger.

- Height scale `1.03` on the prefab root (humanoid bones are not scaled).
- Parts off: jacket_base, jacket_sub, jacket_fastener, jacket_fastener_top.
- New AnimeToon materials: 29. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 4 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: Fun, blendShape1.Fun.
- Gear: dagger on rightHand.

### PicoChan_V4_ShortHairKnight

Hair tails off, jacket on in white and gold. Sword and shield. Tallest Pico variant.

- Height scale `1.06` on the prefab root (humanoid bones are not scaled).
- Parts off: hair_tail_01, hair_tail_02, hair_tail_03, hair_tail_04.
- New AnimeToon materials: 29. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 0 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: Angry, blendShape1.Angry.
- Gear: sword on rightHand, shield on leftHand.

### PicoChan_V5_MageTinkerer

Jacket, goggles and bag are off. Long purple hair, violet shirt, mage hat. Shortest Pico variant.

- Height scale `0.92` on the prefab root (humanoid bones are not scaled).
- Parts off: jacket_base, jacket_sub, jacket_fastener, jacket_fastener_top, goggle_base, goggle_belt, goggle_lens, picolabo_base, picolabo_metal.
- New AnimeToon materials: 24. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 31 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: Joy, blendShape1.Joy.
- Gear: hat on head.

### Casual1_V1_Hair1SmileFencer

Hair 1, shipped eye colour 2, smile, warm casual outfit, fair skin, sword. One of the five shipped hairstyles.

- Height scale `1.0` on the prefab root (humanoid bones are not scaled).
- Parts off: —.
- New AnimeToon materials: 7. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 0 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: slight_smile, smile.
- Gear: sword on rightHand.

### Casual1_V2_Hair2FangedRogue

Hair 2, shipped eye colour 3, angry face with fangs, black-red outfit, darker hair, dagger.

- Height scale `0.95` on the prefab root (humanoid bones are not scaled).
- Parts off: —.
- New AnimeToon materials: 7. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 0 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: angry_face, angry, spike_teeth, spike_teeth3.
- Gear: dagger on rightHand.

### Casual1_V3_Hair3TiredMage

Hair 3, shipped eye colour 4, sad expression, mint outfit, mage hat.

- Height scale `1.0` on the prefab root (humanoid bones are not scaled).
- Parts off: —.
- New AnimeToon materials: 7. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 0 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: sad.
- Gear: hat on head.

### Casual1_V4_Hair4TallArcher

Hair 4, shipped eye colour 2 with a cooler grade, surprised expression, sky outfit, bow and quiver. Tallest Casual variant.

- Height scale `1.07` on the prefab root (humanoid bones are not scaled).
- Parts off: —.
- New AnimeToon materials: 7. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 0 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: surprise, scream.
- Gear: bow on leftHand, quiver on spine.

### Casual1_V5_Hair5MaskedGuard

Hair 5, shipped eye colour 3, cheerful eyes, plum outfit, half mask and shield.

- Height scale `0.98` on the prefab root (humanoid bones are not scaled).
- Parts off: —.
- New AnimeToon materials: 7. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 0 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: smile_eyes.
- Gear: mask on head, shield on leftHand.

### Casual1_V6_Hair4MakeupChampion

Hair 4 again, but with the shipped makeup face, eye colour 4, fangs, sand-and-gold outfit and a greatsword. Reads apart from the tall archer.

- Height scale `1.03` on the prefab root (humanoid bones are not scaled).
- Parts off: —.
- New AnimeToon materials: 7. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 0 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: angry_face, angry, spike_teeth, spike_teeth3.
- Gear: greatsword on rightHand.

### Akio_V1_CrimsonFencer

One-piece school body, so parts cannot be toggled. Crimson bottoms, auburn hair scaled up, blue eyes, angry face, sword. Slightly shorter.

- Height scale `0.97` on the prefab root (humanoid bones are not scaled).
- Parts off: —.
- New AnimeToon materials: 6. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 39 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: Fcl_ALL_Angry, Fcl_BRW_Angry.
- Gear: sword on rightHand.
- Note: Single mesh. Distinction is palette, hair-bone scale, height, face preset and weapon.

### Akio_V2_NightScholar

Violet uniform, silver-lilac hair worn smaller, red eyes, sorrowful face, mage hat and dagger.

- Height scale `1.0` on the prefab root (humanoid bones are not scaled).
- Parts off: —.
- New AnimeToon materials: 6. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 39 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: Fcl_ALL_Sorrow, Fcl_BRW_Sorrow.
- Gear: hat on head, dagger on rightHand.

### Akio_V3_GroveArcher

Olive uniform, warm brown hair, green eyes, cheerful face, bow and quiver. Taller.

- Height scale `1.06` on the prefab root (humanoid bones are not scaled).
- Parts off: —.
- New AnimeToon materials: 6. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 0 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: Fcl_ALL_Fun, Fcl_BRW_Fun.
- Gear: bow on leftHand, quiver on spine.

### Akio_V4_GoldCaptain

White-and-gold uniform, blonde hair, amber eyes, bright smile, sword and shield. Tallest Akio variant.

- Height scale `1.08` on the prefab root (humanoid bones are not scaled).
- Parts off: —.
- New AnimeToon materials: 6. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 39 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: Fcl_ALL_Joy, Fcl_BRW_Joy.
- Gear: sword on rightHand, shield on leftHand.

### Aika_V1_RoseIdol

Rose uniform and rose hair, blue eyes, smile, dagger. Shortest Aika variant. Separate from Akio's crimson fencer by hue and weapon.

- Height scale `0.94` on the prefab root (humanoid bones are not scaled).
- Parts off: —.
- New AnimeToon materials: 6. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 34 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: Fcl_ALL_Joy, Fcl_BRW_Joy.
- Gear: dagger on rightHand.

### Aika_V2_FrostMage

Ice-blue uniform, pale silver hair, blue eyes, surprised face, mage hat.

- Height scale `1.03` on the prefab root (humanoid bones are not scaled).
- Parts off: —.
- New AnimeToon materials: 6. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 0 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: Fcl_ALL_Surprised, Fcl_BRW_Surprised.
- Gear: hat on head.

### Aika_V3_OliveArcher

Moss uniform, dark-blonde hair, green eyes, cheerful face, bow and quiver.

- Height scale `1.0` on the prefab root (humanoid bones are not scaled).
- Parts off: —.
- New AnimeToon materials: 6. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 0 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: Fcl_ALL_Fun, Fcl_BRW_Fun.
- Gear: bow on leftHand, quiver on spine.

### Aika_V4_WineGuard

Wine uniform, black-red hair, red eyes, angry face, sword and shield. Tall.

- Height scale `1.06` on the prefab root (humanoid bones are not scaled).
- Parts off: —.
- New AnimeToon materials: 6. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 0 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: Fcl_ALL_Angry, Fcl_BRW_Angry.
- Gear: sword on rightHand, shield on leftHand.

### Akane_V1_PastelWitch

Petite pastel witch. Lavender clothes, pink hair, violet eyes, cheerful face, mage hat.

- Height scale `0.96` on the prefab root (humanoid bones are not scaled).
- Parts off: —.
- New AnimeToon materials: 7. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 33 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: Fcl_ALL_Fun, Fcl_BRW_Fun.
- Gear: hat on head.

### Akane_V2_InkDuelist

Black-and-red clothes, black hair, red eyes, angry face, sword.

- Height scale `1.0` on the prefab root (humanoid bones are not scaled).
- Parts off: —.
- New AnimeToon materials: 7. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 0 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: Fcl_ALL_Angry, Fcl_BRW_Angry.
- Gear: sword on rightHand.

### Akane_V3_SunnyArcher

Marigold clothes, honey hair, amber eyes, smile, bow and quiver. Tallest Akane variant.

- Height scale `1.08` on the prefab root (humanoid bones are not scaled).
- Parts off: —.
- New AnimeToon materials: 7. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 0 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: Fcl_ALL_Joy, Fcl_BRW_Joy.
- Gear: bow on leftHand, quiver on spine.

### Akane_V4_MossSquire

Moss clothes, brown hair, green eyes, sad face, shield.

- Height scale `1.02` on the prefab root (humanoid bones are not scaled).
- Parts off: —.
- New AnimeToon materials: 7. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 0 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: Fcl_ALL_Sorrow, Fcl_BRW_Sorrow.
- Gear: shield on leftHand.

### Cultist_V1_WhiteCantor

The robe is one mesh, so the silhouette stays. This one is a pale gold-white robe, honey hair, blue eyes, smile, half mask.

- Height scale `1.0` on the prefab root (humanoid bones are not scaled).
- Parts off: —.
- New AnimeToon materials: 9. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 0 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: Face.M_F00_000_00_Fcl_ALL_Joy, Fcl_ALL_Joy.
- Gear: mask on head.

### Cultist_V2_CrimsonRite

Crimson robe, black-red hair, red eyes, angry face, dagger. Slightly shorter.

- Height scale `0.96` on the prefab root (humanoid bones are not scaled).
- Parts off: —.
- New AnimeToon materials: 9. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 0 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: Face.M_F00_000_00_Fcl_ALL_Angry, Fcl_ALL_Angry.
- Gear: dagger on rightHand.

### Cultist_V3_VerdantOracle

Green robe, teal hair, green eyes, cheerful face, mage hat. Taller.

- Height scale `1.05` on the prefab root (humanoid bones are not scaled).
- Parts off: —.
- New AnimeToon materials: 9. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 0 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: Face.M_F00_000_00_Fcl_ALL_Fun, Fcl_ALL_Fun.
- Gear: hat on head.

### Cultist_V4_NightBow

Deep blue robe, silver-blue hair, blue eyes, sad face, bow and quiver.

- Height scale `1.0` on the prefab root (humanoid bones are not scaled).
- Parts off: —.
- New AnimeToon materials: 9. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 0 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: Face.M_F00_000_00_Fcl_ALL_Sorrow, Fcl_ALL_Sorrow.
- Gear: bow on leftHand, quiver on spine.

### StellarWitch_V1_UnrobedDuelist

Hat, robe and scarf are off, leaving blouse and drawers. Red hair, angry face has no blend shapes on this rig, sword.

- Height scale `1.02` on the prefab root (humanoid bones are not scaled).
- Parts off: hat, robe, scart.
- New AnimeToon materials: 15. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 0 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: —.
- Gear: sword on rightHand.
- Note: StellarWitch ships no blend shapes, so the expression field is stored but does not move the face.

### StellarWitch_V2_NightOracle

Full robe and hat, violet cloth, lilac hair, half mask. Hair bones slightly larger.

- Height scale `0.96` on the prefab root (humanoid bones are not scaled).
- Parts off: —.
- New AnimeToon materials: 18. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 21 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: —.
- Gear: mask on head.

### StellarWitch_V3_ScarflessArcher

Scarf off, hat kept, teal robe, teal hair, bow and quiver.

- Height scale `1.0` on the prefab root (humanoid bones are not scaled).
- Parts off: scart.
- New AnimeToon materials: 17. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 0 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: —.
- Gear: bow on leftHand, quiver on spine.

### StellarWitch_V4_TaillessKnight

Back tail and earring off. Gold-white robe, blonde hair, sword and shield. Tallest witch variant.

- Height scale `1.07` on the prefab root (humanoid bones are not scaled).
- Parts off: backtail, earling.
- New AnimeToon materials: 16. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 0 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: —.
- Gear: greatsword on rightHand, shield on leftHand.

### SanpakuChan_V1_PinkBlade

Pink clothes and hair, blue eyes, sword. Hair bones enlarged. The tail is not a separate renderer, so it cannot be toggled.

- Height scale `0.98` on the prefab root (humanoid bones are not scaled).
- Parts off: —.
- New AnimeToon materials: 3. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 20 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: —.
- Gear: sword on rightHand.
- Note: No blend shapes on this model. Tail mesh is part of the body.

### SanpakuChan_V2_BlueMage

Blue clothes, pale blue hair, mage hat.

- Height scale `1.0` on the prefab root (humanoid bones are not scaled).
- Parts off: —.
- New AnimeToon materials: 3. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 0 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: —.
- Gear: hat on head.

### SanpakuChan_V3_InkMask

Black-red clothes, dark hair, red eyes, half mask and dagger. Slightly taller.

- Height scale `1.04` on the prefab root (humanoid bones are not scaled).
- Parts off: —.
- New AnimeToon materials: 3. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 0 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: —.
- Gear: mask on head, dagger on rightHand.

### SanpakuChan_V4_SeaArcher

Sea-green clothes and hair, bow and quiver. Shortest Sanpaku variant.

- Height scale `0.94` on the prefab root (humanoid bones are not scaled).
- Parts off: —.
- New AnimeToon materials: 3. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 0 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: —.
- Gear: bow on leftHand, quiver on spine.

### Reika_V1_HatlessSnow

Holiday hat off. Pale ice dress, silver-blue hair, blue eyes, open mouth, sword. Shorter. This rig has no smile shape.

- Height scale `0.95` on the prefab root (humanoid bones are not scaled).
- Parts off: Hat_mesh.001.
- New AnimeToon materials: 4. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 0 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: Ahh.
- Gear: sword on rightHand.

### Reika_V2_CrimsonCarol

Hat stays. Crimson dress, dark red hair, red eyes, raised brows, dagger.

- Height scale `1.0` on the prefab root (humanoid bones are not scaled).
- Parts off: —.
- New AnimeToon materials: 5. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 0 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: eyeBrow Height.
- Gear: dagger on rightHand.

### Reika_V3_HollyArcher

Hat off. Holly-green dress, brown-green hair, green eyes, a wider open mouth, bow and quiver. Tallest Reika variant.

- Height scale `1.06` on the prefab root (humanoid bones are not scaled).
- Parts off: Hat_mesh.001.
- New AnimeToon materials: 4. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 0 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: Big Ahh.
- Gear: bow on leftHand, quiver on spine.

### Reika_V4_MidnightBell

Hat stays. Midnight violet dress, violet hair, violet eyes, sad face, half mask.

- Height scale `1.02` on the prefab root (humanoid bones are not scaled).
- Parts off: —.
- New AnimeToon materials: 5. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 0 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: Sad.
- Gear: mask on head.

### Fina_V1_CoralSprinter

Back hair off for a shorter silhouette. Coral top, warm hair, blue eyes, smile, sword.

- Height scale `1.0` on the prefab root (humanoid bones are not scaled).
- Parts off: HairBack.
- New AnimeToon materials: 13. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 0 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: Fcl_BRW_Joy, Fcl_EYE_Joy.
- Gear: sword on rightHand.
- Note: Hiding HairBack assumes the front and mid hair still cover the scalp. Confirm in Unity.

### Fina_V2_BobDuelist

Back and mid hair off, leaving the front hair. Blue clothes, cool hair, angry face, dagger. Shorter.

- Height scale `0.96` on the prefab root (humanoid bones are not scaled).
- Parts off: HairBack, HairMid.
- New AnimeToon materials: 12. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 0 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: Fcl_BRW_Angry, Fcl_EYE_Angry.
- Gear: dagger on rightHand.
- Note: A bob only if HairFront covers the crown. This is unverified without a render.

### Fina_V3_FullHairArcher

All three hair pieces stay. Green street clothes, green-brown hair, bow and quiver. Taller.

- Height scale `1.05` on the prefab root (humanoid bones are not scaled).
- Parts off: —.
- New AnimeToon materials: 14. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 13 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: Fcl_BRW_Fun, Fcl_EYE_Fun.
- Gear: bow on leftHand, quiver on spine.

### Fina_V4_SleevelessKnight

The top mesh named topic is off. Gold accents, sword-length greatsword and shield, angry face.

- Height scale `1.03` on the prefab root (humanoid bones are not scaled).
- Parts off: topic.
- New AnimeToon materials: 13. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 0 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: Fcl_BRW_Angry, Fcl_EYE_Angry.
- Gear: greatsword on rightHand, shield on leftHand.
- Note: topic is the only top-like renderer. If it is not the outer shirt, the torso underneath still shows.

### SapphiArtchan_V1_ClassRibbon

Chequer collar and back-skirt tiles off. Rose uniform, warm hair, blue eyes, smile, sword. Rig is Generic, so humanoid retargets will not play.

- Height scale `1.0` on the prefab root (humanoid bones are not scaled).
- Parts off: Colar_Tile, BackSkirt_Tile.
- New AnimeToon materials: 26. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 0 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: face_smile_BS, Face_Blendshape.face_smile_BS.
- Gear: sword on rightHand.
- Note: Generic rig. Variants change looks only; they do not add a humanoid avatar.

### SapphiArtchan_V2_MechKnight

Skirts off so the mech legs and boots carry the silhouette. Blue-and-gold, sword and shield. Tallest.

- Height scale `1.06` on the prefab root (humanoid bones are not scaled).
- Parts off: Skirt, Skirt_Inside, BackSkirt_Inside, BackSkirt_Tile.
- New AnimeToon materials: 24. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 0 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: brow_anger_L, Brow_BS.brow_anger_L.
- Gear: sword on rightHand, shield on leftHand.

### SapphiArtchan_V3_SkirtMage

Mech covers and outer mech shells off, skirts stay. Orchid clothes, pink hair, mage hat. Shorter.

- Height scale `0.97` on the prefab root (humanoid bones are not scaled).
- Parts off: Mech_Cover_L, Mech_Cover_R, Mech_Outer_L, Mech_Outer_R.
- New AnimeToon materials: 24. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 18 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: brow_Up_L, Brow_BS.brow_Up_L.
- Gear: hat on head.

### SapphiArtchan_V4_LiteArcher

Tile pieces off. Sea-green clothes, bow and quiver. Shortest Sapphi variant.

- Height scale `0.94` on the prefab root (humanoid bones are not scaled).
- Parts off: Colar_Tile, BackSkirt_Tile.
- New AnimeToon materials: 26. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 0 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: face_sad_BS, Face_Blendshape.face_sad_BS.
- Gear: bow on leftHand, quiver on spine.

### Muryotaisu_V1_HaloKnight

Coat and halo stay. The halo bone is scaled up. Gold-white coat, blonde hair, amber eyes, smile, sword.

- Height scale `1.0` on the prefab root (humanoid bones are not scaled).
- Parts off: —.
- New AnimeToon materials: 6. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 0 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: Smile.
- Gear: sword on rightHand.

### Muryotaisu_V2_CoatlessDuelist

Coat off. The body renderer remains. Black-red hair, red eyes, troubled face, dagger. Shorter. This rig has no angry shape.

- Height scale `0.95` on the prefab root (humanoid bones are not scaled).
- Parts off: Coat.
- New AnimeToon materials: 5. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 0 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: Trouble.
- Gear: dagger on rightHand.
- Note: Coat off relies on the separate Body mesh. Confirm the torso reads as dressed.

### Muryotaisu_V3_RinglessMage

Halo off. Violet coat and hair, mage hat, mouth open. Tallest Muryotaisu variant.

- Height scale `1.05` on the prefab root (humanoid bones are not scaled).
- Parts off: AngelRing.
- New AnimeToon materials: 5. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 0 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: MouthOpen.
- Gear: hat on head.

### Muryotaisu_V4_GroveArcher

Green coat, green-brown hair, green eyes, eyes closed, bow and quiver. Halo stays at normal size. This rig has no sad shape.

- Height scale `1.02` on the prefab root (humanoid bones are not scaled).
- Parts off: —.
- New AnimeToon materials: 6. Textures are hue-set or skin-toned copies at 256px or smaller, plus an RGBA tint mask (R skin, G hair, B primary, A secondary).
- Safe bone scales applied: 0 transforms (hair, ribbon, tail, hat, bag, halo and similar names only).
- Expression: EyeClose.
- Gear: bow on leftHand, quiver on spine.

## What was not verified

Unity was not opened. These files were not imported, and the models were not rendered. Headless mesh renders were not produced: this environment has no Blender/FBX viewport, so there is no front-view beauty render. The contact sheets show the edited textures and the toggle list only.

Risks to check on first import:

- Prefab-variant YAML is hand-authored. If Unity drops an override, run the rebuild menu.
- Accessory position and rotation will need a pass in the editor. The component only guarantees the parent bone.
- Casual1 hair swaps reuse the Hair renderer's bone list. Hair 1–5 come from the same armature; if a mesh's bone order differs, the hair will deform until the rebuild menu reassigns the mesh in Unity.
- Fina hair and top toggles, and Muryotaisu with the coat off, can show gaps. The sheets call those out.
- Scaling hair bones changes the silhouette and does not change the humanoid avatar. Root scale changes world height for everyone parented to the prefab, including hitboxes that live on that root.
- PicoChan stays on the heavy base mesh. These variants do not decimate it.
- SapphiArtchan remains Generic. Looks change; retargeting does not start working.

