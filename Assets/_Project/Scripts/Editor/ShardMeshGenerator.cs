using System.IO;
using UnityEditor;
using UnityEngine;

namespace CounterSiege.EditorTools
{
    // Bakes a small pool of jagged convex icosahedron shards for
    // RubbleDestructible to use instead of cube primitives. Run once via
    // "Counter Siege / Generate Shard Meshes", then drag the assets onto
    // RubbleDestructible.shardMeshes. Output is fitted to a 1x1x1 box so
    // chunkSize math keeps working.
    public static class ShardMeshGenerator
    {
        const string OutputDir = "Assets/_Project/Meshes/Shards";
        const int ShardCount = 8;

        [MenuItem("Counter Siege/Generate Shard Meshes")]
        public static void Generate()
        {
            if (!AssetDatabase.IsValidFolder(OutputDir))
            {
                Directory.CreateDirectory(OutputDir);
                AssetDatabase.Refresh();
            }

            for (int i = 0; i < ShardCount; i++)
            {
                var mesh = BuildShard(seed: i * 137 + 1);
                mesh.name = $"Shard_{i:D2}";
                var path = $"{OutputDir}/Shard_{i:D2}.asset";
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(mesh, path);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Generated {ShardCount} shard meshes in {OutputDir}");
        }

        static Mesh BuildShard(int seed)
        {
            var rng = new System.Random(seed);

            // Icosahedron: 12 verts, 20 tris. Plenty of facets to read as "jagged",
            // well under the 255-vert convex MeshCollider limit.
            float t = (1f + Mathf.Sqrt(5f)) / 2f;
            Vector3[] baseVerts =
            {
                new Vector3(-1,  t,  0).normalized, new Vector3( 1,  t,  0).normalized,
                new Vector3(-1, -t,  0).normalized, new Vector3( 1, -t,  0).normalized,
                new Vector3( 0, -1,  t).normalized, new Vector3( 0,  1,  t).normalized,
                new Vector3( 0, -1, -t).normalized, new Vector3( 0,  1, -t).normalized,
                new Vector3( t,  0, -1).normalized, new Vector3( t,  0,  1).normalized,
                new Vector3(-t,  0, -1).normalized, new Vector3(-t,  0,  1).normalized,
            };

            int[] tris =
            {
                0,11,5,  0,5,1,  0,1,7,  0,7,10, 0,10,11,
                1,5,9,   5,11,4, 11,10,2, 10,7,6, 7,1,8,
                3,9,4,   3,4,2,  3,2,6,   3,6,8,  3,8,9,
                4,9,5,   2,4,11, 6,2,10,  8,6,7,  9,8,1
            };

            // Push each vertex along its normal; keeps the shape convex
            // (required for MeshCollider on a Rigidbody).
            var verts = new Vector3[baseVerts.Length];
            for (int i = 0; i < verts.Length; i++)
            {
                float r = 0.4f + (float)rng.NextDouble() * 0.6f;
                verts[i] = baseVerts[i] * r;
            }

            // Rescale so the largest |coord| is 0.5 → bounds match a unit cube,
            // so RubbleDestructible's chunkSize (which assumes unit-cube chunks)
            // produces shards of the intended size.
            float maxExtent = 0f;
            for (int i = 0; i < verts.Length; i++)
            {
                maxExtent = Mathf.Max(maxExtent,
                    Mathf.Abs(verts[i].x), Mathf.Abs(verts[i].y), Mathf.Abs(verts[i].z));
            }
            float scale = 0.5f / maxExtent;
            for (int i = 0; i < verts.Length; i++) verts[i] *= scale;

            // Cheap spherical UVs so any textured material samples something
            // plausible. Debris flies off in a few seconds; seam artifacts at
            // the back of the sphere don't matter.
            var uvs = new Vector2[verts.Length];
            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 n = baseVerts[i];
                uvs[i] = new Vector2(
                    0.5f + Mathf.Atan2(n.z, n.x) / (2f * Mathf.PI),
                    0.5f - Mathf.Asin(n.y) / Mathf.PI);
            }

            var mesh = new Mesh { name = "Shard" };
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
