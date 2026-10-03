namespace SandTetris
{
    /// <summary>
    /// ライン数に応じた速度倍率の計算を担当する。
    /// 落下・左右移動の間隔などを「ライン数が増えるほど際限なく速くする」ために使う。
    /// </summary>
    public class GameSpeedScaler
    {
        readonly ScoreTracker _score;

        public GameSpeedScaler(ScoreTracker score)
        {
            _score = score;
        }

        /// <summary>
        /// 10ライン消すごとに0.1ずつ足されていく速度倍率(上限なし)。
        /// 10ライン=1.1倍、20ライン=1.2倍、30ライン=1.3倍…と直線的に増えていく。
        /// </summary>
        public float SpeedMultiplier => 1f + 0.1f * (_score.LinesCleared / 10);

        /// <summary>
        /// 「間隔(秒)」を SpeedMultiplier に応じて縮めた値を返す。下限なし。
        /// ミノの落下・左右移動のリピートなど、「際限なく速くなっていい」ものに使う。
        /// </summary>
        public float ScaleInterval(float baseInterval)
        {
            return baseInterval / SpeedMultiplier;
        }

        /// <summary>
        /// 「間隔(秒)」を SpeedMultiplier に応じて縮めるが、minInterval より下には行かせない。
        /// 砂の物理演算(Gravity)など、「速くなりすぎると1フレームの計算回数が際限なく増えて重くなる」
        /// ものに使う。
        /// </summary>
        public float ScaleGravityInterval(float baseInterval, float minInterval)
        {
            float scaled = baseInterval / SpeedMultiplier;
            return scaled < minInterval ? minInterval : scaled;
        }
    }
}