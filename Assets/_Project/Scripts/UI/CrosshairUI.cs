using UnityEngine;
using UnityEngine.UI;

namespace CounterSiege
{
    public class CrosshairUI : MonoBehaviour
    {
        [Header("Crosshair Style")]
        public float lineLength = 6f;
        public float lineThickness = 2f;
        public float centerGap = 4f;
        public float outlineThickness = 1f;
        public Color crosshairColor = Color.green;
        public Color outlineColor = new Color(0, 0, 0, 0.8f);
        public bool showCenterDot = true;
        public float dotSize = 2f;

        [Header("Dynamic Spread")]
        public float crosshairScale = 3f;  // pixels per degree of inaccuracy
        public float maxSpread = 25f;

        // Line transforms: [0]=top, [1]=bottom, [2]=left, [3]=right
        // Each has an outline behind it
        RectTransform[] lines = new RectTransform[4];
        RectTransform[] outlines = new RectTransform[4];
        RectTransform centerDot;
        RectTransform centerDotOutline;

        [HideInInspector] public RectTransform[] lineRefs; // for HUDBuilder backwards compat

        float currentSpread;

        void Awake()
        {
            BuildCrosshair();
        }

        void BuildCrosshair()
        {
            var rt = GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(100, 100);

            // Directions: offset dir, size orientation
            // Top:    offset (0, +gap), size (thickness, length)
            // Bottom: offset (0, -gap), size (thickness, length)
            // Left:   offset (-gap, 0), size (length, thickness)
            // Right:  offset (+gap, 0), size (length, thickness)

            for (int i = 0; i < 4; i++)
            {
                // Outline (slightly larger, behind)
                outlines[i] = CreateLine($"Outline_{i}", outlineColor);
                // Main line
                lines[i] = CreateLine($"Line_{i}", crosshairColor);
            }

            // Center dot
            if (showCenterDot)
            {
                centerDotOutline = CreateLine("DotOutline", outlineColor);
                centerDot = CreateLine("Dot", crosshairColor);
            }

            UpdatePositions(centerGap);
        }

        RectTransform CreateLine(string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;

            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;

            return rect;
        }

        void Update()
        {
            var player = GameManager.Instance?.playerObject;
            if (player == null) return;

            // Read inaccuracy directly from the equipped weapon
            float inaccuracyDeg = 0f;
            var inventory = player.GetComponent<PlayerInventory>();
            if (inventory != null && inventory.CurrentWeapon is GunBase gun)
                inaccuracyDeg = gun.CurrentInaccuracyDegrees;

            float targetSpread = Mathf.Min(inaccuracyDeg * crosshairScale, maxSpread);

            // Smooth the spread
            currentSpread = Mathf.Lerp(currentSpread, targetSpread, 15f * Time.deltaTime);

            float gap = centerGap + currentSpread;
            UpdatePositions(gap);
        }

        void UpdatePositions(float gap)
        {
            float ol = outlineThickness;

            // Top line
            lines[0].anchoredPosition = new Vector2(0, gap + lineLength * 0.5f);
            lines[0].sizeDelta = new Vector2(lineThickness, lineLength);
            outlines[0].anchoredPosition = lines[0].anchoredPosition;
            outlines[0].sizeDelta = new Vector2(lineThickness + ol * 2, lineLength + ol * 2);

            // Bottom line
            lines[1].anchoredPosition = new Vector2(0, -(gap + lineLength * 0.5f));
            lines[1].sizeDelta = new Vector2(lineThickness, lineLength);
            outlines[1].anchoredPosition = lines[1].anchoredPosition;
            outlines[1].sizeDelta = new Vector2(lineThickness + ol * 2, lineLength + ol * 2);

            // Left line
            lines[2].anchoredPosition = new Vector2(-(gap + lineLength * 0.5f), 0);
            lines[2].sizeDelta = new Vector2(lineLength, lineThickness);
            outlines[2].anchoredPosition = lines[2].anchoredPosition;
            outlines[2].sizeDelta = new Vector2(lineLength + ol * 2, lineThickness + ol * 2);

            // Right line
            lines[3].anchoredPosition = new Vector2(gap + lineLength * 0.5f, 0);
            lines[3].sizeDelta = new Vector2(lineLength, lineThickness);
            outlines[3].anchoredPosition = lines[3].anchoredPosition;
            outlines[3].sizeDelta = new Vector2(lineLength + ol * 2, lineThickness + ol * 2);

            // Center dot
            if (centerDot != null)
            {
                centerDot.anchoredPosition = Vector2.zero;
                centerDot.sizeDelta = new Vector2(dotSize, dotSize);
            }
            if (centerDotOutline != null)
            {
                centerDotOutline.anchoredPosition = Vector2.zero;
                centerDotOutline.sizeDelta = new Vector2(dotSize + ol * 2, dotSize + ol * 2);
            }
        }
    }
}
