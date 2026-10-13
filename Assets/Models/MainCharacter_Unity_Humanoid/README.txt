MAIN CHARACTER (HUMANOID) - UNITY DELIVERY
==========================================
Contents: Main_Character.fbx (mesh, Humanoid-ready rig, animations, sword) + Textures/ + Editor/CharacterMaterialSetup.cs

IMPORT (nothing to assign by hand)
1. Unzip and copy the whole "MainCharacter_Unity_Humanoid" folder into your project's Assets folder.
2. Wait for Unity to finish importing/compiling. The included editor script automatically:
   - creates the materials (Materials folder) and assigns Albedo + Normal textures (Built-in, URP and HDRP supported)
   - sets the rig to Humanoid (Avatar created from this model; bones are named with the standard
     Hips / Spine / LeftUpLeg / LeftLeg / LeftFoot / LeftArm ... names so the mapping is complete)
3. If it did not run: menu  Tools > Main Character > Setup Materials.  (Or set Rig > Animation Type = Humanoid by hand.)
4. Drag Main_Character.fbx into the scene.

NOTES
- Scale: 1 unit = 1 m (chibi character, ~1.3 m tall), pivot at the feet, faces +Z.
- Bone hierarchy: Hips > Spine > Spine1 > Spine2 > Neck > Neck1 > Head; legs Hips > UpLeg > Leg > Foot > ToeBase;
  arms Spine2 > Shoulder > Arm > ForeArm > Hand (+ fingers). The sword is attached to RightHand.
- Animation clips (30 fps): attack, die, idle, run
- Parts of the body that had no material in the source (M_DefaultWhite) are plain white on purpose.
