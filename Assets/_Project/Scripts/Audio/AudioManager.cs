// AI Tool: Anthropic Claude Opus 4.6 (Claude Code CLI)
// Prompt: "Audio manager singleton with a pool of AudioSources for 3D and 2D
//          SFX and a master volume that saves between sessions."
// Modifications: Added the dynamic pool extension when every source is busy.

using System.Collections.Generic;
using UnityEngine;

namespace CounterSiege
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        const string MASTER_VOL_KEY = "MasterVolume";
        const float DEFAULT_VOLUME = 0.3f;

        public float MasterVolume
        {
            get => masterVolume;
            set
            {
                masterVolume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(MASTER_VOL_KEY, masterVolume);
                PlayerPrefs.Save();
            }
        }

        float masterVolume;
        Queue<AudioSource> pool = new();
        int poolSize = 20;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            masterVolume = PlayerPrefs.GetFloat(MASTER_VOL_KEY, DEFAULT_VOLUME);

            for (int i = 0; i < poolSize; i++)
            {
                var go = new GameObject($"AudioSource_{i}");
                go.transform.SetParent(transform);
                var src = go.AddComponent<AudioSource>();
                src.spatialBlend = 1f;
                src.rolloffMode = AudioRolloffMode.Linear;
                src.maxDistance = 50f;
                pool.Enqueue(src);
            }
        }

        public void PlaySFX(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (clip == null) return;

            AudioSource src = GetSource();
            if (src == null) return;

            src.transform.position = position;
            src.clip = clip;
            src.volume = volume * masterVolume;
            src.Play();
        }

        public void PlaySFX2D(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;

            AudioSource src = GetSource();
            if (src == null) return;

            src.spatialBlend = 0f;
            src.clip = clip;
            src.volume = volume * masterVolume;
            src.Play();
        }

        AudioSource GetSource()
        {
            for (int i = 0; i < pool.Count; i++)
            {
                var src = pool.Dequeue();
                pool.Enqueue(src);
                if (!src.isPlaying)
                {
                    src.spatialBlend = 1f;
                    return src;
                }
            }

            var go = new GameObject("AudioSource_Extra");
            go.transform.SetParent(transform);
            var newSrc = go.AddComponent<AudioSource>();
            newSrc.spatialBlend = 1f;
            pool.Enqueue(newSrc);
            return newSrc;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
