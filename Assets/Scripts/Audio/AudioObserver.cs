using UnityEngine;

namespace SandTetris
{
    public class AudioObserver
    {
        readonly AudioSource _source;

        public AudioClip LandSound;
        public AudioClip RotateSound;
        public AudioClip LineClearSound;
        public float Volume = 1f;

        public AudioObserver(BoardModel model, AudioSource source)
        {
            _source = source;

            model.OnPieceLanded += (_, __, ___) => Play(LandSound);
            model.OnPieceRotated += () => Play(RotateSound);
            model.OnLinesFound += _ => Play(LineClearSound);
        }

        void Play(AudioClip clip)
        {
            if (clip != null && _source != null) _source.PlayOneShot(clip, Volume);
        }
    }
}