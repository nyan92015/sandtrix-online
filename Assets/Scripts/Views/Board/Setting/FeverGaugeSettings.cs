using UnityEngine;
using UnityEngine.UI;

namespace SandTetris
{
    [System.Serializable]
    public class FeverGaugeSettings
    {
        public Image[] lampImages = new Image[4]; // 左から順に4つ
        public Sprite spriteRed;
        public Sprite spriteBlue;
        public Sprite spriteYellow;
        public Sprite spriteGreen;
        public Sprite spriteUnlit; // 消灯時(黒)のスプライト
        public float duration = 10f; // フィーバーが続く時間(秒)
    }
}