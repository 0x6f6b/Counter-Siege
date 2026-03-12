// AI Tool: Anthropic Claude Opus 4.6 (Claude Code CLI)
// Prompt: "Backup destructible for meshes OpenFracture can't handle - just
//          break the bounding box into a grid of physics chunks on death."
// Modifications: Added the persistDebris mode for permanent rubble, added
//                shard-mesh pool option, tuned chunkFill to stop rotated
//                chunks overlapping their neighbours.

using UnityEngine;

namespace CounterSiege
{
    // Grid-of-chunks alternative to Destructible for hollow / non-manifold /
    // multi-submesh assets that OpenFracture can't handle.
    public class RubbleDestructible : MonoBehaviour, IDamageable
    {
        [Header("Health")]
        public int maxHealth = 60;
        [HideInInspector] public int currentHealth;

        [Header("Chunks")]
        [Tooltip("Grid resolution per axis. 3 = 27 chunks, 4 = 64 chunks.")]
        [Range(2, 6)] public int chunksPerAxis = 3;
        [Tooltip("Random offset applied to each chunk for irregularity.")]
        public float jitter = 0.05f;
        [Tooltip("Seconds before each chunk is destroyed. Ignored when persistDebris is true.")]
        public float chunkLifetime = 6f;
        [Tooltip("When true, chunks sleep in place as permanent rubble.")]
        public bool persistDebris = true;
        public float explosionForce = 0.5f;
        public float explosionUpwardModifier = 0.2f;
        [Tooltip("Fraction of each grid cell that the chunk occupies. <0.58 prevents rotated chunks from overlapping neighbours.")]
        [Range(0.3f, 0.85f)] public float chunkFill = 0.55f;
        [Tooltip("Hard ceiling on each chunk's velocity. Without this, Unity 6's per-rigidbody maxLinearVelocity / maxDepenetrationVelocity default to infinity, so an overlap or strong impulse on a 0.1 kg chunk yeets it past view range in one frame.")]
        public float chunkMaxSpeed = 18f;
        [Tooltip("Hard ceiling on the velocity physics may give a chunk when ejecting it out of an overlap. Should be <= chunkMaxSpeed.")]
        public float chunkMaxDepenetrationSpeed = 6f;
        [Tooltip("Minimum mass floor. Higher = chunks resist impulses more. Bumped from 0.1 because Δv = J/m sends ultra-light chunks supersonic.")]
        public float chunkMinMass = 1f;
        [Tooltip("Jagged shard meshes used for chunks. Bake with 'Counter Siege/Generate Shard Meshes'. If empty, falls back to cubes.")]
        public Mesh[] shardMeshes;

        [Header("Effects")]
        public AudioClip[] impactSounds;
        public AudioClip destroySound;
        public GameObject destroyVFX;

        bool isDead;

        void Awake() { currentHealth = maxHealth; }

        public void TakeDamage(DamageInfo info)
        {
            if (isDead) return;

            currentHealth = Mathf.Max(0, currentHealth - Mathf.CeilToInt(info.damage));

            if (impactSounds != null && impactSounds.Length > 0 && AudioManager.Instance != null)
            {
                var clip = impactSounds[Random.Range(0, impactSounds.Length)];
                AudioManager.Instance.PlaySFX(clip, transform.position, 0.25f);
            }

            if (currentHealth <= 0) Die();
        }

        void Die()
        {
            isDead = true;

            if (destroySound != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(destroySound, transform.position, 0.6f);

            if (destroyVFX != null)
                Instantiate(destroyVFX, transform.position, Quaternion.identity);

            SpawnChunks();

            // Disable original so its collider doesn't fight the chunks
            foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
            // The husk (this gameObject) is now invisible; either drop it
            // immediately (debris persists, husk would just leak memory) or
            // let it linger as long as the chunks do.
            Destroy(gameObject, persistDebris ? 1f : chunkLifetime + 0.5f);
        }

        void SpawnChunks()
        {
            Bounds bounds = ComputeWorldBounds();
            if (bounds.size == Vector3.zero) return;

            // Pick the renderer's material as the chunk surface so chunks read as
            // "this thing broken apart" not generic debris.
            Material chunkMat = null;
            var srcRenderer = GetComponentInChildren<Renderer>();
            if (srcRenderer != null) chunkMat = srcRenderer.sharedMaterial;

            Vector3 step = new Vector3(
                bounds.size.x / chunksPerAxis,
                bounds.size.y / chunksPerAxis,
                bounds.size.z / chunksPerAxis);
            Vector3 chunkSize = step * chunkFill; // <0.58 = rotated chunks fit in cell

            // Group all chunks under a single root for tidy cleanup
            var root = new GameObject($"{name}_Rubble");
            root.transform.position = bounds.center;

            for (int x = 0; x < chunksPerAxis; x++)
            for (int y = 0; y < chunksPerAxis; y++)
            for (int z = 0; z < chunksPerAxis; z++)
            {
                Vector3 cellCenter = bounds.min + new Vector3(
                    step.x * (x + 0.5f),
                    step.y * (y + 0.5f),
                    step.z * (z + 0.5f));
                cellCenter += Random.insideUnitSphere * jitter;

                GameObject chunk;
                if (shardMeshes != null && shardMeshes.Length > 0)
                {
                    var shard = shardMeshes[Random.Range(0, shardMeshes.Length)];
                    chunk = new GameObject("Chunk");
                    chunk.AddComponent<MeshFilter>().sharedMesh = shard;
                    chunk.AddComponent<MeshRenderer>();
                    var mc = chunk.AddComponent<MeshCollider>();
                    mc.sharedMesh = shard;
                    mc.convex = true;
                }
                else
                {
                    chunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    chunk.name = "Chunk";
                }
                chunk.transform.SetParent(root.transform);
                chunk.transform.position = cellCenter;
                chunk.transform.rotation = Random.rotation;
                chunk.transform.localScale = chunkSize;

                if (chunkMat != null)
                    chunk.GetComponent<MeshRenderer>().sharedMaterial = chunkMat;

                var rb = chunk.AddComponent<Rigidbody>();
                rb.mass = Mathf.Max(chunkMinMass, chunkSize.x * chunkSize.y * chunkSize.z * 50f);
                // PERSISTENT velocity caps. Unity 6 defaults both of these to
                // infinity, which is what was sending chunks into orbit when
                // overlap depenetration or an explosion impulse hit a light
                // chunk. These caps are enforced by the physics engine itself
                // on every step, regardless of force source.
                rb.maxLinearVelocity = chunkMaxSpeed;
                rb.maxDepenetrationVelocity = chunkMaxDepenetrationSpeed;
                rb.AddExplosionForce(explosionForce, bounds.center, bounds.size.magnitude,
                    explosionUpwardModifier, ForceMode.Impulse);

                if (!persistDebris)
                    Destroy(chunk, chunkLifetime + Random.Range(-0.5f, 0.5f));
            }
            if (!persistDebris)
                Destroy(root, chunkLifetime + 1f);
        }

        Bounds ComputeWorldBounds()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                var b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
                return b;
            }
            var col = GetComponentInChildren<Collider>();
            if (col != null) return col.bounds;
            return new Bounds(transform.position, Vector3.one);
        }
    }
}
