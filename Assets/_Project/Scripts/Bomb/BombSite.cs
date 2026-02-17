using UnityEngine;

namespace CounterSiege
{
    [RequireComponent(typeof(Collider))]
    public class BombSite : MonoBehaviour
    {
        public string siteId = "A";

        void Awake()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
            gameObject.tag = "BombSite";
        }
    }
}
