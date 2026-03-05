using UnityEngine;
using UnityEngine.InputSystem;

namespace CounterSiege
{
    public class WeaponPreview : MonoBehaviour
    {
        public GameObject[] weaponPrefabs;
        public Transform weaponHolder;

        GameObject currentInstance;
        int currentIndex;

        // Adjustment speeds
        float moveSpeed = 0.1f;
        float rotateSpeed = 15f;
        float scaleSpeed = 0.5f;

        bool showHelp = true;
        Keyboard kb;

        void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (weaponPrefabs != null && weaponPrefabs.Length > 0)
                SpawnWeapon(0);
        }

        void Update()
        {
            kb = Keyboard.current;
            if (kb == null || currentInstance == null) return;

            float dt = Time.deltaTime;

            // Speed modifiers: hold Shift = 3x, hold Ctrl = 0.2x
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

            // Switch weapons: Q/E
            if (kb.qKey.wasPressedThisFrame)
                SpawnWeapon((currentIndex - 1 + weaponPrefabs.Length) % weaponPrefabs.Length);
            if (kb.eKey.wasPressedThisFrame)
                SpawnWeapon((currentIndex + 1) % weaponPrefabs.Length);

            // Adjust position: WASD + R/F for up/down
            Vector3 posOffset = Vector3.zero;
            if (kb.wKey.isPressed) posOffset.z += moveSpeed * dt;
            if (kb.sKey.isPressed) posOffset.z -= moveSpeed * dt;
            if (kb.aKey.isPressed) posOffset.x -= moveSpeed * dt;
            if (kb.dKey.isPressed) posOffset.x += moveSpeed * dt;
            if (kb.rKey.isPressed) posOffset.y += moveSpeed * dt;
            if (kb.fKey.isPressed) posOffset.y -= moveSpeed * dt;
            currentInstance.transform.localPosition += posOffset;

            // Adjust rotation: I/K = pitch, J/L = yaw, U/O = roll
            Vector3 rotOffset = Vector3.zero;
            if (kb.iKey.isPressed) rotOffset.x -= rotateSpeed * dt;
            if (kb.kKey.isPressed) rotOffset.x += rotateSpeed * dt;
            if (kb.jKey.isPressed) rotOffset.y -= rotateSpeed * dt;
            if (kb.lKey.isPressed) rotOffset.y += rotateSpeed * dt;
            if (kb.uKey.isPressed) rotOffset.z += rotateSpeed * dt;
            if (kb.oKey.isPressed) rotOffset.z -= rotateSpeed * dt;
            currentInstance.transform.localEulerAngles += rotOffset;

            // Adjust scale: +/- (uniform)
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

            // Reset transform
            if (kb.backquoteKey.wasPressedThisFrame)
            {
                currentInstance.transform.localPosition = Vector3.zero;
                currentInstance.transform.localEulerAngles = Vector3.zero;
                currentInstance.transform.localScale = Vector3.one * 0.01f;
            }
        }

        void SpawnWeapon(int index)
        {
            if (currentInstance != null)
                Destroy(currentInstance);

            currentIndex = index;
            currentInstance = Instantiate(weaponPrefabs[index], weaponHolder);
            currentInstance.name = weaponPrefabs[index].name;

            Debug.Log($"[WeaponPreview] Showing: {currentInstance.name}");
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
            Debug.Log($"[WeaponPreview] Copied to clipboard:\n{text}");
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

            if (showHelp)
            {
                var helpStyle = new GUIStyle(GUI.skin.label) { fontSize = 13 };
                helpStyle.normal.textColor = new Color(1, 1, 1, 0.7f);

                float y = 130;
                float lineH = 20;
                string[] lines = {
                    "Q / E  -  Prev / Next weapon",
                    "WASD   -  Move X/Z",
                    "R / F  -  Move Up / Down",
                    "IJKL   -  Rotate Pitch / Yaw",
                    "U / O  -  Rotate Roll",
                    "+  / - -  Scale Up / Down",
                    "Shift  -  Fast  |  Ctrl  -  Slow",
                    "C      -  Copy transform to clipboard",
                    "`      -  Reset transform",
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
