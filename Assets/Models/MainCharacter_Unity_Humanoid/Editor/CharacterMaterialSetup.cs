#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CharacterDelivery
{
    public class MatSpec
    {
        public string name, baseTex, normalTex; public Color color; public float metallic, smoothness;
        public MatSpec(string n, string b, string nm, Color c, float m, float s) { name = n; baseTex = b; normalTex = nm; color = c; metallic = m; smoothness = s; }
    }

    [InitializeOnLoad]
    public static class CharacterMaterialSetup
    {
        const string FbxName = "Main_Character";
        static bool scheduled;
        static readonly MatSpec[] Specs = new MatSpec[]
        {
            new MatSpec("M_Chenille", "Chenille_BaseColor", "Chenille_Normal", new Color(1.000f, 1.000f, 1.000f, 1f), 0.520f, 0.363f),
            new MatSpec("M_FabricBlack", "FabricBlack_BaseColor", "FabricBlack_Normal", new Color(1.000f, 1.000f, 1.000f, 1f), 0.200f, 0.385f),
            new MatSpec("M_Hair", "Hair_BaseColor", null, new Color(1.000f, 1.000f, 1.000f, 1f), 0.000f, 0.500f),
            new MatSpec("M_MaskFlat", null, null, new Color(0.118f, 0.118f, 0.118f, 1f), 0.000f, 0.250f),
            new MatSpec("M_FlatB", null, null, new Color(0.051f, 0.031f, 0.020f, 1f), 0.000f, 0.500f),
            new MatSpec("M_DefaultWhite", null, null, new Color(0.906f, 0.906f, 0.906f, 1f), 0.000f, 0.500f),
            new MatSpec("M_Waffle", "Waffle_BaseColor", "Waffle_Normal", new Color(1.000f, 1.000f, 1.000f, 1f), 0.081f, 0.304f),
            new MatSpec("M_YellowMetal", "YellowMetal_BaseColor", "YellowMetal_Normal", new Color(1.000f, 1.000f, 1.000f, 1f), 0.000f, 0.769f),
            new MatSpec("M_YellowFabric", "YellowFabric_BaseColor", "YellowFabric_Normal", new Color(1.000f, 1.000f, 1.000f, 1f), 0.000f, 0.387f),
            new MatSpec("M_Shoe", "Shoe_BaseColor", null, new Color(1.000f, 1.000f, 1.000f, 1f), 0.255f, 0.000f),
            new MatSpec("M_Skin", "Skin_BaseColor", "Skin_Normal", new Color(1.000f, 1.000f, 1.000f, 1f), 0.000f, 0.600f),
            new MatSpec("M_Eye", "Eye_BaseColor", "Eye_Normal", new Color(1.000f, 1.000f, 1.000f, 1f), 0.000f, 0.600f),
            new MatSpec("M_Plaster", "Plaster_BaseColor", null, new Color(1.000f, 1.000f, 1.000f, 1f), 0.000f, 0.385f),
            new MatSpec("M_Chrome", "Chrome_BaseColor", "Chrome_Normal", new Color(1.000f, 1.000f, 1.000f, 1f), 1.000f, 0.954f),
            new MatSpec("M_BlackMetal", "BlackMetal_BaseColor", "BlackMetal_Normal", new Color(1.000f, 1.000f, 1.000f, 1f), 1.000f, 0.746f),
            new MatSpec("M_Wood", "Wood_BaseColor", "Wood_Normal", new Color(1.000f, 1.000f, 1.000f, 1f), 0.868f, 0.259f)
        };

        static CharacterMaterialSetup() { Schedule(); }

        public static void Schedule()
        {
            if (scheduled) return;
            scheduled = true;
            EditorApplication.delayCall += () => { scheduled = false; Run(false); };
        }

        [MenuItem("Tools/Main Character/Setup Materials")]
        static void Manual() { Run(true); }

        static string FindFbx()
        {
            foreach (string guid in AssetDatabase.FindAssets(FbxName))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (p.EndsWith("/" + FbxName + ".fbx")) return p;
            }
            return null;
        }

        static Texture2D LoadTex(string dir, string name)
        {
            return string.IsNullOrEmpty(name) ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "/" + name + ".jpg");
        }

        static int Pipeline()
        {
            var rp = GraphicsSettings.currentRenderPipeline;
            if (rp == null) return 0;
            return rp.GetType().Name.Contains("HD") ? 2 : 1;
        }

        public static void Run(bool verbose)
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) { Schedule(); return; }
            string fbx = FindFbx();
            if (fbx == null) { if (verbose) Debug.LogWarning("Main_Character.fbx not found in project."); return; }
            string dir = Path.GetDirectoryName(fbx).Replace('\\', '/');
            string texDir = dir + "/Textures", matDir = dir + "/Materials";
            var importer = AssetImporter.GetAtPath(fbx) as ModelImporter;
            if (importer == null) return;

            foreach (var s in Specs)
            {
                if (!string.IsNullOrEmpty(s.baseTex) && LoadTex(texDir, s.baseTex) == null) { if (verbose) Debug.LogWarning("Texture missing: " + s.baseTex); return; }
                if (!string.IsNullOrEmpty(s.normalTex) && LoadTex(texDir, s.normalTex) == null) { if (verbose) Debug.LogWarning("Texture missing: " + s.normalTex); return; }
            }
            foreach (var s in Specs)
            {
                if (string.IsNullOrEmpty(s.normalTex)) continue;
                var ti = AssetImporter.GetAtPath(texDir + "/" + s.normalTex + ".jpg") as TextureImporter;
                if (ti != null && ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; ti.SaveAndReimport(); }
            }

            int pipe = Pipeline();
            Shader shader = pipe == 1 ? Shader.Find("Universal Render Pipeline/Lit") : pipe == 2 ? Shader.Find("HDRP/Lit") : Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Standard");
            string baseProp = pipe == 1 ? "_BaseMap" : pipe == 2 ? "_BaseColorMap" : "_MainTex";
            string colProp = pipe == 0 ? "_Color" : "_BaseColor";
            string nrmProp = pipe == 2 ? "_NormalMap" : "_BumpMap";
            string smProp = pipe == 0 ? "_Glossiness" : "_Smoothness";

            if (!AssetDatabase.IsValidFolder(matDir)) AssetDatabase.CreateFolder(dir, "Materials");
            foreach (var s in Specs)
            {
                string path = matDir + "/" + s.name + ".mat";
                Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
                else if (m.shader != shader) m.shader = shader;
                if (m.HasProperty(colProp)) m.SetColor(colProp, s.color);
                if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", s.metallic);
                if (m.HasProperty(smProp)) m.SetFloat(smProp, s.smoothness);
                if (m.HasProperty(baseProp)) m.SetTexture(baseProp, LoadTex(texDir, s.baseTex));
                if (!string.IsNullOrEmpty(s.normalTex) && m.HasProperty(nrmProp))
                {
                    m.SetTexture(nrmProp, LoadTex(texDir, s.normalTex));
                    m.EnableKeyword("_NORMALMAP");
                    if (pipe == 2) m.EnableKeyword("_NORMALMAP_TANGENT_SPACE");
                }
                EditorUtility.SetDirty(m);
            }
            AssetDatabase.SaveAssets();

            var map = importer.GetExternalObjectMap();
            bool need = false;
            foreach (var s in Specs)
            {
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(matDir + "/" + s.name + ".mat");
                var id = new AssetImporter.SourceAssetIdentifier(typeof(Material), s.name);
                if (!map.ContainsKey(id) || map[id] != mat) { importer.AddRemap(id, mat); need = true; }
            }

            // Humanoid rig: set once (so you can still switch it manually afterwards)
            string key = "MainCharacterHumanoidSet_" + AssetDatabase.AssetPathToGUID(fbx);
            if (!EditorPrefs.GetBool(key, false))
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                EditorPrefs.SetBool(key, true);
                need = true;
            }

            if (need)
            {
                importer.SaveAndReimport();
                Debug.Log("[MainCharacter] Materials created in " + matDir + ", assigned to Main_Character.fbx, rig set to Humanoid.");
            }
            else if (verbose) Debug.Log("[MainCharacter] Already set up.");
        }
    }

    class CharacterPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            foreach (string p in imported)
                if (p.Contains("Main_Character") || p.Contains("/Textures/"))
                { CharacterMaterialSetup.Schedule(); return; }
        }
    }
}
#endif
