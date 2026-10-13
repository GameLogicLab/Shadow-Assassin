#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace PandaDelivery
{
    public class PandaMatSpec
    {
        public string name, baseTex, normalTex; public Color color; public float smoothness;
        public PandaMatSpec(string n, string b, string nm, Color c, float s) { name = n; baseTex = b; normalTex = nm; color = c; smoothness = s; }
    }

    [InitializeOnLoad]
    public static class PandaMaterialSetup
    {
        static bool scheduled;
        static readonly PandaMatSpec[] Specs = new PandaMatSpec[]
        {
            new PandaMatSpec("M_Panda_Body", "Panda_Body_BaseColor", "Panda_Body_Normal", Color.white, 0.506f),
            new PandaMatSpec("M_Panda_BodyFlat", null, "Panda_Body_Normal", new Color(0.906f, 0.906f, 0.906f, 1f), 0.506f),
            new PandaMatSpec("M_Panda_Cloth", "Panda_Cloth_BaseColor", "Panda_Cloth_Normal", Color.white, 0.544f),
            new PandaMatSpec("M_Panda_Cloth2", "Panda_Cloth2_BaseColor", "Panda_Cloth2_Normal", Color.white, 0.239f),
            new PandaMatSpec("M_Panda_Hair", null, null, new Color(0.15f, 0.15f, 0.15f, 1f), 0.2f),
            new PandaMatSpec("M_Weapon_Base1", "Weapon_Base1", null, Color.white, 0.4f),
            new PandaMatSpec("M_Weapon_Outline1", "Weapon_Outline1", null, Color.white, 0.4f),
            new PandaMatSpec("M_Weapon_Base2", "Weapon_Base2", null, Color.white, 0.4f),
            new PandaMatSpec("M_Weapon_Outline2", "Weapon_Outline2", null, Color.white, 0.4f),
        };

        static PandaMaterialSetup() { Schedule(); }

        public static void Schedule()
        {
            if (scheduled) return;
            scheduled = true;
            EditorApplication.delayCall += () => { scheduled = false; Run(false); };
        }

        [MenuItem("Tools/Panda/Setup Materials")]
        static void Manual() { Run(true); }

        static string FindFbx()
        {
            foreach (string guid in AssetDatabase.FindAssets("Panda_Character"))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (p.EndsWith(".fbx")) return p;
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
            if (fbx == null) { if (verbose) Debug.LogWarning("Panda_Character.fbx not found in project."); return; }
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
                if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
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
            if (need)
            {
                importer.SaveAndReimport();
                Debug.Log("[Panda] Materials created in " + matDir + " and assigned to Panda_Character.fbx");
            }
            else if (verbose) Debug.Log("[Panda] Materials already set up.");
        }
    }

    class PandaPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            foreach (string p in imported)
                if (p.Contains("Panda_Character") || p.Contains("/Textures/Panda_") || p.Contains("/Textures/Weapon_"))
                { PandaMaterialSetup.Schedule(); return; }
        }
    }
}
#endif
