using UnityEngine;
using UnityEngine.UI;

namespace CounterSiege
{
    public class ScopeOverlayUI : MonoBehaviour
    {
        CanvasGroup canvasGroup;
        RawImage vignetteImage;
        RawImage ringImage;

        Texture2D vignetteTexture;
        Texture2D ringTexture;

        bool texturesBuilt;
        float currentAlpha;
        float targetAlpha;

        const float CIRCLE_RADIUS = 0.42f; // fraction of screen height
        const float RING_RADIUS_FRAC = 0.95f; // ring at 95% of circle edge
        const float EDGE_SOFTNESS = 4f; // pixels of AA on vignette edge
        const float FADE_SPEED = 5f; // ~0.2s to full opacity

        void Awake()
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            // GO starts inactive via prefab; Show() activates it
        }

        void OnDestroy()
        {
            if (vignetteTexture != null) Destroy(vignetteTexture);
            if (ringTexture != null) Destroy(ringTexture);
        }

        void EnsureBuilt()
        {
            if (texturesBuilt) return;
            texturesBuilt = true;
            BuildVignette();
            BuildReticle();
        }

        void BuildVignette()
        {
            // Generate texture at correct aspect ratio so circle stays circular
            float aspect = (float)Screen.width / Screen.height;
            int texH = 512;
            int texW = Mathf.Max(texH, Mathf.RoundToInt(texH * aspect));

            vignetteTexture = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
            vignetteTexture.filterMode = FilterMode.Bilinear;
            vignetteTexture.wrapMode = TextureWrapMode.Clamp;

            float centerX = texW * 0.5f;
            float centerY = texH * 0.5f;
            float radius = texH * CIRCLE_RADIUS;

            var pixels = new Color32[texW * texH];

            for (int y = 0; y < texH; y++)
            {
                for (int x = 0; x < texW; x++)
                {
                    float dx = x - centerX;
                    float dy = y - centerY;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist < radius - EDGE_SOFTNESS)
                    {
                        pixels[y * texW + x] = new Color32(0, 0, 0, 0);
                    }
                    else if (dist > radius + EDGE_SOFTNESS)
                    {
                        pixels[y * texW + x] = new Color32(0, 0, 0, 255);
                    }
                    else
                    {
                        float t = (dist - (radius - EDGE_SOFTNESS)) / (EDGE_SOFTNESS * 2f);
                        t = t * t * (3f - 2f * t); // smoothstep
                        pixels[y * texW + x] = new Color32(0, 0, 0, (byte)(t * 255));
                    }
                }
            }

            vignetteTexture.SetPixels32(pixels);
            vignetteTexture.Apply();

            // Fullscreen RawImage
            var vigGO = new GameObject("ScopeVignette");
            vigGO.transform.SetParent(transform, false);
            var rt = vigGO.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            vignetteImage = vigGO.AddComponent<RawImage>();
            vignetteImage.texture = vignetteTexture;
            vignetteImage.raycastTarget = false;
        }

        void BuildReticle()
        {
            var reticleGO = new GameObject("ScopeReticle");
            reticleGO.transform.SetParent(transform, false);
            var reticleRT = reticleGO.AddComponent<RectTransform>();
            reticleRT.anchorMin = new Vector2(0.5f, 0.5f);
            reticleRT.anchorMax = new Vector2(0.5f, 0.5f);
            reticleRT.sizeDelta = Vector2.zero;

            // Circle diameter in canvas units (reference 1920x1080)
            float circleDiameter = 1080f * CIRCLE_RADIUS * 2f;
            float circleRadius = circleDiameter * 0.5f;

            Color lineColor = new Color(0, 0, 0, 1f);
            float lineThick = 2.5f;

            // Vertical crosshair stave
            CreateLine("VertLine", reticleRT, lineColor,
                Vector2.zero, new Vector2(lineThick, circleDiameter));

            // Horizontal crosshair stave
            CreateLine("HorizLine", reticleRT, lineColor,
                Vector2.zero, new Vector2(circleDiameter, lineThick));

            // Rangefinder tick marks (3 ticks below center on vertical stave)
            float tickWidth = 14f;
            for (int i = 0; i < 3; i++)
            {
                float yOff = -(circleRadius * 0.15f) - (i * circleRadius * 0.1f);
                CreateLine($"Tick_{i}", reticleRT, lineColor,
                    new Vector2(0, yOff), new Vector2(tickWidth, lineThick));
            }

            // Scope ring arcs (circle with gaps at cardinal points)
            BuildRingTexture(reticleRT, circleDiameter);
        }

        void BuildRingTexture(RectTransform parent, float circleDiameter)
        {
            int texSize = 512;
            ringTexture = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
            ringTexture.filterMode = FilterMode.Bilinear;
            ringTexture.wrapMode = TextureWrapMode.Clamp;

            float center = texSize * 0.5f;
            float radius = texSize * 0.5f * RING_RADIUS_FRAC;
            float ringWidth = 1.5f;
            float gapAngle = 8f; // degrees gap at each cardinal direction

            var pixels = new Color32[texSize * texSize];
            var clear = new Color32(0, 0, 0, 0);

            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

            for (int y = 0; y < texSize; y++)
            {
                for (int x = 0; x < texSize; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    float edgeDist = Mathf.Abs(dist - radius);
                    if (edgeDist > ringWidth + 1f) continue;

                    float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                    if (angle < 0) angle += 360f;

                    // Gaps at 0 (right), 90 (up), 180 (left), 270 (down)
                    bool inGap = false;
                    for (int g = 0; g < 4; g++)
                    {
                        float diff = Mathf.Abs(Mathf.DeltaAngle(angle, g * 90f));
                        if (diff < gapAngle) { inGap = true; break; }
                    }

                    if (!inGap)
                    {
                        float alpha = Mathf.Clamp01(1f - edgeDist / (ringWidth + 0.5f));
                        pixels[y * texSize + x] = new Color32(0, 0, 0, (byte)(alpha * 216));
                    }
                }
            }

            ringTexture.SetPixels32(pixels);
            ringTexture.Apply();

            var ringGO = new GameObject("ScopeRing");
            ringGO.transform.SetParent(parent, false);
            var rt = ringGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(circleDiameter, circleDiameter);
            rt.anchoredPosition = Vector2.zero;

            ringImage = ringGO.AddComponent<RawImage>();
            ringImage.texture = ringTexture;
            ringImage.raycastTarget = false;
        }

        void CreateLine(string name, RectTransform parent, Color color,
            Vector2 position, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }

        void Update()
        {
            if (Mathf.Abs(currentAlpha - targetAlpha) > 0.005f)
            {
                currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, FADE_SPEED * Time.deltaTime);
                canvasGroup.alpha = currentAlpha;
            }
            else
            {
                currentAlpha = targetAlpha;
                canvasGroup.alpha = currentAlpha;
                if (targetAlpha <= 0f)
                    gameObject.SetActive(false);
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);
            EnsureBuilt();
            targetAlpha = 1f;
        }

        public void Hide()
        {
            if (!gameObject.activeSelf) return;
            targetAlpha = 0f;
        }

        public float ScopeProgress => currentAlpha;
        public bool IsFullyVisible => currentAlpha >= 0.99f;
    }
}
