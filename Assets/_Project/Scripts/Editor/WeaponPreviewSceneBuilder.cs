#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CounterSiege.Editor
{
    public static class WeaponPreviewSceneBuilder
    {
        [MenuItem("Counter Siege/Setup/Open Weapon Preview Scene")]
        public static void CreateWeaponPreviewScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "WeaponPreview";

            // Sky/lighting
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.4f, 0.4f, 0.45f);

            // Directional light
            var lightGO = new GameObject("Directional Light");
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.color = new Color(1f, 0.95f, 0.9f);
            lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);

            // Fill light from below-right
            var fillGO = new GameObject("Fill Light");
            var fill = fillGO.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.4f;
            fill.color = new Color(0.8f, 0.85f, 1f);
            fillGO.transform.rotation = Quaternion.Euler(-20, 120, 0);

            // Camera (mimics FPS player camera)
            var camGO = new GameObject("PreviewCamera");
            var cam = camGO.AddComponent<Camera>();
            cam.fieldOfView = 60;
            cam.nearClipPlane = 0.01f;
            cam.backgroundColor = new Color(0.15f, 0.15f, 0.18f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGO.transform.position = Vector3.zero;
            camGO.transform.rotation = Quaternion.identity;
            camGO.AddComponent<AudioListener>();

            // Weapon holder (same offset as PlayerInventory)
            var holderGO = new GameObject("WeaponHolder");
            holderGO.transform.SetParent(camGO.transform);
            holderGO.transform.localPosition = new Vector3(0.3f, -0.2f, 0.5f);

            // Floor for visual reference
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.position = new Vector3(0, -2f, 0);
            floor.transform.localScale = new Vector3(2, 1, 2);
            var floorMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            floorMat.color = new Color(0.25f, 0.25f, 0.28f);
            floor.GetComponent<Renderer>().sharedMaterial = floorMat;

            // WeaponPreview controller
            var previewGO = new GameObject("_WeaponPreview");
            var preview = previewGO.AddComponent<WeaponPreview>();
            preview.weaponHolder = holderGO.transform;

            // Load all weapon prefabs
            string[] prefabNames = { "Glock", "USP", "AK47", "AWP", "M4A4" };
            var prefabs = new System.Collections.Generic.List<GameObject>();

            foreach (var name in prefabNames)
            {
                string path = $"Assets/_Project/Prefabs/Weapons/{name}_ViewModel.prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                    prefabs.Add(prefab);
                else
                    Debug.LogWarning($"[WeaponPreview] Prefab not found: {path}");
            }

            preview.weaponPrefabs = prefabs.ToArray();

            // Save scene
            string scenePath = "Assets/_Project/Scenes/WeaponPreview.unity";
            string sceneDir = "Assets/_Project/Scenes";
            if (!AssetDatabase.IsValidFolder(sceneDir))
                AssetDatabase.CreateFolder("Assets/_Project", "Scenes");

            EditorSceneManager.SaveScene(scene, scenePath);
            EditorSceneManager.MarkSceneDirty(scene);

            Debug.Log($"[WeaponPreview] Scene created at {scenePath}. Press Play to start adjusting weapons.");
        }
    }
}
#endif
