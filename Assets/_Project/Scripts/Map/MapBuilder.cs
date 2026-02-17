using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

namespace CounterSiege
{
    public class MapBuilder : MonoBehaviour
    {
        Material floorMat;
        Material wallMat;
        Material crateMat;
        Material aSiteMat;
        Material bSiteMat;

        void Awake()
        {
            CreateMaterials();
            BuildMap();
            BakeNavMesh();
        }

        void CreateMaterials()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");

            floorMat = new Material(shader);
            floorMat.color = new Color(0.7f, 0.7f, 0.7f);

            wallMat = new Material(shader);
            wallMat.color = new Color(0.4f, 0.4f, 0.45f);

            crateMat = new Material(shader);
            crateMat.color = new Color(0.6f, 0.45f, 0.25f);

            aSiteMat = new Material(shader);
            aSiteMat.color = new Color(0.8f, 0.3f, 0.3f, 0.5f);

            bSiteMat = new Material(shader);
            bSiteMat.color = new Color(0.3f, 0.3f, 0.8f, 0.5f);
        }

        void BuildMap()
        {
            var mapParent = new GameObject("Map");
            mapParent.isStatic = true;

            // ============ MAIN FLOOR ============
            CreateFloor(mapParent.transform, Vector3.zero, new Vector3(150, 1, 150));

            // ============ OUTER WALLS ============
            // North
            CreateWall(mapParent.transform, new Vector3(0, 2, 75), new Vector3(150, 4, 1));
            // South
            CreateWall(mapParent.transform, new Vector3(0, 2, -75), new Vector3(150, 4, 1));
            // East
            CreateWall(mapParent.transform, new Vector3(75, 2, 0), new Vector3(1, 4, 150));
            // West
            CreateWall(mapParent.transform, new Vector3(-75, 2, 0), new Vector3(1, 4, 150));

            // ============ T SPAWN AREA (South) ============
            // T Spawn is at z = -60
            CreateSpawnPoints(Team.Terrorist, new Vector3(0, 0.5f, -60), 3f);

            // ============ CT SPAWN AREA (North) ============
            // CT Spawn is at z = 60
            CreateSpawnPoints(Team.CounterTerrorist, new Vector3(0, 0.5f, 60), 3f);

            // ============ A SITE (Northwest) ============
            // A Site platform/marker
            var aSite = CreateBox(mapParent.transform, new Vector3(-30, 0.05f, 40), new Vector3(15, 0.1f, 15), aSiteMat);
            aSite.name = "BombSiteA";
            aSite.AddComponent<BoxCollider>().isTrigger = true;
            aSite.GetComponent<BoxCollider>().size = new Vector3(1, 20, 1);
            var bsA = aSite.AddComponent<BombSite>();
            bsA.siteId = "A";

            // A Site crates for cover
            CreateCrate(mapParent.transform, new Vector3(-33, 1, 43), new Vector3(2, 2, 2));
            CreateCrate(mapParent.transform, new Vector3(-27, 1, 37), new Vector3(2, 2, 2));
            CreateCrate(mapParent.transform, new Vector3(-25, 1, 43), new Vector3(3, 1.5f, 1.5f));

            // ============ B SITE (Northeast) ============
            var bSite = CreateBox(mapParent.transform, new Vector3(30, 0.05f, 40), new Vector3(15, 0.1f, 15), bSiteMat);
            bSite.name = "BombSiteB";
            bSite.AddComponent<BoxCollider>().isTrigger = true;
            bSite.GetComponent<BoxCollider>().size = new Vector3(1, 20, 1);
            var bsB = bSite.AddComponent<BombSite>();
            bsB.siteId = "B";

            // B Site crates
            CreateCrate(mapParent.transform, new Vector3(33, 1, 43), new Vector3(2, 2, 2));
            CreateCrate(mapParent.transform, new Vector3(27, 1, 37), new Vector3(2, 2, 2));
            CreateCrate(mapParent.transform, new Vector3(32, 1, 38), new Vector3(1.5f, 1.5f, 3));

            // ============ MID AREA ============
            // Mid corridor walls
            CreateWall(mapParent.transform, new Vector3(-8, 2, 0), new Vector3(1, 4, 40)); // Left mid wall
            CreateWall(mapParent.transform, new Vector3(8, 2, 0), new Vector3(1, 4, 40));  // Right mid wall

            // Mid doors (gap in walls)
            CreateWall(mapParent.transform, new Vector3(-8, 2, -25), new Vector3(1, 4, 10));
            CreateWall(mapParent.transform, new Vector3(8, 2, -25), new Vector3(1, 4, 10));

            // Mid crates
            CreateCrate(mapParent.transform, new Vector3(0, 1, 5), new Vector3(2, 2, 2));
            CreateCrate(mapParent.transform, new Vector3(3, 1, -5), new Vector3(2, 2, 2));

            // ============ A LONG (West side) ============
            // Long walls
            CreateWall(mapParent.transform, new Vector3(-40, 2, -20), new Vector3(1, 4, 30));
            CreateWall(mapParent.transform, new Vector3(-20, 2, -20), new Vector3(1, 4, 30));

            // A Long to A Site connector
            CreateWall(mapParent.transform, new Vector3(-40, 2, 20), new Vector3(1, 4, 30));

            // Long corner cover
            CreateCrate(mapParent.transform, new Vector3(-30, 1, -10), new Vector3(2, 2, 2));
            CreateCrate(mapParent.transform, new Vector3(-35, 1, 0), new Vector3(2, 2, 3));

            // ============ B TUNNELS (East side) ============
            CreateWall(mapParent.transform, new Vector3(20, 2, -20), new Vector3(1, 4, 30));
            CreateWall(mapParent.transform, new Vector3(40, 2, -20), new Vector3(1, 4, 30));

            // B Tunnels to B Site connector
            CreateWall(mapParent.transform, new Vector3(40, 2, 20), new Vector3(1, 4, 30));

            // Tunnel crates
            CreateCrate(mapParent.transform, new Vector3(30, 1, -10), new Vector3(2, 2, 2));
            CreateCrate(mapParent.transform, new Vector3(25, 1, 5), new Vector3(3, 2, 2));

            // ============ CATWALKS / CONNECTIONS ============
            // Catwalk from mid to A
            CreateWall(mapParent.transform, new Vector3(-15, 2, 25), new Vector3(10, 4, 1));

            // Catwalk from mid to B
            CreateWall(mapParent.transform, new Vector3(15, 2, 25), new Vector3(10, 4, 1));

            // CT Ramp walls
            CreateWall(mapParent.transform, new Vector3(-15, 2, 50), new Vector3(1, 4, 15));
            CreateWall(mapParent.transform, new Vector3(15, 2, 50), new Vector3(1, 4, 15));

            // ============ ADDITIONAL COVER ============
            // A Lobby area
            CreateCrate(mapParent.transform, new Vector3(-25, 1, -35), new Vector3(2, 2, 2));

            // B Lobby area
            CreateCrate(mapParent.transform, new Vector3(25, 1, -35), new Vector3(2, 2, 2));

            // Mid cross cover
            CreateCrate(mapParent.transform, new Vector3(-5, 1, 15), new Vector3(1.5f, 2, 1.5f));
            CreateCrate(mapParent.transform, new Vector3(5, 1, 15), new Vector3(1.5f, 2, 1.5f));

            // CT area cover
            CreateCrate(mapParent.transform, new Vector3(-10, 1, 55), new Vector3(2, 2, 2));
            CreateCrate(mapParent.transform, new Vector3(10, 1, 55), new Vector3(2, 2, 2));

            // Add NavMeshSurface
            var navSurface = mapParent.AddComponent<NavMeshSurface>();
            navSurface.collectObjects = CollectObjects.Children;
        }

