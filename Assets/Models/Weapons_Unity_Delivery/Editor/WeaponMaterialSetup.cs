#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace WeaponDelivery
{
    public class MatSpec
    {
        public string name, baseTex, normalTex; public float metallic, smoothness;
        public MatSpec(string n, string b, string nm, float m, float s) { name = n; baseTex = b; normalTex = nm; metallic = m; smoothness = s; }
    }
    public class FbxSpec
    {
        public string fbx; public MatSpec[] mats;
        public FbxSpec(string f, MatSpec[] m) { fbx = f; mats = m; }
    }

    [InitializeOnLoad]
    public static class WeaponMaterialSetup
    {
        static bool scheduled;
        static readonly FbxSpec[] Specs = new FbxSpec[]
        {
            new FbxSpec("Weapon_Sword", new MatSpec[] {
                new MatSpec("M_Wood", "Wood_BaseColor", "Wood_Normal", 0.868f, 0.259f),
                new MatSpec("M_Plaster", "Plaster_BaseColor", "Plaster_Normal", 0.0f, 0.385f),
                new MatSpec("M_BlackMetal", "BlackMetal_BaseColor", "BlackMetal_Normal", 1.0f, 0.746f),
                new MatSpec("M_Chrome", "Chrome_BaseColor", "Chrome_Normal", 1.0f, 0.954f)
            }),
            new FbxSpec("Weapon_Dagger", new MatSpec[] {
                new MatSpec("M_Dagger_BaseA", "Dagger_BaseA", null, 0.0f, 0.4f),
                new MatSpec("M_Dagger_OutlineA", "Dagger_OutlineA", null, 0.0f, 0.4f),
                new MatSpec("M_Dagger_BaseB", "Dagger_BaseB", null, 0.0f, 0.4f),
                new MatSpec("M_Dagger_OutlineB", "Dagger_OutlineB", null, 0.0f, 0.4f)
            }),
            new FbxSpec("Weapon_Gem", new MatSpec[] {
                new MatSpec("M_Gem", "Gem_BaseColor", null, 0.2f, 0.9f)
            })
        };

        static WeaponMaterialSetup() { Schedule(); }

        public static void Schedule()
        {
            if (scheduled) return;
            scheduled = true;
            EditorApplication.delayCall += () => { scheduled = false; RunAll(false); };
        }

        [MenuItem("Tools/Weapons/Setup Materials")]
        static void Manual() { RunAll(true); }

        static void RunAll(bool verbose)
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) { Schedule(); return; }
            foreach (var f in Specs) Run(f, verbose);
        }

        static string FindFbx(string name)
        {
            foreach (string guid in AssetDatabase.FindAssets(name))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (p.EndsWith("/" + name + ".fbx")) return p;
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

        static void Run(FbxSpec spec, bool verbose)
        {
            string fbx = FindFbx(spec.fbx);
            if (fbx == null) return;
            string dir = Path.GetDirectoryName(fbx).Replace('\\', '/');
            string texDir = dir + "/Textures", matDir = dir + "/Materials";
            var importer = AssetImporter.GetAtPath(fbx) as ModelImporter;
            if (importer == null) return;

            foreach (var s in spec.mats)
            {
                if (!string.IsNullOrEmpty(s.baseTex) && LoadTex(texDir, s.baseTex) == null) { if (verbose) Debug.LogWarning("Texture missing: " + s.baseTex); return; }
                if (!string.IsNullOrEmpty(s.normalTex) && LoadTex(texDir, s.normalTex) == null) { if (verbose) Debug.LogWarning("Texture missing: " + s.normalTex); return; }
            }
            foreach (var s in spec.mats)
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
            foreach (var s in spec.mats)
            {
                string path = matDir + "/" + s.name + ".mat";
                Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
                else if (m.shader != shader) m.shader = shader;
                if (m.HasProperty(colProp)) m.SetColor(colProp, Color.white);
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
            foreach (var s in spec.mats)
            {
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(matDir + "/" + s.name + ".mat");
                var id = new AssetImporter.SourceAssetIdentifier(typeof(Material), s.name);
                if (!map.ContainsKey(id) || map[id] != mat) { importer.AddRemap(id, mat); need = true; }
            }
            if (need)
            {
                importer.SaveAndReimport();
                Debug.Log("[Weapons] Materials created in " + matDir + " and assigned to " + spec.fbx + ".fbx");
            }
            else if (verbose) Debug.Log("[Weapons] " + spec.fbx + ": materials already set up.");
        }
    }

    class WeaponPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            foreach (string p in imported)
                if (p.Contains("/Weapon_") || p.Contains("/Textures/"))
                { WeaponMaterialSetup.Schedule(); return; }
        }
    }
}
#endif
