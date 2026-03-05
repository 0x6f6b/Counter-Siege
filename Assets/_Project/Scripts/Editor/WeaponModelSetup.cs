using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace CounterSiege
{
    public static class WeaponModelSetup
    {
        struct TextureEntry
        {
            public string path;
            public string name;
            public Texture2D tex;
        }

        struct WeaponModelInfo
        {
            public string folderName;
            public string modelFile; // FBX or GLB filename (without extension for asset loading)
            public string weaponDataAsset; // name of the .asset file
            public Vector3 defaultScale;
            public Vector3 defaultPosition;
            public Vector3 defaultRotation;
        }

        static readonly WeaponModelInfo[] weapons = new[]
        {
            new WeaponModelInfo
            {
                folderName = "Glock",
                modelFile = "Glock17Gen5_model",
                weaponDataAsset = "Glock",
                defaultScale = new Vector3(1.5351f, 1.5351f, 1.5351f),
                defaultPosition = new Vector3(0.0211f, -0.08f, 0.1686f),
                defaultRotation = new Vector3(0f, 90.1f, 0f)
            },
            new WeaponModelInfo
            {
                folderName = "USP",
                modelFile = "USPS",
                weaponDataAsset = "USP",
                defaultScale = new Vector3(1.0596f, 1.0596f, 1.0596f),
                defaultPosition = new Vector3(-0.0118f, -0.08f, -0.0783f),
                defaultRotation = new Vector3(359f, 358.3f, 0f)
            },
            new WeaponModelInfo
            {
                folderName = "AK47",
                modelFile = "AK47_CS2",
                weaponDataAsset = "AK47",
                defaultScale = new Vector3(1.7741f, 1.7741f, 1.7741f),
                defaultPosition = new Vector3(0.0955f, -0.15f, 0.1361f),
                defaultRotation = new Vector3(5.1f, 2.3f, 0f)
            },
            new WeaponModelInfo
            {
                folderName = "AWP",
                modelFile = "AWP_CS2",
                weaponDataAsset = "AWP",
                defaultScale = new Vector3(1.0265f, 1.0265f, 1.0265f),
                defaultPosition = new Vector3(-0.0837f, -0.15f, -0.2812f),
                defaultRotation = new Vector3(1f, 1.1f, 0f)
            },
            new WeaponModelInfo
            {
                folderName = "M4A4",
                modelFile = "M4A4_CS2",
                weaponDataAsset = "M4A4",
                defaultScale = new Vector3(1.0819f, 1.0819f, 1.0819f),
                defaultPosition = new Vector3(-0.0386f, -0.3073f, -0.4918f),
                defaultRotation = new Vector3(359.3f, 359f, 0f)
            },
        };

        [MenuItem("Counter Siege/Setup/Create Weapon Model Prefabs")]
        public static void CreateWeaponModelPrefabs()
        {
            string prefabDir = "Assets/_Project/Prefabs/Weapons";
            if (!AssetDatabase.IsValidFolder(prefabDir))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs"))
                    AssetDatabase.CreateFolder("Assets/_Project", "Prefabs");
                AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "Weapons");
            }

            int created = 0;
            foreach (var info in weapons)
            {
                string modelFolder = $"Assets/_Project/Models/Weapons/{info.folderName}";
                if (!AssetDatabase.IsValidFolder(modelFolder))
                {
                    Debug.LogWarning($"[WeaponModelSetup] Folder not found: {modelFolder}, skipping {info.folderName}");
                    continue;
                }

                // Find the model asset
                GameObject modelAsset = FindModelAsset(modelFolder, info.modelFile);
                if (modelAsset == null)
                {
                    Debug.LogWarning($"[WeaponModelSetup] Model not found for {info.folderName} in {modelFolder}");
                    continue;
                }

                // Create materials
                string matDir = $"{modelFolder}/Materials";
                if (!AssetDatabase.IsValidFolder(matDir))
                    AssetDatabase.CreateFolder(modelFolder, "Materials");

                var materials = CreateMaterials(modelFolder, matDir, info.folderName);

                // Create wrapper prefab
                var wrapper = new GameObject($"{info.folderName}_ViewModel");
                var modelInstance = Object.Instantiate(modelAsset);
                modelInstance.name = "Model";
                modelInstance.transform.SetParent(wrapper.transform);
                modelInstance.transform.localPosition = Vector3.zero;
                modelInstance.transform.localRotation = Quaternion.identity;
                modelInstance.transform.localScale = Vector3.one;

                // Set default transform on wrapper
                wrapper.transform.localPosition = info.defaultPosition;
                wrapper.transform.localRotation = Quaternion.Euler(info.defaultRotation);
                wrapper.transform.localScale = info.defaultScale;

                // Set layer recursively
                SetLayerRecursive(wrapper, 7); // WeaponViewModel layer

                // Remove all colliders
                foreach (var col in wrapper.GetComponentsInChildren<Collider>(true))
                    Object.DestroyImmediate(col);

                // Assign materials to renderers
                if (materials.Length > 0)
                {
                    var renderers = wrapper.GetComponentsInChildren<Renderer>(true);
                    foreach (var renderer in renderers)
                    {
                        var mats = renderer.sharedMaterials;
                        // Try to match renderer to a specific material by name
                        Material bestMatch = materials[0]; // default to primary
                        if (materials.Length > 1)
                        {
                            string rName = renderer.gameObject.name.ToLower();
                            for (int m = 1; m < materials.Length; m++)
                            {
                                string matName = materials[m].name.ToLower();
                                // Match part name (e.g. "barrel" in mat name vs renderer name)
                                foreach (string part in new[] { "barrel", "mag", "slide", "scope", "stock" })
                                {
                                    if (matName.Contains(part) && rName.Contains(part))
                                    {
                                        bestMatch = materials[m];
                                        break;
                                    }
                                }
                            }
                        }
                        for (int i = 0; i < mats.Length; i++)
                            mats[i] = bestMatch;
                        renderer.sharedMaterials = mats;
                    }
                }

                // Save prefab
                string prefabPath = $"{prefabDir}/{info.folderName}_ViewModel.prefab";
                PrefabUtility.SaveAsPrefabAsset(wrapper, prefabPath);
                Object.DestroyImmediate(wrapper);

                // Assign to WeaponData
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                AssignToWeaponData(info.weaponDataAsset, prefab);

                created++;
                Debug.Log($"[WeaponModelSetup] Created {prefabPath}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[WeaponModelSetup] Done! Created {created} weapon model prefabs.");
        }

        static GameObject FindModelAsset(string folder, string modelName)
        {
            // Try FBX first, then GLB
            string[] extensions = { ".fbx", ".glb", ".FBX", ".GLB" };
            foreach (var ext in extensions)
            {
                string path = $"{folder}/{modelName}{ext}";
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset != null) return asset;
            }

            // Fallback: search folder for any model file
            var guids = AssetDatabase.FindAssets("t:Model", new[] { folder });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset != null) return asset;
            }

            return null;
        }

        static Material[] CreateMaterials(string modelFolder, string matDir, string weaponName)
        {
            // Find textures in folder
            var texGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { modelFolder });
            var textures = texGuids
                .Select(g => AssetDatabase.GUIDToAssetPath(g))
                .Where(p => !p.Contains("/Materials/"))
                .Select(p => new TextureEntry { path = p, name = Path.GetFileNameWithoutExtension(p).ToLower(), tex = AssetDatabase.LoadAssetAtPath<Texture2D>(p) })
                .Where(t => t.tex != null)
                .ToArray();

            if (textures.Length == 0)
                return new Material[0];

            // Find color/albedo texture
            Texture2D colorTex = textures
                .Where(t => t.name.Contains("color") || t.name.Contains("basecolor") || t.name.Contains("_bc"))
                .Select(t => t.tex).FirstOrDefault();

            // Find normal map
            Texture2D normalTex = textures
                .Where(t => t.name.Contains("normal") || t.name.Contains("_n"))
                .Select(t => t.tex).FirstOrDefault();

            // Find ORM (Occlusion/Roughness/Metallic) texture
            Texture2D ormTex = textures
                .Where(t => t.name.Contains("orm"))
                .Select(t => t.tex).FirstOrDefault();

            // Find roughness texture (standalone)
            Texture2D roughTex = textures
                .Where(t => (t.name.Contains("rough") || t.name.Contains("_r")) && !t.name.Contains("orm") && !t.name.Contains("normal") && !t.name.Contains("color"))
                .Select(t => t.tex).FirstOrDefault();

            // Find metallic texture (standalone)
            Texture2D metallicTex = textures
                .Where(t => (t.name.Contains("metallic") || t.name.Contains("_m")) && !t.name.Contains("orm") && !t.name.Contains("normal") && !t.name.Contains("color"))
                .Select(t => t.tex).FirstOrDefault();

            // Set normal map texture type
            if (normalTex != null)
            {
                string normalPath = AssetDatabase.GetAssetPath(normalTex);
                var importer = AssetImporter.GetAtPath(normalPath) as TextureImporter;
                if (importer != null && importer.textureType != TextureImporterType.NormalMap)
                {
                    importer.textureType = TextureImporterType.NormalMap;
                    importer.SaveAndReimport();
                }
            }

            // Create URP Lit material
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogError("[WeaponModelSetup] URP/Lit shader not found!");
                return new Material[0];
            }

            var mat = new Material(shader);
            mat.name = $"{weaponName}_Mat";

            if (colorTex != null)
                mat.SetTexture("_BaseMap", colorTex);

            if (normalTex != null)
            {
                mat.SetTexture("_BumpMap", normalTex);
                mat.SetFloat("_BumpScale", 1f);
                mat.EnableKeyword("_NORMALMAP");
            }

            if (ormTex != null)
            {
                // ORM texture: R=Occlusion, G=Roughness, B=Metallic
                // Use R channel for occlusion (correct mapping)
                mat.SetTexture("_OcclusionMap", ormTex);
                mat.EnableKeyword("_OCCLUSIONMAP");
                mat.SetFloat("_OcclusionStrength", 1f);

                // Generate a proper MetallicSmoothness texture from ORM data
                // URP Lit _MetallicGlossMap expects: R=Metallic, A=Smoothness
                var msTex = CreateMetallicSmoothnessFromORM(ormTex);
                if (msTex != null)
                {
                    string msPath = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(ormTex));
                    msPath = $"{msPath}/Materials/{weaponName}_MetallicSmoothness.asset";
                    AssetDatabase.CreateAsset(msTex, msPath);
                    mat.SetTexture("_MetallicGlossMap", msTex);
                    mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                }
                mat.SetFloat("_Smoothness", 1f); // driven by texture alpha
                mat.SetFloat("_Metallic", 0f);   // driven by texture R
            }
            else
            {
                if (metallicTex != null)
                {
                    mat.SetTexture("_MetallicGlossMap", metallicTex);
                    mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                    mat.SetFloat("_Smoothness", 0.5f);
                }
                else
                {
                    mat.SetFloat("_Metallic", 0.3f);
                }

                if (roughTex != null)
                    mat.SetFloat("_Smoothness", 0.3f);
                else if (metallicTex == null)
                    mat.SetFloat("_Smoothness", 0.5f);
            }

            string matPath = $"{matDir}/{weaponName}_Mat.mat";
            AssetDatabase.CreateAsset(mat, matPath);

            // For Glock, create additional materials for barrel and magazine
            if (weaponName == "Glock")
            {
                var barrelMat = CreateGlockPartMaterial(textures, matDir, "barrel", shader);
                var magMat = CreateGlockPartMaterial(textures, matDir, "mag", shader);
                return new[] { mat, barrelMat, magMat }.Where(m => m != null).ToArray();
            }

            return new[] { mat };
        }

        static Material CreateGlockPartMaterial(TextureEntry[] textures, string matDir, string partName, Shader shader)
        {
            Texture2D color = null, normal = null, metallic = null, rough = null;

            foreach (var t in textures)
            {
                string name = t.name;
                if (!name.Contains(partName)) continue;
                Texture2D tex = t.tex;

                if (name.Contains("_bc") || name.Contains("color") || name.Contains("basecolor"))
                    color = tex;
                else if (name.Contains("_n") || name.Contains("normal"))
                    normal = tex;
                else if (name.Contains("_m") || name.Contains("metallic"))
                    metallic = tex;
                else if (name.Contains("_r") || name.Contains("rough"))
                    rough = tex;
            }

            if (color == null && normal == null) return null;

            var mat = new Material(shader);
            mat.name = $"Glock_{partName}_Mat";

            if (color != null) mat.SetTexture("_BaseMap", color);
            if (normal != null)
            {
                string normalPath = AssetDatabase.GetAssetPath(normal);
                var importer = AssetImporter.GetAtPath(normalPath) as TextureImporter;
                if (importer != null && importer.textureType != TextureImporterType.NormalMap)
                {
                    importer.textureType = TextureImporterType.NormalMap;
                    importer.SaveAndReimport();
                }
                mat.SetTexture("_BumpMap", normal);
                mat.EnableKeyword("_NORMALMAP");
            }
            if (metallic != null)
            {
                mat.SetTexture("_MetallicGlossMap", metallic);
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
            mat.SetFloat("_Smoothness", rough != null ? 0.3f : 0.5f);
            mat.SetFloat("_Metallic", 0.3f);

            string matPath = $"{matDir}/Glock_{partName}_Mat.mat";
            AssetDatabase.CreateAsset(mat, matPath);
            return mat;
        }

        static void AssignToWeaponData(string weaponDataName, GameObject prefab)
        {
            string assetPath = $"Assets/_Project/ScriptableObjects/Weapons/{weaponDataName}.asset";
            var weaponData = AssetDatabase.LoadAssetAtPath<WeaponData>(assetPath);
            if (weaponData == null)
            {
                Debug.LogWarning($"[WeaponModelSetup] WeaponData not found at {assetPath}");
                return;
            }

            weaponData.viewModelPrefab = prefab;
            EditorUtility.SetDirty(weaponData);
        }

        static Texture2D CreateMetallicSmoothnessFromORM(Texture2D ormTex)
        {
            // Make ORM texture readable
            string ormPath = AssetDatabase.GetAssetPath(ormTex);
            var importer = AssetImporter.GetAtPath(ormPath) as TextureImporter;
            bool wasReadable = true;
            if (importer != null && !importer.isReadable)
            {
                wasReadable = false;
                importer.isReadable = true;
                importer.SaveAndReimport();
            }

            int w = ormTex.width;
            int h = ormTex.height;
            var srcPixels = ormTex.GetPixels();
            var dstPixels = new Color[srcPixels.Length];

            for (int i = 0; i < srcPixels.Length; i++)
            {
                float metallic = srcPixels[i].b;   // ORM: B = Metallic
                float roughness = srcPixels[i].g;   // ORM: G = Roughness
                float smoothness = 1f - roughness;  // URP wants smoothness
                dstPixels[i] = new Color(metallic, metallic, metallic, smoothness);
            }

            var msTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
            msTex.SetPixels(dstPixels);
            msTex.Apply(true);

            // Restore original readability
            if (!wasReadable && importer != null)
            {
                importer.isReadable = false;
                importer.SaveAndReimport();
            }

            return msTex;
        }

        static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
                SetLayerRecursive(child.gameObject, layer);
        }
    }
}
