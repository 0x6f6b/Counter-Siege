using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CounterSiege
{
    public class KillfeedUI : MonoBehaviour
    {
        public Transform killfeedParent;
        public float displayDuration = 4f;
        public int maxEntries = 5;

        List<KillfeedEntry> entries = new();

        struct KillfeedEntry
        {
            public GameObject uiObject;
            public float spawnTime;
        }

        void Start()
        {
            EventBus.OnKill += OnKill;
        }

        void OnDestroy()
        {
            EventBus.OnKill -= OnKill;
        }

        void OnKill(GameObject victim, GameObject killer, string weapon, HitZone hitZone)
        {
            string killerName = "World";
            string victimName = "Unknown";

            if (killer != null)
            {
                var kh = killer.GetComponent<PlayerHealth>();
                killerName = kh != null ? kh.playerName : killer.name;
            }

            var vh = victim.GetComponent<PlayerHealth>();
            victimName = vh != null ? vh.playerName : victim.name;

            string headshot = hitZone == HitZone.Head ? " [HS]" : "";
            AddEntry($"{killerName}  [{weapon}]{headshot}  {victimName}");
        }

        void AddEntry(string text)
        {
            if (killfeedParent == null) return;

            var go = new GameObject("KillfeedEntry");
            go.transform.SetParent(killfeedParent, false);

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 20;

            var txt = go.AddComponent<Text>();
            txt.text = text;
            txt.fontSize = 14;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleRight;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (txt.font == null)
                txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (txt.font == null)
                txt.font = Font.CreateDynamicFontFromOSFont("Arial", 14);
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;

            entries.Add(new KillfeedEntry { uiObject = go, spawnTime = Time.time });

            while (entries.Count > maxEntries)
            {
                Destroy(entries[0].uiObject);
                entries.RemoveAt(0);
            }
        }

        void Update()
        {
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                float age = Time.time - entries[i].spawnTime;
                if (age > displayDuration)
                {
                    if (entries[i].uiObject != null)
                        Destroy(entries[i].uiObject);
                    entries.RemoveAt(i);
                }
            }
        }
    }
}
