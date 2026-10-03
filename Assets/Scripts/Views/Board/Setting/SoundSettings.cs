using UnityEngine;

namespace SandTetris
{
    [System.Serializable]
    public class SoundSettings
    {
        public AudioClip landSound;
        public AudioClip rotateSound;
        public AudioClip lineClearSound;
        [Range(0f, 1f)] public float volume = 1f;
    }
}