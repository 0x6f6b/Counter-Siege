using UnityEngine;
using UnityEngine.InputSystem;

namespace CounterSiege
{
    public class ViewModelAlignment : MonoBehaviour
    {
        public GameObject[] weaponPrefabs;
        public Transform weaponHolder;
        public GameObject characterModel;
        public Vector3 orbitTarget = new Vector3(0.2f, 1.3f, 0.3f);

        GameObject currentInstance;
        int currentIndex;
        int currentAnim; // 0=idle, 1=walk, 2=run
        Animator characterAnimator;
        Camera mainCam;

        // Orbit camera state
        float orbitYaw = 30f;
        float orbitPitch = 10f;
        float orbitDist = 2.5f;

        float moveSpeed = 0.1f;
        float rotateSpeed = 15f;
        float scaleSpeed = 0.5f;

        bool showHelp = true;
        Keyboard kb;
        Mouse mouse;

        void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            mainCam = Camera.main;

            if (characterModel != null)
            {
                characterAnimator = characterModel.GetComponentInChildren<Animator>();
                if (characterAnimator != null)
                {
                    characterAnimator.applyRootMotion = false;
                    characterAnimator.SetBool("IsGrounded", true);
                }
            }

            if (weaponPrefabs != null && weaponPrefabs.Length > 0)
                SpawnWeapon(0);

            UpdateOrbitCamera();
        }

        void Update()
        {
            kb = Keyboard.current;
            mouse = Mouse.current;
            if (kb == null || currentInstance == null) return;

            float dt = Time.deltaTime;

            // Speed modifiers
            if (kb.leftShiftKey.isPressed)
            {
                moveSpeed = 0.3f;
                rotateSpeed = 45f;
                scaleSpeed = 1.5f;
            }
            else if (kb.leftCtrlKey.isPressed)
            {
                moveSpeed = 0.02f;
                rotateSpeed = 3f;
                scaleSpeed = 0.1f;
            }
            else
            {
                moveSpeed = 0.1f;
                rotateSpeed = 15f;
                scaleSpeed = 0.5f;
            }

            // Orbit camera: right-click drag to rotate, scroll to zoom
            if (mouse != null)
            {
                if (mouse.rightButton.isPressed)
                {
                    var delta = mouse.delta.ReadValue();
                    orbitYaw += delta.x * 0.3f;
                    orbitPitch -= delta.y * 0.3f;
                    orbitPitch = Mathf.Clamp(orbitPitch, -60f, 80f);
                }

                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.1f)
                {
                    orbitDist -= scroll * 0.002f;
                    orbitDist = Mathf.Clamp(orbitDist, 0.5f, 6f);
                }

                UpdateOrbitCamera();
            }

            // Switch weapons: Q/E
            if (kb.qKey.wasPressedThisFrame)
                SpawnWeapon((currentIndex - 1 + weaponPrefabs.Length) % weaponPrefabs.Length);
            if (kb.eKey.wasPressedThisFrame)
                SpawnWeapon((currentIndex + 1) % weaponPrefabs.Length);

            // Adjust position: WASD + R/F
            Vector3 posOffset = Vector3.zero;
            if (kb.wKey.isPressed) posOffset.z += moveSpeed * dt;
            if (kb.sKey.isPressed) posOffset.z -= moveSpeed * dt;
            if (kb.aKey.isPressed) posOffset.x -= moveSpeed * dt;
            if (kb.dKey.isPressed) posOffset.x += moveSpeed * dt;
            if (kb.rKey.isPressed) posOffset.y += moveSpeed * dt;
            if (kb.fKey.isPressed) posOffset.y -= moveSpeed * dt;
            currentInstance.transform.localPosition += posOffset;

            // Adjust rotation: IJKL + U/O
            Vector3 rotOffset = Vector3.zero;
            if (kb.iKey.isPressed) rotOffset.x -= rotateSpeed * dt;
            if (kb.kKey.isPressed) rotOffset.x += rotateSpeed * dt;
            if (kb.jKey.isPressed) rotOffset.y -= rotateSpeed * dt;
            if (kb.lKey.isPressed) rotOffset.y += rotateSpeed * dt;
            if (kb.uKey.isPressed) rotOffset.z += rotateSpeed * dt;
            if (kb.oKey.isPressed) rotOffset.z -= rotateSpeed * dt;
            currentInstance.transform.localEulerAngles += rotOffset;