        void CreateSpawnPoints(Team team, Vector3 center, float spacing)
        {
            for (int i = 0; i < 5; i++)
            {
                float xOffset = (i - 2) * spacing;
                var sp = new GameObject($"SpawnPoint_{team}_{i}");
                sp.transform.position = center + new Vector3(xOffset, 0, 0);
                sp.transform.rotation = Quaternion.Euler(0, team == Team.Terrorist ? 0 : 180, 0);
                var spawnPoint = sp.AddComponent<SpawnPoint>();
                spawnPoint.team = team;
            }
        }

        GameObject CreateFloor(Transform parent, Vector3 pos, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Floor";
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(pos.x, -0.5f, pos.z);
            go.transform.localScale = new Vector3(scale.x, 1, scale.z);
            go.isStatic = true;
            go.GetComponent<Renderer>().material = floorMat;
            return go;
        }

        GameObject CreateWall(Transform parent, Vector3 pos, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Wall";
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.isStatic = true;
            go.GetComponent<Renderer>().material = wallMat;
            return go;
        }

        GameObject CreateCrate(Transform parent, Vector3 pos, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Crate";
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.isStatic = true;
            go.GetComponent<Renderer>().material = crateMat;
            return go;
        }

        GameObject CreateBox(Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().material = mat;
            Destroy(go.GetComponent<Collider>());
            return go;
        }

        void BakeNavMesh()
        {
            var surface = FindAnyObjectByType<NavMeshSurface>();
            if (surface != null)
                surface.BuildNavMesh();
        }
    }
}
