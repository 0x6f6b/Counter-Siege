#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CounterSiege.Editor
{
    public static class AudioSetup
    {
        static readonly string AudioPath = "Assets/_Project/Audio";

        [MenuItem("Counter Siege/Convert/Wire Audio Clips to Weapon Assets")]
        public static void WireWeaponAudio()
        {
            // AK-47
            var ak = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Project/ScriptableObjects/Weapons/AK47.asset");
            if (ak != null)
            {
                ak.fireSound = Load<AudioClip>($"{AudioPath}/Weapons/AK47/ak47_fire.wav");
                ak.reloadSound = Load<AudioClip>($"{AudioPath}/Weapons/AK47/ak47_clipin.wav");
                ak.equipSound = Load<AudioClip>($"{AudioPath}/Weapons/AK47/ak47_draw.wav");
                EditorUtility.SetDirty(ak);
                Debug.Log("Wired AK-47 audio");
            }

            // M4A4
            var m4 = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Project/ScriptableObjects/Weapons/M4A4.asset");
            if (m4 != null)
            {
                m4.fireSound = Load<AudioClip>($"{AudioPath}/Weapons/M4A1/m4a1_fire.wav");
                m4.reloadSound = Load<AudioClip>($"{AudioPath}/Weapons/M4A1/m4a1_clipin.wav");
                m4.equipSound = Load<AudioClip>($"{AudioPath}/Weapons/M4A1/m4a1_draw.wav");
                EditorUtility.SetDirty(m4);
                Debug.Log("Wired M4A4 audio");
            }

            // Glock
            var glock = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Project/ScriptableObjects/Weapons/Glock.asset");
            if (glock != null)
            {
                glock.fireSound = Load<AudioClip>($"{AudioPath}/Weapons/Glock/glock_fire.wav");
                glock.reloadSound = Load<AudioClip>($"{AudioPath}/Weapons/Glock/glock_clipin.wav");
                glock.equipSound = Load<AudioClip>($"{AudioPath}/Weapons/Glock/glock_draw.wav");
                EditorUtility.SetDirty(glock);
                Debug.Log("Wired Glock audio");
            }

            // USP
            var usp = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Project/ScriptableObjects/Weapons/USP.asset");
            if (usp != null)
            {
                usp.fireSound = Load<AudioClip>($"{AudioPath}/Weapons/USP/usp_fire.wav");
                usp.reloadSound = Load<AudioClip>($"{AudioPath}/Weapons/USP/usp_clipin.wav");
                usp.equipSound = Load<AudioClip>($"{AudioPath}/Weapons/USP/usp_draw.wav");
                EditorUtility.SetDirty(usp);
                Debug.Log("Wired USP audio");
            }

            // AWP
            var awp = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Project/ScriptableObjects/Weapons/AWP.asset");
            if (awp != null)
            {
                awp.fireSound = Load<AudioClip>($"{AudioPath}/Weapons/AWP/awp_fire.wav");
                awp.reloadSound = Load<AudioClip>($"{AudioPath}/Weapons/AWP/awp_clipin.wav");
                awp.equipSound = Load<AudioClip>($"{AudioPath}/Weapons/AWP/awp_draw.wav");
                EditorUtility.SetDirty(awp);
                Debug.Log("Wired AWP audio");
            }

            // Knife
            var knife = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Project/ScriptableObjects/Weapons/Knife.asset");
            if (knife != null)
            {
                knife.fireSound = Load<AudioClip>($"{AudioPath}/Weapons/Knife/knife_slash1.wav");
                knife.equipSound = Load<AudioClip>($"{AudioPath}/Weapons/Knife/knife_deploy1.wav");
                knife.impactSounds = new AudioClip[]
                {
                    Load<AudioClip>($"{AudioPath}/Weapons/Knife/knife_hit1.wav"),
                    Load<AudioClip>($"{AudioPath}/Weapons/Knife/knife_stab.wav"),
                };
                EditorUtility.SetDirty(knife);
                Debug.Log("Wired Knife audio");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("All weapon audio clips wired!");
        }

        [MenuItem("Counter Siege/Convert/Setup GameAudio in Scene")]
        public static void SetupGameAudio()
        {
            // Find or create AudioManager
            var audioMgr = Object.FindFirstObjectByType<AudioManager>();
            if (audioMgr == null)
            {
                var go = new GameObject("_AudioManager");
                audioMgr = go.AddComponent<AudioManager>();
                UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Create AudioManager");
            }

            // Add GameAudio component
            var gameAudio = audioMgr.GetComponent<GameAudio>();
            if (gameAudio == null)
                gameAudio = audioMgr.gameObject.AddComponent<GameAudio>();

            // Wire footsteps (concrete for Dust2)
            gameAudio.footstepSounds = new AudioClip[]
            {
                Load<AudioClip>($"{AudioPath}/Player/Footsteps/concrete1.wav"),
                Load<AudioClip>($"{AudioPath}/Player/Footsteps/concrete2.wav"),
                Load<AudioClip>($"{AudioPath}/Player/Footsteps/concrete3.wav"),
                Load<AudioClip>($"{AudioPath}/Player/Footsteps/concrete4.wav"),
                Load<AudioClip>($"{AudioPath}/Player/Footsteps/concrete5.wav"),
                Load<AudioClip>($"{AudioPath}/Player/Footsteps/concrete6.wav"),
            };

            // Land sounds
            gameAudio.landSounds = new AudioClip[]
            {
                Load<AudioClip>($"{AudioPath}/Player/jumplanding.wav"),
                Load<AudioClip>($"{AudioPath}/Player/jumplanding2.wav"),
            };

            // Damage
            gameAudio.damageSounds = new AudioClip[]
            {
                Load<AudioClip>($"{AudioPath}/Player/Damage/damage1.wav"),
                Load<AudioClip>($"{AudioPath}/Player/Damage/damage2.wav"),
                Load<AudioClip>($"{AudioPath}/Player/Damage/damage3.wav"),
            };

            gameAudio.headshotSounds = new AudioClip[]
            {
                Load<AudioClip>($"{AudioPath}/Player/Damage/headshot1.wav"),
                Load<AudioClip>($"{AudioPath}/Player/Damage/headshot2.wav"),
            };

            gameAudio.helmetHitSound = Load<AudioClip>($"{AudioPath}/Player/Damage/bhit_helmet-1.wav");
            gameAudio.armorHitSound = Load<AudioClip>($"{AudioPath}/Player/Damage/kevlar1.wav");

            // Death
            gameAudio.deathSounds = new AudioClip[]
            {
                Load<AudioClip>($"{AudioPath}/Player/Death/death1.wav"),
                Load<AudioClip>($"{AudioPath}/Player/Death/death2.wav"),
                Load<AudioClip>($"{AudioPath}/Player/Death/death3.wav"),
            };

            // Bomb
            gameAudio.bombPlantSound = Load<AudioClip>($"{AudioPath}/Bomb/c4_plant.wav");
            gameAudio.bombDefuseSound = Load<AudioClip>($"{AudioPath}/Bomb/c4_disarmfinish.wav");
            gameAudio.bombExplodeSound = Load<AudioClip>($"{AudioPath}/Bomb/c4_explode1.wav");
            gameAudio.bombBeepSound = Load<AudioClip>($"{AudioPath}/Bomb/c4_beep1.wav");

            // Round announcements
            gameAudio.roundStartSound = Load<AudioClip>($"{AudioPath}/Radio/mm_success_lets_roll.wav");
            gameAudio.ctWinSound = Load<AudioClip>($"{AudioPath}/Radio/ctwin.wav");
            gameAudio.tWinSound = Load<AudioClip>($"{AudioPath}/Radio/terwin.wav");
            gameAudio.bombPlantedAnnounce = Load<AudioClip>($"{AudioPath}/Radio/bombpl.wav");
            gameAudio.bombDefusedAnnounce = Load<AudioClip>($"{AudioPath}/Radio/bombdef.wav");

            // UI
            gameAudio.buttonClickSound = Load<AudioClip>($"{AudioPath}/UI/buttonclick.wav");

            EditorUtility.SetDirty(gameAudio);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            Debug.Log("GameAudio setup complete with all clips wired!");
        }

        static T Load<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                Debug.LogWarning($"Audio clip not found: {path}");
            return asset;
        }
    }
}
#endif
