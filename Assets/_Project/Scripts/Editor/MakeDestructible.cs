using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using CounterSiege;

namespace CounterSiege.EditorTools
{
    // Adds OpenFracture + Destructible to selected scene GameObjects so they
    // can be shot apart at runtime. Hollow/non-manifold meshes (corrugated
    // roofs, pipes) tend to fragment badly. Source FBX needs Read/Write; this
    // tool flips that flag automatically. Parent must be uniformly scaled.
    public static class MakeDestructible
    {
        const int DefaultHealth = 60;
        const int DefaultFragmentCount = 5;   // Lower = higher success rate on bad meshes

        [MenuItem("Counter Siege/Make Selection Destructible %#d")]
        public static void ApplyToSelection()
        {
            var targets = Selection.gameObjects;
            if (targets == null || targets.Length == 0)
            {
                EditorUtility.DisplayDialog("Make Destructible",
                    "Select one or more GameObjects in the scene first.", "OK");
                return;
            }

            int ok = 0, skipped = 0;
            foreach (var go in targets)
            {
                if (MakeOne(go)) ok++;
                else skipped++;
            }
            Debug.Log($"[MakeDestructible] applied={ok} skipped={skipped}");
            EditorUtility.DisplayDialog("Make Destructible",
                $"Applied to {ok} object(s). Skipped {skipped}.\n\n" +
                "Shoot them in Play mode to fracture. Non-manifold meshes may " +
                "produce odd fragments; try lower fragment counts on those.",
                "OK");
        }

        [MenuItem("Counter Siege/Make Selection Destructible (Rubble) %#&d")]
        public static void ApplyRubbleToSelection()
        {
            var targets = Selection.gameObjects;
            if (targets == null || targets.Length == 0)
            {
                EditorUtility.DisplayDialog("Make Destructible (Rubble)",
                    "Select one or more GameObjects in the scene first.", "OK");
                return;
            }

            int ok = 0, skipped = 0;
            foreach (var go in targets)
            {
                if (MakeRubble(go)) ok++;
                else skipped++;
            }
            Debug.Log($"[MakeDestructible/Rubble] applied={ok} skipped={skipped}");
        }

        [MenuItem("Counter Siege/Remove Destructible From Selection")]
        public static void RemoveFromSelection()
        {
            int removed = 0;
            foreach (var go in Selection.gameObjects)
            {
                var d = go.GetComponent<Destructible>();
                if (d != null) { Object.DestroyImmediate(d); removed++; }
                var f = go.GetComponent<Fracture>();
                if (f != null) { Object.DestroyImmediate(f); removed++; }
                var r = go.GetComponent<RubbleDestructible>();
                if (r != null) { Object.DestroyImmediate(r); removed++; }
            }
            Debug.Log($"[MakeDestructible] removed {removed} components");
        }

        static bool MakeRubble(GameObject go)
        {
            if (go.GetComponent<RubbleDestructible>() != null)
            {
                Debug.Log($"[MakeDestructible] {go.name} already has RubbleDestructible; skipped.");
                return false;
            }
            // Make sure shots can hit it
            var col = go.GetComponentInChildren<Collider>();
            if (col == null)
            {
                var mf = go.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    var mc = go.AddComponent<MeshCollider>();
                    // Non-convex is fine because there's no Rigidbody on the source
                }
            }
            var rd = go.AddComponent<RubbleDestructible>();
            rd.maxHealth = DefaultHealth;
            EditorUtility.SetDirty(go);
            return true;
        }

        static bool MakeOne(GameObject go)
        {
            var mf = go.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null)
            {
                Debug.LogWarning($"[MakeDestructible] {go.name} has no MeshFilter/mesh; skipped.");
                return false;
            }

            // Already destructible
            if (go.GetComponent<Destructible>() != null && go.GetComponent<Fracture>() != null)
            {
                Debug.Log($"[MakeDestructible] {go.name} already destructible; skipped.");
                return false;
            }

            EnsureMeshReadable(mf.sharedMesh);
            EnsureUniformParentScale(go);
            EnsureRigidbody(go);
            EnsureConvexCollider(go);

            var fracture = go.GetComponent<Fracture>();
            if (fracture == null) fracture = go.AddComponent<Fracture>();

