using UnityEngine;

namespace SandTetris
{
    [System.Serializable]
    public class InfectionSweepSettings
    {
        public float flashSpeed = 10f;
        [Range(0f, 1f)] public float flashMaxBlend = 0.6f;
        [Range(0f, 1f)] public float ringWidth = 0.35f;
    }
}