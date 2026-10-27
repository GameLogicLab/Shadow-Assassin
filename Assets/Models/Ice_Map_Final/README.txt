ICE MAP - UNITY HANDOFF
=======================
Files
  ice_map.fbx  : 13 meshes (final, modifiers/geometry-nodes already applied). No skeleton/animation.
  Textures/    : all textures (34 PNG files). File names match what the FBX references.

How to use
  1. Copy this whole folder into Unity's Assets.
  2. Base colour + Normal maps are linked by file name. If a material stays grey: select ice_map.fbx > Materials tab > Extract Materials, then assign the maps listed below.
  3. Normal maps are OpenGL (Y+), Unity default. Set Texture Type = Normal map for every *_normal / *_Normal file.
  4. Smoothness in Unity = 1 - roughness. Use the *_roughness / *_Roughness files (invert) or set Smoothness manually.
  5. Add a Directional Light, otherwise the scene looks black.

Objects -> material -> textures
  Cube.001_Final   @ [-6.1, 0.0, 0.0]  | Cube001_Unity_Final        | Base: cube_basecolor.png | Normal: cube_normal.png | Roughness: cube_roughness.png | Metallic 0.0
  Cube.009_Final   @ [7.3, 0.0, 0.0]  | Cube009_Unity_Final.002    | Base: purple_base.png | Normal: purple_normal.png | Roughness: purple_roughness.png | Metallic 0.0
  Cube.010_Final   @ [7.2, -7.8, 0.0]  | Cube010_Unity_Final        | Base: zig_zag.png | Normal: zig_zag_normal.png | Roughness: zig_zag_roughness.png | Metallic 0.0
  Cube.012_Final   @ [7.2, -7.8, 0.0]  | Cube009_Unity_Final.001    | Base: base_strip_color.png | Normal: base_strip_normal.png | Roughness: base_strip_roughness.png | Metallic 0.0
  Cube.013_Final   @ [0.0, -8.8, 0.0]  | Cube013_Unity_Final        | Base: bit_ice.png | Normal: bit_ice_normal.png | Roughness: bit_ice_roughness.png | Metallic 0.0
  Cube.014_Final   @ [-6.4, -8.8, 0.0]  | Cube014_Unity_Final        | Base: flat colour [0.39, 0.69, 0.87] (linear) | Normal: glass_normal.png | Roughness: glass_roughness.png | Metallic 0.0 | Alpha 0.5 (transparent glass)
  Cube.015_Final   @ [11.5, 2.3, 0.0]  | Cube015_Unity_Final        | Base: green_basecolor.png | Normal: green_normal.png | Roughness: green_roughness.png | Metallic 0.0
  Cube.016_Final   @ [15.0, 2.3, 0.0]  | Cube016_Unity_Final        | Base: gold_basecolor.png | Normal: gold_normal.png | Roughness: gold_roughness.png | Metallic 0.0
  Cube.017_Final   @ [0.0, 0.0, 0.0]  | Cube017_Unity_Final        | Base: ice_floor_BaseColor.png | Normal: ice_floor_Normal.png | Roughness: ice_floor_Roughness.png | Metallic 0.0
  Cube.019_Final   @ [-6.4, -15.1, 0.0]  | Cube019_Unity_Final        | Base: yellow.png | Normal: yellow_normal.png | Roughness: yellow_roughness.png | Metallic 0.0
  Cube.021_Final   @ [0.0, 0.0, 0.0]  | Cube021_Unity_Final        | Base: ice_blue.png | Normal: ice_blue_normal.png | Roughness: ice_blue_roughness.png | Metallic 0.0
  Cube.022_Final   @ [0.0, -15.0, 0.0]  | Cube022_Unity_Final        | Base: pink_basecolor.png | Normal: - | Roughness: pink_roughness.png | Metallic 0.0
  Cube_Final       @ [0.0, 0.0, 0.0]  | Cube_Unity_Final           | Base: cube_basecolor.png | Normal: cube_normal.png | Roughness: cube_roughness.png | Metallic 0.0

Notes
  - Cube.014_Final is glass: blue colour with Alpha 0.5 -> set the Unity material to Transparent/Fade. Textures: glass_normal.png, glass_roughness.png.
  - Cube.022_Final (pink) has no normal map. Cube.015_Final (green) and Cube.016_Final (gold) use flat colour textures; their normal maps are only a very subtle stripe bump.
  - Some normal maps are almost flat (baked from smooth surfaces); that is expected.
  - Yellow base colour texture (yellow.png) is 2048 px, all others 1024 px.
