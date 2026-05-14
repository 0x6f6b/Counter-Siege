// AI Tool: Anthropic Claude Opus 4.6 (Claude Code CLI)
// Prompt: "Plan the architecture for a CS:GO-style tactical FPS in Unity 6 with round-based
//          gameplay, economy system, and bot AI. Use an event-driven architecture with
//          ScriptableObject data and prefab-based spawning."
// Modifications: Added character model setup with team-specific prefabs, added URP camera
//                stack configuration, integrated with TeamManager/EconomyManager/ScoreManager.

using UnityEngine;
using UnityEngine.SceneManagement;

namespace CounterSiege
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public static Team PlayerTeam = Team.Terrorist;

        public GameSettings settings;
        public WeaponDatabase weaponDatabase;
        public UnityEngine.InputSystem.InputActionAsset inputActions;

        [Header("Prefabs")]
        public GameObject playerPrefab;
        public GameObject botPrefab;
        public GameObject hudPrefab;

        [Header("Character Models")]
        public GameObject ctModelPrefab;
        public GameObject tModelPrefab;
        public RuntimeAnimatorController characterAnimController;

        [Header("First Person")]
        public Vector3 firstPersonModelOffset = new Vector3(0.15f, 0f, 0f);

        [HideInInspector] public TeamManager teamManager;
        [HideInInspector] public RoundManager roundManager;
        [HideInInspector] public EconomyManager economyManager;
        [HideInInspector] public ScoreManager scoreManager;
        [HideInInspector] public GameObject playerObject;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            teamManager = GetComponent<TeamManager>();
            roundManager = GetComponent<RoundManager>();
            economyManager = GetComponent<EconomyManager>();
            scoreManager = GetComponent<ScoreManager>();

            // Instantiate HUD if prefab is assigned
            if (hudPrefab != null && HUDController.Instance == null)
                Instantiate(hudPrefab);

            gameObject.AddComponent<EscapeMenuUI>();

            InitializeMatch();
        }

        void InitializeMatch()
        {
            // Spawn player
            playerObject = SpawnPlayer(PlayerTeam);

            // Spawn teammate bots
            for (int i = 0; i < settings.playersPerTeam - 1; i++)
                SpawnBot(PlayerTeam, $"Bot_{PlayerTeam}_{i + 1}");

            // Spawn enemy bots
            Team enemyTeam = PlayerTeam == Team.Terrorist ? Team.CounterTerrorist : Team.Terrorist;
            for (int i = 0; i < settings.playersPerTeam; i++)
                SpawnBot(enemyTeam, $"Bot_{enemyTeam}_{i + 1}");

            // Start match
            roundManager.StartMatch();
        }

        GameObject SpawnPlayer(Team team)
        {
            Vector3 spawnPos = teamManager.GetSpawnPosition(team);

            GameObject go;
            if (playerPrefab != null)
            {
                go = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
                go.name = "Player";
            }
            else
            {
                go = SpawnPlayerLegacy(team, spawnPos);
                return go;
            }

            // Reparent existing scene camera into CameraHolder if needed
            var prefabCam = go.GetComponentInChildren<Camera>();
            var existingCam = Camera.main;
            if (existingCam != null && prefabCam != null && existingCam != prefabCam)
            {
                // Use the prefab's camera, destroy the scene one
                Destroy(existingCam.gameObject);
            }

            // Ensure weapon camera exists for view model rendering (URP camera stack)
            var mainCam = go.GetComponentInChildren<Camera>();
            if (mainCam != null && mainCam.transform.Find("WeaponCamera") == null)
            {
                mainCam.cullingMask &= ~(1 << 7);

                // Ensure main camera is set as Base
                var mainCamData = mainCam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                if (mainCamData == null)
                    mainCamData = mainCam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                mainCamData.renderType = UnityEngine.Rendering.Universal.CameraRenderType.Base;

                var weaponCamObj = new GameObject("WeaponCamera");
                weaponCamObj.transform.SetParent(mainCam.transform);
                weaponCamObj.transform.localPosition = Vector3.zero;
                weaponCamObj.transform.localRotation = Quaternion.identity;
                var weaponCam = weaponCamObj.AddComponent<Camera>();
                weaponCam.cullingMask = 1 << 7;
                weaponCam.nearClipPlane = 0.01f;
                weaponCam.fieldOfView = 60f;

                // Set as Overlay camera and add to main camera's stack
                var weaponCamData = weaponCamObj.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                weaponCamData.renderType = UnityEngine.Rendering.Universal.CameraRenderType.Overlay;
                mainCamData.cameraStack.Add(weaponCam);
            }

            // Set up character model
            SetupCharacterModel(go, team);

            // Hide player's own body except arms/gloves
            HideLocalPlayerBody(go);

            // Initialize health
            var ph = go.GetComponent<PlayerHealth>();
            if (ph != null) ph.Initialize(team, "Player");

            // Register
            teamManager.RegisterPlayer(go, team);
            economyManager.RegisterPlayer(go);
            scoreManager.RegisterPlayer(go, "Player", team);

            return go;
        }

        GameObject SpawnBot(Team team, string botName)
        {
            Vector3 spawnPos = teamManager.GetSpawnPosition(team);

            GameObject go;
            if (botPrefab != null)
            {
                go = Instantiate(botPrefab, spawnPos, Quaternion.identity);
                go.name = botName;
            }
            else
            {
                go = SpawnBotLegacy(team, botName, spawnPos);
                return go;
            }

            // Set up character model
            SetupCharacterModel(go, team);

            // Initialize health
            var ph = go.GetComponent<PlayerHealth>();
            if (ph != null) ph.Initialize(team, botName);

            // Register
            teamManager.RegisterPlayer(go, team);
            economyManager.RegisterPlayer(go);
            scoreManager.RegisterPlayer(go, botName, team);

            return go;
        }

        void SetupCharacterModel(GameObject go, Team team)
        {
            var modelPrefab = team == Team.CounterTerrorist ? ctModelPrefab : tModelPrefab;

            // Find and replace the capsule Body with the character model
            var body = go.transform.Find("Body");
            if (body != null)
            {
                var bodyRenderer = body.GetComponent<Renderer>();
                if (bodyRenderer != null)
                    bodyRenderer.enabled = false; // Hide capsule, keep for fallback

                if (modelPrefab != null)
                {
                    body.gameObject.SetActive(false);

                    var model = Instantiate(modelPrefab, go.transform);
                    model.name = "CharacterModel";
                    model.transform.localPosition = Vector3.zero;
                    model.transform.localRotation = Quaternion.identity;

                    // Set all model renderers to Player layer
                    int playerLayer = LayerMask.NameToLayer("Player");
                    if (playerLayer >= 0)
                    {
                        foreach (var r in model.GetComponentsInChildren<Renderer>(true))
                            r.gameObject.layer = playerLayer;
                    }

                    // Remove any colliders from the model (physics handled by CharacterController/CapsuleCollider)
                    foreach (var col in model.GetComponentsInChildren<Collider>(true))
                        Destroy(col);

                    // Tint terrorist team models beige (multiplies with existing texture)
                    if (team == Team.Terrorist)
                    {
                        var tintColor = new Color(1.4f, 1.2f, 0.8f);
                        foreach (var r in model.GetComponentsInChildren<Renderer>(true))
                        {
                            var block = new MaterialPropertyBlock();
                            r.GetPropertyBlock(block);
                            block.SetColor("_BaseColor", tintColor);
                            r.SetPropertyBlock(block);
                        }
                    }

                    // Set up animation
                    var charAnim = go.GetComponent<CharacterAnimator>();
                    if (charAnim == null)
                        charAnim = go.AddComponent<CharacterAnimator>();
                    charAnim.modelRoot = model.transform;
                    charAnim.animator = model.GetComponentInChildren<Animator>();

                    if (charAnim.animator != null && characterAnimController != null)
                        charAnim.animator.runtimeAnimatorController = characterAnimController;
                }
                else
                {
                    // Fallback: color the capsule
                    body.gameObject.SetActive(true);
                    var bodyR = body.GetComponent<Renderer>();
                    if (bodyR != null)
                    {
                        bodyR.enabled = true;
                        bodyR.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                        bodyR.material.color = team == Team.Terrorist
                            ? new Color(0.8f, 0.6f, 0.2f)
                            : new Color(0.2f, 0.4f, 0.8f);
                    }
                }
            }
        }

        void HideLocalPlayerBody(GameObject go)
        {
            var model = go.transform.Find("CharacterModel");
            if (model == null) return;

            // Hide everything except gloves so the local player doesn't see their own body.
            var visibleParts = new System.Collections.Generic.HashSet<string>(
                System.StringComparer.OrdinalIgnoreCase) { "Gloves" };

            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                if (visibleParts.Contains(r.gameObject.name))
                    continue;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            }

            // Offset the model so arms are centered in view
            model.localPosition = new Vector3(0.35f, -0.2f, -0.1f);
            model.localEulerAngles = new Vector3(-5f, 30f, 0f);
        }

        // Legacy fallback: builds player from code (used when no prefab is assigned)
        GameObject SpawnPlayerLegacy(Team team, Vector3 spawnPos)
        {
            var go = new GameObject("Player");
            go.transform.position = spawnPos;
            int playerLayer = LayerMask.NameToLayer("Player");
            go.layer = playerLayer >= 0 ? playerLayer : 0;

            var cc = go.AddComponent<CharacterController>();
            cc.height = 2f;
            cc.radius = 0.5f;
            cc.center = Vector3.up;

            var bodyVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            bodyVisual.name = "Body";
            bodyVisual.transform.SetParent(go.transform);
            bodyVisual.transform.localPosition = new Vector3(0, 1, 0);
            bodyVisual.layer = playerLayer >= 0 ? playerLayer : 0;
            Destroy(bodyVisual.GetComponent<Collider>());
            var bodyR = bodyVisual.GetComponent<Renderer>();
            bodyR.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            bodyR.material.color = team == Team.Terrorist ? new Color(0.8f, 0.6f, 0.2f) : new Color(0.2f, 0.4f, 0.8f);

            var headObj = new GameObject("HeadCollider");
            headObj.transform.SetParent(go.transform);
            headObj.transform.localPosition = new Vector3(0, 1.85f, 0);
            headObj.tag = "Head";
            headObj.layer = playerLayer >= 0 ? playerLayer : 0;
            var headCol = headObj.AddComponent<SphereCollider>();
            headCol.radius = 0.15f;

            var camHolder = new GameObject("CameraHolder");
            camHolder.transform.SetParent(go.transform);
            camHolder.transform.localPosition = new Vector3(0, 1.6f, 0);

            var existingCam = Camera.main;
            if (existingCam != null)
            {
                existingCam.transform.SetParent(camHolder.transform);
                existingCam.transform.localPosition = Vector3.zero;
                existingCam.transform.localRotation = Quaternion.identity;
            }
            else
            {
                var camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                camObj.transform.SetParent(camHolder.transform);
                camObj.transform.localPosition = Vector3.zero;
                camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
            }

            var cam = go.GetComponentInChildren<Camera>();
            cam.cullingMask &= ~(1 << 7); // Exclude WeaponViewModel layer

            // Weapon camera (URP overlay, renders only WeaponViewModel layer on top)
            var camData = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if (camData == null)
                camData = cam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            camData.renderType = UnityEngine.Rendering.Universal.CameraRenderType.Base;

            var weaponCamObj = new GameObject("WeaponCamera");
            weaponCamObj.transform.SetParent(cam.transform);
            weaponCamObj.transform.localPosition = Vector3.zero;
            weaponCamObj.transform.localRotation = Quaternion.identity;
            var weaponCam = weaponCamObj.AddComponent<Camera>();
            weaponCam.cullingMask = 1 << 7;
            weaponCam.nearClipPlane = 0.01f;
            weaponCam.fieldOfView = 60f;

            var weaponCamData = weaponCamObj.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            weaponCamData.renderType = UnityEngine.Rendering.Universal.CameraRenderType.Overlay;
            camData.cameraStack.Add(weaponCam);

            var weaponHolder = new GameObject("WeaponHolder");
            weaponHolder.transform.SetParent(cam.transform);
            weaponHolder.transform.localPosition = new Vector3(0.3f, -0.2f, 0.5f);

            go.AddComponent<PlayerController>();
            go.AddComponent<PlayerLook>();
            var ph = go.AddComponent<PlayerHealth>();
            ph.Initialize(team, "Player");

            var inv = go.AddComponent<PlayerInventory>();
            inv.weaponHolder = weaponHolder.transform;

            go.AddComponent<PlayerEconomy>();
            go.AddComponent<PlayerInteraction>();
            go.AddComponent<PlayerInputHandler>();

            teamManager.RegisterPlayer(go, team);
            economyManager.RegisterPlayer(go);
            scoreManager.RegisterPlayer(go, "Player", team);

            return go;
        }

        // Legacy fallback: builds bot from code (used when no prefab is assigned)
        GameObject SpawnBotLegacy(Team team, string botName, Vector3 spawnPos)
        {
            var go = new GameObject(botName);
            go.transform.position = spawnPos;
            int playerLayer = LayerMask.NameToLayer("Player");
            go.layer = playerLayer >= 0 ? playerLayer : 0;

            var agent = go.AddComponent<UnityEngine.AI.NavMeshAgent>();
            agent.height = 2f;
            agent.radius = 0.5f;
            agent.speed = 5.2f;
            agent.angularSpeed = 360f;
            agent.acceleration = 20f;
            agent.stoppingDistance = 0.5f;
            agent.baseOffset = 0f;

            var bodyCol = go.AddComponent<CapsuleCollider>();
            bodyCol.height = 2f;
            bodyCol.radius = 0.4f;
            bodyCol.center = Vector3.up;

            var bodyVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            bodyVisual.name = "Body";
            bodyVisual.transform.SetParent(go.transform);
            bodyVisual.transform.localPosition = new Vector3(0, 1, 0);
            bodyVisual.layer = playerLayer >= 0 ? playerLayer : 0;
            Destroy(bodyVisual.GetComponent<Collider>());
            var bodyR = bodyVisual.GetComponent<Renderer>();
            bodyR.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            bodyR.material.color = team == Team.Terrorist ? new Color(0.8f, 0.6f, 0.2f) : new Color(0.2f, 0.4f, 0.8f);

            var headObj = new GameObject("HeadCollider");
            headObj.transform.SetParent(go.transform);
            headObj.transform.localPosition = new Vector3(0, 1.85f, 0);
            headObj.tag = "Head";
            headObj.layer = playerLayer >= 0 ? playerLayer : 0;
            var headCol = headObj.AddComponent<SphereCollider>();
            headCol.radius = 0.15f;

            var ph = go.AddComponent<PlayerHealth>();
            ph.Initialize(team, botName);

            var weaponHolder = new GameObject("WeaponHolder");
            weaponHolder.transform.SetParent(go.transform);
            weaponHolder.transform.localPosition = new Vector3(0.3f, 1.5f, 0.5f);

            var inv = go.AddComponent<PlayerInventory>();
            inv.weaponHolder = weaponHolder.transform;

            go.AddComponent<PlayerEconomy>();
            go.AddComponent<BotSensors>();
            go.AddComponent<BotAimController>();
            go.AddComponent<BotController>();

            teamManager.RegisterPlayer(go, team);
            economyManager.RegisterPlayer(go);
            scoreManager.RegisterPlayer(go, botName, team);

            return go;
        }

        public void RespawnAll()
        {
            // Reset spawn indices so positions are reused properly
            teamManager.ResetSpawnIndices();

            foreach (var player in teamManager.GetAllPlayers())
            {
                var ph = player.GetComponent<PlayerHealth>();
                if (ph == null) continue;

                Vector3 spawnPos = teamManager.GetSpawnPosition(ph.team);

                // Disable CharacterController before teleporting (it blocks transform.position changes)
                var cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;

                // Disable NavMeshAgent before teleporting
                var agent = player.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (agent != null) agent.enabled = false;

                player.transform.position = spawnPos;

                // Re-enable CharacterController
                if (cc != null) cc.enabled = true;

                // Re-enable NavMeshAgent and warp
                if (agent != null)
                {
                    agent.enabled = true;
                    agent.Warp(spawnPos);
                }

                ph.ResetHealth();

                // Re-enable components
                var pc = player.GetComponent<PlayerController>();
                if (pc != null) pc.enabled = true;
                var pl = player.GetComponent<PlayerLook>();
                if (pl != null) pl.enabled = true;

                foreach (var col in player.GetComponentsInChildren<Collider>(true))
                    col.enabled = true;
                foreach (var r in player.GetComponentsInChildren<Renderer>(true))
                    r.enabled = true;

                // Give default loadout
                var inv = player.GetComponent<PlayerInventory>();
                if (inv != null)
                    inv.GiveDefaultLoadout(ph.team);
            }
        }

        public void EndMatch(Team winner)
        {
            EventBus.OnMatchWon?.Invoke(winner);
        }

        public void ReturnToMenu()
        {
            Instance = null;
            SceneManager.LoadScene("MainMenu");
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                EventBus.Reset();
            }
        }
    }
}
