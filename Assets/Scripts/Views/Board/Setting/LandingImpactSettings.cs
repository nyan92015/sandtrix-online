using UnityEngine;

namespace SandTetris
{
    [System.Serializable]
    public class LandingImpactSettings
    {
        public Color32 flashColor = new Color32(255, 255, 255, 255);
        public float flashDuration = 0.12f;
        public float shakeDuration = 0.15f;
        public float shakeMagnitude = 8f;
    }
}