using UnityEngine;

namespace SandTetris
{
    [System.Serializable]
    public class TimingSettings
    {
        public float fallInterval = 1f / 84f; // 本家のMED基準(84px/秒)を、1ピクセルあたりの秒数に変換した値
        public float softDropBonusSpeedIdlePxPerSec = 120f;   // ソフトドロップ中、左右移動していないときに足す速さ(px/秒)。本家準拠
        public float softDropBonusSpeedMovingPxPerSec = 60f; // ソフトドロップ中、左右移動しているときに足す速さ(px/秒)。本家準拠
        public float softDropPointsPerSecond = 100f; // ソフトドロップを1秒押し続けたときの得点
        public float moveRepeatInterval = 1f / 120f; // 本家準拠(120px/秒)。ライン数による加速の対象外
        public float gravityInterval = 0.03f;
        public float ashGravityInterval = 0.15f; // 灰が1マス落ちる間隔(秒)。砂の重力とは別・ライン数の影響も受けない固定値
        public float ashLifetimeSeconds = 6f;    // 灰が消えるまでの時間(秒)。砂の下に埋もれて止まっても、この時間で強制的に消える
        public float minGravityInterval = 0.01f; // 加速してもこれより短くはしない
        [Range(0f, 1f)] public float diagonalMoveChance = 0.35f;
        [Range(0f, 1f)] public float fallMoveChance = 0.85f;
        public float clearFlashDuration = 0.2f;
        public float landingFreezeDuration = 0.06f;
        public float lineClearFreezeDuration = 0.08f;
    }
}