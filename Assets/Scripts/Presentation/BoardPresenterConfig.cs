namespace SandTetris
{
    /// <summary>
    /// BoardPresenterの挙動を調整する設定値。MonoBehaviour側(View)のInspectorで
    /// 設定された値をここに詰め替えて渡す想定(PresenterはUnity非依存のまま保つため)。
    /// </summary>
    public class BoardPresenterConfig
    {
        public float FallInterval = 0.6f;
        public float SoftDropSpeedMultiplier = 3f; // ソフトドロップ中は、通常の落下の何倍の速さで落ちるか
        public float MoveRepeatInterval = 0.08f;
        public float GravityInterval = 0.03f;
        public float MinGravityInterval = 0.01f; // これより短くはしない(処理負荷の安全弁)

        public float DiagonalMoveChance = 0.35f;
        public float FallMoveChance = 0.85f;
        public float AshGravityInterval = 0.15f; // 灰が1マス落ちる間隔(秒)。砂の重力とは別・ライン数の影響も受けない固定値
        public float AshLifetimeSeconds = 6f;    // 灰が消えるまでの時間(秒)。砂の下に埋もれて止まっても、この時間で強制的に消える

        public float ClearFlashDuration = 0.2f;
        public float LandingFreezeDuration = 0.06f;
        public float LineClearFreezeDuration = 0.08f;

        public int BlockSize = 8;

        public float SoftDropPointsPerSecond = 100f; // ソフトドロップを1秒押し続けたときの得点

        // シーソー式地面
        public float ContestWindowSeconds = 10f;      // 攻撃が始まってから確定するまでの秒数
        public int ContestThreshold = 2000;           // 攻撃が成立するための基準点(起点・確定・逆転の3か所に共通)
        public float GroundRiseSeconds = 3f;          // 攻撃が確定してから、地面が目標の高さまで上がりきる秒数
        public double GroundPushBaseCost = 3000.0;    // 圧力から高さを決めるときの、1段目の基準コスト
        public UnityEngine.Color32 GroundColor = new UnityEngine.Color32(90, 90, 95, 255);
        public UnityEngine.Color32 AshColor = new UnityEngine.Color32(150, 150, 155, 255); // 上がりきって崩れたあとの灰の色
    }
}