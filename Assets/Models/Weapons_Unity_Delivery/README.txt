WEAPONS - UNITY DELIVERY
========================
Contents: Weapon_Sword.fbx, Weapon_Dagger.fbx, Weapon_Gem.fbx + Textures/ + Editor/WeaponMaterialSetup.cs

IMPORT (nothing to assign by hand)
1. Unzip and copy the whole "Weapons_Unity_Delivery" folder into your project's Assets folder.
2. Wait for Unity to finish importing/compiling. The included editor script automatically creates the materials
   (Materials folder), assigns Albedo + Normal textures, sets Metallic/Smoothness and applies them to each FBX.
   Works with Built-in, URP and HDRP (it detects the pipeline).
3. If it did not run: menu  Tools > Weapons > Setup Materials.

NOTES
- Each weapon is a separate FBX (pivot at the center of the weapon, 1 unit = 1 m, no rig/animation).
- Weapon_Sword: 5 parts (handle x2, guard, black blade, chrome blade). Weapon_Dagger: 2 parts (blade + outline shell,
  keep backface culling ON). Weapon_Gem: green gem.
- Attach to the character's hand bone as a child and adjust position/rotation.
