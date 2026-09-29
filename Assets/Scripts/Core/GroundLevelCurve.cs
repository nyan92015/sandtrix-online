using System;

namespace SandTetris
{
    /// <summary>
    /// 累計スコアから「何段分の高さに相当するか」を計算する共通ロジック。
    ///
    /// 1段目のコストが baseCost 点、以降1段上がるごとに必要な追加コストが ratio 倍ずつ
    /// 重くなる(等比数列)方式。
    /// 追加コスト(段N): baseCost × ratio^(N-1)
    /// 累計コスト(段Nに到達するまで): baseCost/(ratio-1) × (ratio^N - 1)
    ///
    /// この累計コストの式は N が小数でも成り立つので、逆算した N をそのまま高さとして使う。
    /// 切り捨てをしないので、段の途中(例: 1.38段)の高さも、点数に応じて連続的に決まる。
    ///
    /// 基準コストを引数にしているのは、「相手を押し上げるコスト」と
    /// 「自分の地面を下げるコスト」で別々の重さを使いたいため。
    /// </summary>
    public static class GroundLevelCurve
    {
        public const double DefaultRatio = 1.5;

        /// <summary>
        /// 累計スコアから、相当する段数(0以上、小数を含む)を求める。
        /// 段の境目(累計コストちょうど)では、整数の段数と一致する。
        /// </summary>
        public static float GetLevel(int totalScore, double baseCost, double ratio = DefaultRatio)
        {
            if (totalScore <= 0 || baseCost <= 0.0) return 0f;

            // 累計コスト(N) = baseCost/(ratio-1) × (ratio^N - 1) を N について解く
            double x = totalScore * (ratio - 1) / baseCost + 1.0;
            double n = Math.Log(x, ratio);

            return n < 0.0 ? 0f : (float)n;
        }
    }
}