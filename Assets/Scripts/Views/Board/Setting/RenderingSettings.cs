using UnityEngine;
using UnityEngine.UI;

namespace SandTetris
{
    [System.Serializable]
    public class RenderingSettings
    {
        public RawImage displayImage;
        public Color32 backgroundColor = new Color32(18, 18, 24, 255);
    }
}