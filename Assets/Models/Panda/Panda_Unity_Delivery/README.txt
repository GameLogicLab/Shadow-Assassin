PANDA CHARACTER - UNITY DELIVERY
================================
Contents: Panda_Character.fbx (mesh, rig, animations) + Textures/ + Editor/PandaMaterialSetup.cs

IMPORT (nothing to assign by hand)
1. Unzip and copy the whole "Panda_Unity_Delivery" folder into your project's Assets folder.
2. Wait for Unity to finish importing/compiling. The included editor script automatically creates the
   materials (Assets/.../Materials), assigns Albedo + Normal textures, and applies them to the FBX.
   Works with Built-in, URP and HDRP (it detects the pipeline).
3. If it did not run: menu  Tools > Panda > Setup Materials.
4. Drag Panda_Character.fbx into the scene.

NOTES
- Scale: 1 unit = 1 m, ~1.8 m tall, pivot at feet, faces +Z.
- Rig: Generic (73 bones). Dagger is attached to bone hand.R.
- Animation clips (30 fps): attack, idle.001, nota, nota.004, run.001
- Normal maps are set to "Normal map" type automatically by the script.
- Hair and one body material use flat colors (no texture) on purpose.
