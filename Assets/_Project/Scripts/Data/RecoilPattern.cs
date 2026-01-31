using UnityEngine;

namespace CounterSiege
{
    [CreateAssetMenu(fileName = "RecoilPattern", menuName = "Counter Siege/Recoil Pattern")]
    public class RecoilPattern : ScriptableObject
    {
        public Vector2[] pattern = new Vector2[]
        {
            new(0, 0.5f), new(0, 0.6f), new(0, 0.7f),
            new(-0.1f, 0.8f), new(0.1f, 0.9f), new(0.2f, 0.7f),
            new(-0.2f, 0.6f), new(-0.3f, 0.5f), new(0.3f, 0.4f),
            new(0.2f, 0.3f)
        };
        public float recoveryRate = 5f;

        public Vector2 GetOffset(int shotIndex)
        {
            if (pattern == null || pattern.Length == 0) return Vector2.zero;
            return pattern[Mathf.Min(shotIndex, pattern.Length - 1)];
        }
    }
}
