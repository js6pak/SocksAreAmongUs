using UnityEngine;

namespace SocksAreAmongUs
{
    public static class SoundManagerExtensions
    {
        private static AudioSource AddAudioSource(this SoundManager soundManager, GameObject gameObject, AudioClip clip, float volume = 1f)
        {
            var audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.spatialBlend = 1f;
            audioSource.spread = 1f;
            audioSource.outputAudioMixerGroup = soundManager.sfxMixer;
            audioSource.volume = volume;
            audioSource.Play();
            Object.Destroy(gameObject, clip.length * ((double) Time.timeScale < 0.009999999776482582 ? 0.01f : Time.timeScale));

            return audioSource;
        }

        public static AudioSource PlaySound(this SoundManager soundManager, AudioClip clip, Transform parent, float volume = 1f)
        {
            var gameObject = new GameObject("One shot audio");
            gameObject.transform.parent = parent;
            gameObject.transform.localPosition = Vector3.zero;

            return soundManager.AddAudioSource(gameObject, clip, volume);
        }

        public static AudioSource PlaySound(this SoundManager soundManager, AudioClip clip, Vector3 position, float volume = 1f)
        {
            var gameObject = new GameObject("One shot audio");
            gameObject.transform.position = position;

            return soundManager.AddAudioSource(gameObject, clip, volume);
        }
    }
}
