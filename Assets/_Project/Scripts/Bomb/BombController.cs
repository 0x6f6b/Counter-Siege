using UnityEngine;

namespace CounterSiege
{
    public class BombController : MonoBehaviour
    {
        public BombState bombState = BombState.Carried;
        float plantProgress;
        float defuseProgress;
        float detonationTimer;
        float plantTime = 3.2f;
        float defuseTime = 10f;
        GameObject planter;
        GameObject defuser;
        BombSite site;

        void Start()
        {
            var settings = GameManager.Instance?.settings;
            if (settings != null)
            {
                plantTime = settings.plantTime;
                defuseTime = settings.defuseTime;
            }
        }

        void Update()
        {
            switch (bombState)
            {
                case BombState.Planting:
                    plantProgress += Time.deltaTime;
                    if (plantProgress >= plantTime)
                        CompletePlant();
                    break;

                case BombState.Planted:
                    detonationTimer -= Time.deltaTime;
                    EventBus.OnBombTimerTick?.Invoke(detonationTimer);
                    if (detonationTimer <= 0)
                        Explode();
                    break;

                case BombState.Defusing:
                    defuseProgress += Time.deltaTime;
                    if (defuseProgress >= defuseTime)
                        CompleteDefuse();
                    break;
            }
        }

        public void StartPlant(GameObject planter, BombSite site)
        {
            if (bombState != BombState.Carried) return;
            this.planter = planter;
            this.site = site;
            bombState = BombState.Planting;
            plantProgress = 0f;
            EventBus.OnBombStateChanged?.Invoke("Planting...");
        }

        public void CancelPlant()
        {
            if (bombState != BombState.Planting) return;
            bombState = BombState.Carried;
            plantProgress = 0f;
            EventBus.OnBombStateChanged?.Invoke("");
        }

        void CompletePlant()
        {
            bombState = BombState.Planted;
            var settings = GameManager.Instance?.settings;
            detonationTimer = settings != null ? settings.bombTimer : 40f;

            // Position on ground at site
            if (site != null)
                transform.position = site.transform.position + Vector3.up * 0.1f;

            EventBus.OnBombPlanted?.Invoke(planter);
            EventBus.OnBombStateChanged?.Invoke("BOMB PLANTED");
        }

        public void StartDefuse(GameObject defuser)
        {
            if (bombState != BombState.Planted) return;
            this.defuser = defuser;
            bombState = BombState.Defusing;
            defuseProgress = 0f;
            EventBus.OnBombStateChanged?.Invoke("Defusing...");
        }

        public void CancelDefuse()
        {
            if (bombState != BombState.Defusing) return;
            bombState = BombState.Planted;
            defuseProgress = 0f;
            EventBus.OnBombStateChanged?.Invoke("BOMB PLANTED");
        }

        void CompleteDefuse()
        {
            bombState = BombState.Defused;
            EventBus.OnBombDefused?.Invoke(defuser);
            EventBus.OnBombStateChanged?.Invoke("BOMB DEFUSED");
        }

        void Explode()
        {
            bombState = BombState.Exploded;

            var settings = GameManager.Instance?.settings;
            float damage = settings != null ? settings.bombDamage : 500f;
            float radius = settings != null ? settings.bombRadius : 30f;

            // Damage all players in radius
            Collider[] hits = Physics.OverlapSphere(transform.position, radius);
            foreach (var hit in hits)
            {
                var ph = hit.GetComponentInParent<PlayerHealth>();
                if (ph != null && !ph.isDead)
                {
                    float dist = Vector3.Distance(transform.position, ph.transform.position);
                    float falloff = 1f - (dist / radius);
                    float finalDamage = damage * Mathf.Max(0, falloff);

                    var dmgInfo = new DamageInfo(finalDamage, null, HitZone.Chest, "Bomb", 1f);
                    ph.TakeDamage(dmgInfo);
                }
            }

            EventBus.OnBombExploded?.Invoke();
            EventBus.OnBombStateChanged?.Invoke("BOMB EXPLODED");

            // Simple explosion visual
            var explosionVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            explosionVisual.transform.position = transform.position;
            explosionVisual.transform.localScale = Vector3.one * 5f;
            Destroy(explosionVisual.GetComponent<Collider>());
            var r = explosionVisual.GetComponent<Renderer>();
            r.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            r.material.color = new Color(1f, 0.5f, 0f, 0.8f);
            Destroy(explosionVisual, 1f);
        }

        public float PlantProgress => plantProgress / plantTime;
        public float DefuseProgress => defuseProgress / defuseTime;
        public float DetonationTimer => detonationTimer;
    }
}
