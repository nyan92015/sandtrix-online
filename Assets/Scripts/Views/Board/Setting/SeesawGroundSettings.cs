using UnityEngine;

namespace SandTetris
{
    [System.Serializable]
    public class SeesawGroundSettings
    {
        public float contestWindowSeconds = 10f; // 攻撃が始まってから確定するまでの秒数
        public int contestThreshold = 2000;      // 攻撃が成立するための基準点(起点・確定・逆転の3か所に共通)
        public float groundRiseSeconds = 3f;     // 攻撃が確定してから、地面が目標の高さまで上がりきる秒数
        public float groundPushBaseCost = 3000f; // 圧力から高さを決めるときの、1段目の基準コスト
        public Color32 groundColor = new Color32(90, 90, 95, 255); // せり上がっている間も、崩れて灰になった後も、同じこの色を使う
        public Color32 frameColor = new Color32(30, 30, 36, 255); // 額縁(盤面の外側の帯)の、通常時の色
        public int frameThicknessPx = 4;                          // 額縁の幅(ピクセル)
        public Color32 warningColor = new Color32(220, 40, 40, 255); // 警告中の色(額縁・境界線・縞、共通)
    }
}