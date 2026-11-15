using System;
using UnityEngine;
using UnityEngine.UI;
using SpaceMissionGame.Core;

namespace SpaceMissionGame.Audio
{
    /// <summary>
    /// [Member 3 - HanMing] Procedural NASA Mission Control & Rocket Sound Effects (`SpaceSoundFX.cs`).
    ///
    /// Generates all audio clips in memory using `AudioClip.Create` (no external .wav/.mp3 files needed!)
    /// and auto-attaches at runtime with ZERO edits to Member 2's scene or prefabs.
    /// </summary>
    public class SpaceSoundFX : MonoBehaviour
    {
        public static SpaceSoundFX Instance { get; private set; }

        private const int SampleRate = 44100;

        private AudioSource _sfxSource;
        private AudioSource _engineSource;

        private AudioClip _clickClip;
        private AudioClip _equipClip;
        private AudioClip _sepClip;
        private AudioClip _chimeClip;
        private AudioClip _alertClip;
        private AudioClip _engineLoopClip;

        public bool IsMuted { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (FindAnyObjectByType<SpaceSoundFX>() != null) return;

            if (FindAnyObjectByType<AudioListener>() == null && Camera.main != null)
            {
                Camera.main.gameObject.AddComponent<AudioListener>();
            }

            GameObject go = new GameObject("NASA_SpaceSoundFX_Member3");
            go.AddComponent<SpaceSoundFX>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
            _sfxSource.spatialBlend = 0f;

            _engineSource = gameObject.AddComponent<AudioSource>();
            _engineSource.playOnAwake = false;
            _engineSource.loop = true;
            _engineSource.spatialBlend = 0f;

            BuildAllProceduralClips();
        }

        public void SetMuted(bool muted)
        {
            IsMuted = muted;
            if (muted) StopEngineRumble();
        }

        public void PlayClick()
        {
            if (!IsMuted && _clickClip != null) _sfxSource.PlayOneShot(_clickClip, 0.32f);
        }

        public void PlayEquip()
        {
            if (!IsMuted && _equipClip != null) _sfxSource.PlayOneShot(_equipClip, 0.40f);
        }

        public void PlaySeparation()
        {
            if (!IsMuted && _sepClip != null) _sfxSource.PlayOneShot(_sepClip, 0.55f);
        }

        public void PlaySuccessChime()
        {
            if (!IsMuted && _chimeClip != null) _sfxSource.PlayOneShot(_chimeClip, 0.38f);
        }

        public void PlayAlertBeep()
        {
            if (!IsMuted && _alertClip != null) _sfxSource.PlayOneShot(_alertClip, 0.35f);
        }

        public void StartEngineRumble(float volume = 0.35f, float pitch = 1.0f)
        {
            if (IsMuted || _engineLoopClip == null) return;
            _engineSource.clip = _engineLoopClip;
            _engineSource.volume = Mathf.Clamp01(volume);
            _engineSource.pitch = pitch;
            if (!_engineSource.isPlaying) _engineSource.Play();
        }

        public void StopEngineRumble()
        {
            if (_engineSource != null && _engineSource.isPlaying) _engineSource.Stop();
        }

        public void StopAllSounds()
        {
            StopEngineRumble();
            if (_sfxSource != null && _sfxSource.isPlaying) _sfxSource.Stop();
        }

        // =========================================================================
        // PROCEDURAL WAVEFORM SYNTHESIS
        // =========================================================================
        private void BuildAllProceduralClips()
        {
            _clickClip = MakeClickClip();
            _equipClip = MakeEquipClip();
            _sepClip = MakeSeparationClip();
            _chimeClip = MakeSuccessChimeClip();
            _alertClip = MakeAlertBeepClip();
            _engineLoopClip = MakeEngineRumbleLoop();
        }

        private static AudioClip MakeClickClip()
        {
            int len = (int)(SampleRate * 0.045f);
            float[] data = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Exp(-t * 95f);
                float freq = Mathf.Lerp(1150f, 650f, i / (float)len);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.6f;
            }
            return CreateClip("SFX_Click", data, false);
        }

        private static AudioClip MakeEquipClip()
        {
            int len = (int)(SampleRate * 0.11f);
            float[] data = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Exp(-t * 28f);
                float freq = t < 0.045f ? 520f : 780f;
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.5f;
            }
            return CreateClip("SFX_Equip", data, false);
        }

        private static AudioClip MakeSeparationClip()
        {
            int len = (int)(SampleRate * 0.28f);
            float[] data = new float[len];
            System.Random rng = new System.Random(77);
            float prev = 0f;
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)SampleRate;
                float thudEnv = Mathf.Exp(-t * 26f);
                float thud = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(160f, 52f, t / 0.28f) * t) * thudEnv * 0.8f;

                float hissEnv = Mathf.Exp(-t * 14f);
                float raw = (float)(rng.NextDouble() * 2.0 - 1.0);
                prev = prev * 0.65f + raw * 0.35f;
                float hiss = prev * hissEnv * 0.45f;

                data[i] = Mathf.Clamp(thud + hiss, -1f, 1f);
            }
            return CreateClip("SFX_Separation", data, false);
        }

        private static AudioClip MakeSuccessChimeClip()
        {
            int len = (int)(SampleRate * 0.65f);
            float[] data = new float[len];
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.50f }; // C5 E5 G5 C6
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)SampleRate;
                int noteIdx = Mathf.Min(notes.Length - 1, (int)(t / 0.11f));
                float localT = t - noteIdx * 0.11f;
                float env = Mathf.Exp(-localT * 8.5f);
                data[i] = Mathf.Sin(2f * Mathf.PI * notes[noteIdx] * t) * env * 0.42f;
            }
            return CreateClip("SFX_Success", data, false);
        }

        private static AudioClip MakeAlertBeepClip()
        {
            int len = (int)(SampleRate * 0.32f);
            float[] data = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)SampleRate;
                float pulse = (t < 0.12f || (t > 0.16f && t < 0.28f)) ? 1f : 0f;
                data[i] = Mathf.Sin(2f * Mathf.PI * 340f * t) * pulse * 0.38f;
            }
            return CreateClip("SFX_Alert", data, false);
        }

        private static AudioClip MakeEngineRumbleLoop()
        {
            int len = SampleRate * 2; // 2-second seamless rumble loop
            float[] data = new float[len];
            System.Random rng = new System.Random(2026);
            float lowPass = 0f;
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)SampleRate;
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                lowPass = lowPass * 0.94f + white * 0.06f; // deep rocket roar
                float sub = Mathf.Sin(2f * Mathf.PI * 46f * t) * 0.25f;
                data[i] = Mathf.Clamp(lowPass * 2.4f + sub, -1f, 1f);
            }
            return CreateClip("SFX_RaptorLoop", data, true);
        }

        private static AudioClip CreateClip(string name, float[] samples, bool loop)
        {
            AudioClip clip = AudioClip.Create(name, samples.Length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