            // Adjust scale: +/-
            if (kb.equalsKey.isPressed || kb.numpadPlusKey.isPressed)
                currentInstance.transform.localScale *= 1f + scaleSpeed * dt;
            if (kb.minusKey.isPressed || kb.numpadMinusKey.isPressed)
                currentInstance.transform.localScale *= 1f - scaleSpeed * dt;

            // Toggle help
            if (kb.hKey.wasPressedThisFrame)
                showHelp = !showHelp;

            // Copy to clipboard
            if (kb.cKey.wasPressedThisFrame)
                CopyTransformToClipboard();

            // Save to prefab
            if (kb.pKey.wasPressedThisFrame)
                SaveToPrefab();

            // Reset transform
            if (kb.backquoteKey.wasPressedThisFrame)
            {
                currentInstance.transform.localPosition = Vector3.zero;
                currentInstance.transform.localEulerAngles = Vector3.zero;
                currentInstance.transform.localScale = Vector3.one * 0.01f;
            }

            // Switch animation: 1/2/3
            if (kb.digit1Key.wasPressedThisFrame) SetAnimation(0);
            if (kb.digit2Key.wasPressedThisFrame) SetAnimation(1);
            if (kb.digit3Key.wasPressedThisFrame) SetAnimation(2);
        }

        void UpdateOrbitCamera()
        {
            if (mainCam == null) return;

            float yawRad = orbitYaw * Mathf.Deg2Rad;
            float pitchRad = orbitPitch * Mathf.Deg2Rad;

            Vector3 offset = new Vector3(
                Mathf.Sin(yawRad) * Mathf.Cos(pitchRad),
                Mathf.Sin(pitchRad),
                Mathf.Cos(yawRad) * Mathf.Cos(pitchRad)
            ) * orbitDist;

            mainCam.transform.position = orbitTarget + offset;
            mainCam.transform.LookAt(orbitTarget);
        }

        void SpawnWeapon(int index)
        {
            if (currentInstance != null)
                Destroy(currentInstance);

            currentIndex = index;
            currentInstance = Instantiate(weaponPrefabs[index], weaponHolder);
            currentInstance.name = weaponPrefabs[index].name;

            // Remove colliders
            foreach (var col in currentInstance.GetComponentsInChildren<Collider>(true))
                Destroy(col);

            // Set WeaponType animator parameter based on weapon name
            if (characterAnimator != null)
            {
                int weaponType = GetWeaponTypeFromName(currentInstance.name);
                characterAnimator.SetInteger("WeaponType", weaponType);
            }

            Debug.Log($"[ViewModelAlignment] Showing: {currentInstance.name}");
        }

        // Maps weapon prefab name to WeaponType enum: Knife=0, Pistol=1, Rifle=2, Sniper=3
        int GetWeaponTypeFromName(string name)
        {
            string lower = name.ToLower();
            if (lower.Contains("glock") || lower.Contains("usp"))
                return 1; // Pistol
            if (lower.Contains("awp"))
                return 3; // Sniper
            if (lower.Contains("knife"))
                return 0; // Knife
            return 2; // Rifle (AK47, M4A4, etc.)
        }

        void SetAnimation(int index)
        {
            currentAnim = index;
            if (characterAnimator == null) return;

            float velZ = index switch
            {
                1 => 0.5f,
                2 => 1f,
                _ => 0f
            };
            characterAnimator.SetFloat("VelX", 0f);
            characterAnimator.SetFloat("VelZ", velZ);
            characterAnimator.SetFloat("Speed", velZ * 6.94f);

            string[] names = { "Idle", "Walk", "Run" };
            Debug.Log($"[ViewModelAlignment] Animation: {names[index]}");
        }

        void CopyTransformToClipboard()
        {
            var t = currentInstance.transform;
            var pos = t.localPosition;
            var rot = t.localEulerAngles;
            var scale = t.localScale;

            string text = $"{currentInstance.name}\n" +
                          $"Position: {pos.x:F4}, {pos.y:F4}, {pos.z:F4}\n" +
                          $"Rotation: {rot.x:F1}, {rot.y:F1}, {rot.z:F1}\n" +
                          $"Scale:    {scale.x:F4}, {scale.y:F4}, {scale.z:F4}";

            GUIUtility.systemCopyBuffer = text;
            Debug.Log($"[ViewModelAlignment] Copied to clipboard:\n{text}");
        }

        void SaveToPrefab()
        {
#if UNITY_EDITOR
            var prefab = weaponPrefabs[currentIndex];
            string prefabPath = UnityEditor.AssetDatabase.GetAssetPath(prefab);

            if (string.IsNullOrEmpty(prefabPath))
            {
                Debug.LogError("[ViewModelAlignment] Cannot find prefab asset path");
                return;
            }

            var prefabContents = UnityEditor.PrefabUtility.LoadPrefabContents(prefabPath);
            prefabContents.transform.localPosition = currentInstance.transform.localPosition;
            prefabContents.transform.localRotation = currentInstance.transform.localRotation;
            prefabContents.transform.localScale = currentInstance.transform.localScale;

            UnityEditor.PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
            UnityEditor.PrefabUtility.UnloadPrefabContents(prefabContents);

            Debug.Log($"[ViewModelAlignment] Saved transform to prefab: {prefabPath}");
#else
            Debug.LogWarning("[ViewModelAlignment] Prefab saving only works in Editor");
#endif
        }

        void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };
            style.normal.textColor = Color.white;

            string weaponName = currentInstance != null ? currentInstance.name : "None";
            GUI.Label(new Rect(20, 20, 500, 30), $"[{currentIndex + 1}/{weaponPrefabs.Length}] {weaponName}", style);

            if (currentInstance != null)
            {
                var t = currentInstance.transform;
                var smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 14 };
                smallStyle.normal.textColor = Color.white;

                GUI.Label(new Rect(20, 50, 500, 25),
                    $"Pos: ({t.localPosition.x:F4}, {t.localPosition.y:F4}, {t.localPosition.z:F4})", smallStyle);
                GUI.Label(new Rect(20, 72, 500, 25),
                    $"Rot: ({t.localEulerAngles.x:F1}, {t.localEulerAngles.y:F1}, {t.localEulerAngles.z:F1})", smallStyle);
                GUI.Label(new Rect(20, 94, 500, 25),
                    $"Scale: ({t.localScale.x:F4}, {t.localScale.y:F4}, {t.localScale.z:F4})", smallStyle);
            }

            string[] animNames = { "Idle", "Walk", "Run" };
            var animStyle = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            animStyle.normal.textColor = Color.cyan;
            GUI.Label(new Rect(20, 116, 500, 25), $"Anim: {animNames[currentAnim]}", animStyle);

            if (showHelp)
            {
                var helpStyle = new GUIStyle(GUI.skin.label) { fontSize = 13 };
                helpStyle.normal.textColor = new Color(1, 1, 1, 0.7f);

                float y = 150;
                float lineH = 20;
                string[] lines = {
                    "Right-click drag  -  Orbit camera",
                    "Scroll wheel      -  Zoom in/out",
                    "Q / E  -  Prev / Next weapon",
                    "WASD   -  Move X/Z",
                    "R / F  -  Move Up / Down",
                    "IJKL   -  Rotate Pitch / Yaw",
                    "U / O  -  Rotate Roll",
                    "+  / - -  Scale Up / Down",
                    "Shift  -  Fast  |  Ctrl  -  Slow",
                    "C      -  Copy transform to clipboard",
                    "P      -  Save transform to prefab",
                    "`      -  Reset transform",
                    "1/2/3  -  Idle / Walk / Run animation",
                    "H      -  Toggle this help"
                };

                foreach (var line in lines)
                {
                    GUI.Label(new Rect(20, y, 400, lineH), line, helpStyle);
                    y += lineH;
                }
            }
        }
    }
}
