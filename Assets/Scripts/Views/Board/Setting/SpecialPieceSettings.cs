using UnityEngine;

namespace SandTetris
{
    [System.Serializable]
    public class SpecialPieceSettings
    {
        [Range(0f, 1f)] public float normalBrightenAmount = 0.3f; // 通常の砂の明るさ(落下中・着地後、両方に反映される)

        [Header("落下中の色。判定用のColorIndexの順と対応: 赤・青・黄・緑")]
        public Color32 colorRed = new Color32(150, 30, 30, 255);
        public Color32 colorBlue = new Color32(30, 80, 150, 255);
        public Color32 colorYellow = new Color32(150, 120, 20, 255);
        public Color32 colorGreen = new Color32(30, 120, 60, 255);
        [Range(0f, 2f)] public float saturationBoost = 0f; // 4色まとめて、どれだけ彩度を上げるか

        public Color32 edgeColor = new Color32(255, 255, 255, 255);
        [Range(0f, 1f)] public float edgeMinBrightness = 0.3f;
        [Range(0f, 1f)] public float edgeMaxBrightness = 1.0f;
        public float pulseSpeed = 3f;
        public float brightBias = 0.6f; // 1より小さいほど、明るい側に長く留まる
    }
}