#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CounterSiege.Editor
{
    public static class WeaponPlaygroundBuilder
    {
        [MenuItem("Counter Siege/Create Weapon Playground Scene")]
        public static void BuildScene()
        {
            // Create new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            scene.name = "WeaponPlayground";

            // Remove default directional light; a custom one is set up below.
            var defaultLight = GameObject.Find("Directional Light");
            if (defaultLight != null) Object.DestroyImmediate(defaultLight);

            // ── Lighting ──
            var lightObj = new GameObject("Directional Light");
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.95f, 0.85f);
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;
            lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);
            Undo.RegisterCreatedObjectUndo(lightObj, "Create light");

            // ── Ground plane ──
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.position = new Vector3(0, -0.5f, 0);
            ground.transform.localScale = new Vector3(100, 1, 100);
            ground.isStatic = true;
            var groundR = ground.GetComponent<Renderer>();
            var groundMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            groundMat.color = new Color(0.35f, 0.38f, 0.35f);
            EnsureDirectory("Assets/_Project/Materials");
            AssetDatabase.CreateAsset(groundMat, "Assets/_Project/Materials/PlaygroundGround.mat");
            groundR.sharedMaterial = groundMat;
            Undo.RegisterCreatedObjectUndo(ground, "Create ground");

            // ── Walls (backstop behind targets) ──
            var backWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backWall.name = "BackWall";
            backWall.transform.position = new Vector3(0, 2.5f, 25);
            backWall.transform.localScale = new Vector3(30, 6, 0.5f);
            backWall.isStatic = true;
            var wallMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            wallMat.color = new Color(0.5f, 0.45f, 0.4f);
            AssetDatabase.CreateAsset(wallMat, "Assets/_Project/Materials/PlaygroundWall.mat");
            backWall.GetComponent<Renderer>().sharedMaterial = wallMat;
            Undo.RegisterCreatedObjectUndo(backWall, "Create back wall");

            // Side walls
            var leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftWall.name = "LeftWall";
            leftWall.transform.position = new Vector3(-15, 2.5f, 12.5f);
            leftWall.transform.localScale = new Vector3(0.5f, 6, 25);
            leftWall.isStatic = true;
            leftWall.GetComponent<Renderer>().sharedMaterial = wallMat;
            Undo.RegisterCreatedObjectUndo(leftWall, "Create left wall");

            var rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightWall.name = "RightWall";
            rightWall.transform.position = new Vector3(15, 2.5f, 12.5f);
            rightWall.transform.localScale = new Vector3(0.5f, 6, 25);
            rightWall.isStatic = true;
            rightWall.GetComponent<Renderer>().sharedMaterial = wallMat;
            Undo.RegisterCreatedObjectUndo(rightWall, "Create right wall");

            // ── Range markers (distance lines on the ground) ──
            var markerMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            markerMat.color = new Color(0.8f, 0.8f, 0.2f);
            AssetDatabase.CreateAsset(markerMat, "Assets/_Project/Materials/PlaygroundMarker.mat");

            float[] distances = { 5f, 10f, 15f, 20f };
            foreach (float d in distances)
            {
                var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                marker.name = $"Marker_{d}m";
                marker.transform.position = new Vector3(0, 0.01f, d);
                marker.transform.localScale = new Vector3(20, 0.02f, 0.05f);
                marker.isStatic = true;
                marker.GetComponent<Renderer>().sharedMaterial = markerMat;
                Object.DestroyImmediate(marker.GetComponent<Collider>());
                Undo.RegisterCreatedObjectUndo(marker, "Create marker");
            }

            // ── Cover objects for testing ──
            var coverMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            coverMat.color = new Color(0.45f, 0.35f, 0.25f);
            AssetDatabase.CreateAsset(coverMat, "Assets/_Project/Materials/PlaygroundCover.mat");

            // A few crates
            CreateCrate(new Vector3(-4, 0.5f, 6), new Vector3(2, 1, 2), coverMat);
            CreateCrate(new Vector3(5, 0.5f, 8), new Vector3(1.5f, 1.5f, 1.5f), coverMat);
            CreateCrate(new Vector3(0, 0.5f, 10), new Vector3(1, 1, 1), coverMat);
            CreateCrate(new Vector3(0, 1.5f, 10), new Vector3(1, 1, 1), coverMat);

            // ── AudioManager ──
            if (Object.FindAnyObjectByType<AudioManager>() == null)
            {
                var audioObj = new GameObject("AudioManager");
                audioObj.AddComponent<AudioManager>();
                Undo.RegisterCreatedObjectUndo(audioObj, "Create AudioManager");
            }

            // ── WeaponPlayground manager ──
            var managerObj = new GameObject("_WeaponPlayground");
            var playground = managerObj.AddComponent<WeaponPlayground>();

            // Wire references
            var weaponDb = AssetDatabase.LoadAssetAtPath<WeaponDatabase>("Assets/_Project/ScriptableObjects/WeaponDatabase.asset");
            var inputActions = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player.prefab");
            var hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/HUD.prefab");

            playground.weaponDatabase = weaponDb;
            playground.inputActions = inputActions;
            playground.playerPrefab = playerPrefab;
            playground.hudPrefab = hudPrefab;
            playground.infiniteAmmo = true;
            playground.targetCount = 6;
            playground.targetSpacing = 3f;
            playground.targetDistance = 15f;

            Undo.RegisterCreatedObjectUndo(managerObj, "Create WeaponPlayground manager");

            // Weapon pickup labels. The pickups themselves are spawned at
            // runtime by WeaponPlayground; static labels mark where they go.

            // ── Save scene ──
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string scenePath = "Assets/_Project/Scenes/WeaponPlayground.unity";
            EnsureDirectory("Assets/_Project/Scenes");
            EditorSceneManager.SaveScene(scene, scenePath);

            // Add to build settings if not already there
            AddSceneToBuildSettings(scenePath);

            Debug.Log($"Weapon Playground scene created at {scenePath}!\n" +
                      "Press Play to test weapons. Controls:\n" +
                      "  WASD - Move  |  Mouse - Look  |  LMB - Fire  |  RMB - Scope/Alt fire\n" +
                      "  1/2/3 - Weapon slots  |  R - Reload  |  G - Drop  |  T - Reset targets\n" +
                      "  B - Buy menu (if HUD available)");
        }

        static void CreateCrate(Vector3 pos, Vector3 scale, Material mat)
        {
            var crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crate.name = "Crate";
            crate.transform.position = pos;
            crate.transform.localScale = scale;
            crate.isStatic = true;
            crate.GetComponent<Renderer>().sharedMaterial = mat;
            Undo.RegisterCreatedObjectUndo(crate, "Create crate");
        }

        static void AddSceneToBuildSettings(string scenePath)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var s in scenes)
            {
                if (s.path == scenePath) return; // Already added
            }
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log($"Added {scenePath} to Build Settings.");
        }

        static void EnsureDirectory(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
                string folder = System.IO.Path.GetFileName(path);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }
    }
}
#endif
