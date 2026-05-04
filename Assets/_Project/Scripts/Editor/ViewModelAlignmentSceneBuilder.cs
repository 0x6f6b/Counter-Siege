#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CounterSiege.Editor
{
    public static class ViewModelAlignmentSceneBuilder
    {
        [MenuItem("Counter Siege/Setup/Open Viewmodel Alignment Scene")]
        public static void CreateViewModelAlignmentScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "ViewModelAlignment";

            // Lighting
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.4f, 0.4f, 0.45f);

            var lightGO = new GameObject("Directional Light");
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.color = new Color(1f, 0.95f, 0.9f);
            lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);

            var fillGO = new GameObject("Fill Light");
            var fill = fillGO.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.4f;
            fill.color = new Color(0.8f, 0.85f, 1f);
            fillGO.transform.rotation = Quaternion.Euler(-20, 120, 0);

            // Character model (CT)
            string ctModelPath = "Assets/_Project/Models/Characters/CT/source/Russian_Soldier1.fbx";
            var ctPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ctModelPath);
            GameObject characterGO = null;
            if (ctPrefab != null)
            {
                characterGO = (GameObject)PrefabUtility.InstantiatePrefab(ctPrefab);
                characterGO.name = "CharacterModel";
                characterGO.transform.position = Vector3.zero;
                characterGO.transform.rotation = Quaternion.identity;

                var anim = characterGO.GetComponent<Animator>();
                if (anim == null) anim = characterGO.AddComponent<Animator>();

                string controllerPath = "Assets/_Project/Animations/CharacterAnimator.controller";
                var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
                if (controller != null) anim.runtimeAnimatorController = controller;
                anim.applyRootMotion = false;
            }
            else
            {
                Debug.LogWarning($"[ViewModelAlignment] CT model not found at {ctModelPath}");
            }

            // Virtual eye point (represents the player's camera position in-game)
            // Weapon is parented here, NOT to the actual scene camera
            var eyePointGO = new GameObject("EyePoint");
            eyePointGO.transform.position = new Vector3(0, 1.6f, 0);
            eyePointGO.transform.rotation = Quaternion.identity;

            var holderGO = new GameObject("WeaponHolder");
            holderGO.transform.SetParent(eyePointGO.transform);
            holderGO.transform.localPosition = new Vector3(0.3f, -0.2f, 0.5f);

            // 3rd-person orbit camera framing the character upper body + weapon.
            var camGO = new GameObject("MainCamera");
            camGO.tag = "MainCamera";
            var cam = camGO.AddComponent<Camera>();
            cam.fieldOfView = 40;
            cam.nearClipPlane = 0.01f;
            cam.backgroundColor = new Color(0.15f, 0.15f, 0.18f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            // Position: front-right, looking at character's hands area
            camGO.transform.position = new Vector3(1.2f, 1.5f, 1.5f);
            camGO.transform.LookAt(new Vector3(0.2f, 1.3f, 0.3f));
            camGO.AddComponent<AudioListener>();

            // Floor
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(2, 1, 2);
            var floorMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            floorMat.color = new Color(0.25f, 0.25f, 0.28f);
            floor.GetComponent<Renderer>().sharedMaterial = floorMat;

            // Controller
            var controllerGO = new GameObject("_ViewModelAlignment");
            var ctrl = controllerGO.AddComponent<ViewModelAlignment>();
            ctrl.weaponHolder = holderGO.transform;
            ctrl.characterModel = characterGO;
            ctrl.orbitTarget = new Vector3(0.2f, 1.3f, 0.3f);

            // Load weapon prefabs
            string[] prefabNames = { "Glock", "USP", "AK47", "AWP", "M4A4" };
            var prefabs = new System.Collections.Generic.List<GameObject>();
            foreach (var name in prefabNames)
            {
                string path = $"Assets/_Project/Prefabs/Weapons/{name}_ViewModel.prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                    prefabs.Add(prefab);
                else
                    Debug.LogWarning($"[ViewModelAlignment] Prefab not found: {path}");
            }
            ctrl.weaponPrefabs = prefabs.ToArray();

            // Save scene
            string scenePath = "Assets/_Project/Scenes/ViewModelAlignment.unity";
            string sceneDir = "Assets/_Project/Scenes";
            if (!AssetDatabase.IsValidFolder(sceneDir))
                AssetDatabase.CreateFolder("Assets/_Project", "Scenes");

            EditorSceneManager.SaveScene(scene, scenePath);
            EditorSceneManager.MarkSceneDirty(scene);

            Debug.Log($"[ViewModelAlignment] Scene created at {scenePath}. Press Play to align weapons.");
        }
    }
}
#endif
