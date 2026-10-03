using TMPro;
using UnityEngine.UI;

namespace SandTetris
{
    [System.Serializable]
    public class ScoreUISettings
    {
        public TMP_Text scoreText;
        public TMP_Text multiplierText;
        public TMP_Text linesClearedText;
        public Slider bucketGaugeSlider;
    }
}