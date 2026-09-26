namespace SandTetris
{
    /// <summary>
    /// BoardPresenterの挙動を調整する設定値。MonoBehaviour側(View)のInspectorで
    /// 設定された値をここに詰め替えて渡す想定(PresenterはUnity非依存のまま保つため)。
    /// </summary>
    public class BoardPresenterConfig
    {
        public float FallInterval = 0.6f;
        public float SoftDropInterval = 0.04f;
        public float MoveRepeatInterval = 0.08f;
        public float GravityInterval = 0.03f;

        public float DiagonalMoveChance = 0.35f;
        public float FallMoveChance = 0.85f;

        public float ClearFlashDuration = 0.2f;
        public float LandingFreezeDuration = 0.06f;
        public float LineClearFreezeDuration = 0.08f;
    }
}