            // Initialize options (the constructors fill in defaults)
            fracture.fractureOptions = new FractureOptions
            {
                fragmentCount = DefaultFragmentCount,
                xAxis = true,
                yAxis = true,
                zAxis = true,
                detectFloatingFragments = false,
                asynchronous = false,   // sync = errors throw loudly instead of silent fail
                textureScale = Vector2.one,
                textureOffset = Vector2.zero,
                insideMaterial = FindOrCreateInsideMaterial()
            };

            WarnIfMeshUnsuitable(go, mf.sharedMesh);
            fracture.triggerOptions = new TriggerOptions
            {
                triggerType = TriggerType.Keyboard,   // disables auto-fracture; Destructible calls it manually
                minimumCollisionForce = 999999f,
                filterCollisionsByTag = false,
                triggerAllowedTags = new List<string>(),
                triggerKey = KeyCode.None
            };
            fracture.refractureOptions = new RefractureOptions
            {
                enableRefracturing = false,
                maxRefractureCount = 0,
                invokeCallbacks = false
            };
            fracture.callbackOptions = new CallbackOptions();

            var dest = go.GetComponent<Destructible>();
            if (dest == null) dest = go.AddComponent<Destructible>();
            dest.maxHealth = DefaultHealth;
            dest.fragmentLifetime = 6f;

            EditorUtility.SetDirty(go);
            return true;
        }

        static void EnsureMeshReadable(Mesh mesh)
        {
            if (mesh.isReadable) return;
            var path = AssetDatabase.GetAssetPath(mesh);
            if (string.IsNullOrEmpty(path)) return;
            var imp = AssetImporter.GetAtPath(path);
            if (imp is ModelImporter mi)
            {
                if (!mi.isReadable)
                {
                    mi.isReadable = true;
                    mi.SaveAndReimport();
                }
            }
        }

        static void EnsureUniformParentScale(GameObject go)
        {
            var p = go.transform.parent;
            if (p == null) return;
            var s = p.lossyScale;
            if (Mathf.Abs(s.x - s.y) > 0.001f || Mathf.Abs(s.y - s.z) > 0.001f)
            {
                Debug.LogWarning(
                    $"[MakeDestructible] {go.name} has non-uniformly-scaled parent " +
                    $"(scale={s}). Fragments may render incorrectly. " +
                    "Consider unparenting before fracturing.");
            }
        }

        static void EnsureRigidbody(GameObject go)
        {
            var rb = go.GetComponent<Rigidbody>();
            if (rb == null) rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;   // static prop until destroyed
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        }

        static void EnsureConvexCollider(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col is MeshCollider mc)
            {
                if (!mc.convex) mc.convex = true;
            }
            else if (col == null)
            {
                var newMc = go.AddComponent<MeshCollider>();
                newMc.convex = true;
            }
            // Box/sphere/capsule colliders are fine, leave them.
        }

        static void WarnIfMeshUnsuitable(GameObject go, Mesh mesh)
        {
            // Quick heuristics for whether OpenFracture is likely to fail.
            // We can't cheaply prove a mesh is manifold, but obvious red flags help.
            string warn = null;
            if (mesh.subMeshCount > 1)
                warn = $"{mesh.subMeshCount} submeshes; OpenFracture only fractures submesh 0, the rest vanish.";
            else if (mesh.vertexCount > 5000)
                warn = $"{mesh.vertexCount} vertices; high-poly meshes often fracture slowly or fail.";
            else if (mesh.triangles.Length < 36)
                warn = $"only {mesh.triangles.Length / 3} triangles; too simple to fracture cleanly.";

            if (warn != null)
            {
                Debug.LogWarning($"[MakeDestructible] {go.name} mesh is risky: {warn} " +
                    "Object will still respond to damage but may just disappear instead of fracturing. " +
                    "Best fracture targets: small solid props (crates, barrels, boxes).");
            }
        }

        static Material insideMatCache;
        static Material FindOrCreateInsideMaterial()
        {
            if (insideMatCache != null) return insideMatCache;
            const string path = "Assets/_Project/Materials/Destructibles/Mat_Inside_Generic.mat";
            insideMatCache = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (insideMatCache != null) return insideMatCache;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            insideMatCache = new Material(shader) { name = "Mat_Inside_Generic" };
            var c = new Color(0.18f, 0.15f, 0.12f);
            insideMatCache.color = c;
            if (insideMatCache.HasProperty("_BaseColor")) insideMatCache.SetColor("_BaseColor", c);
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            AssetDatabase.CreateAsset(insideMatCache, path);
            return insideMatCache;
        }
    }
}
