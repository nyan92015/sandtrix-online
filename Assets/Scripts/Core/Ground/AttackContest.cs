using System;

namespace SandTetris
{
    /// <summary>
    /// 「攻撃と相殺のせめぎ合い」のルールだけを持つクラス。通信にもUnityにも依存しない。
    ///
    /// 圧力(Pressure)を、自分の視点の符号付きの1つの数値で持つ:
    ///   正 = 自分が攻撃側(相手の盤面を圧迫しようとしている)
    ///   負 = 相手が攻撃側(自分の盤面が圧迫されようとしている)
    ///   0  = 待機中(誰も攻めていない)
    ///
    /// ルール:
    /// - 起点: 待機中に、1回の得点が Threshold 以上のとき、その人が攻撃側になり、
    ///   WindowSeconds 秒の窓が始まる。Threshold 未満の得点は、待機中は無視する
    /// - 窓の間: 攻撃側の得点は圧力に積み上がり、守備側の得点は1対1でそれを打ち消す
    ///   (得点の大きさは問わない。守備側の得点は相手には影響しない)
    /// - 逆転: 守備側が圧力を上回ったとき、超過分が Threshold 以上なら、役割が入れ替わり、
    ///   超過分を圧力として新しい窓が始まる。未満なら、逆転にならず待機中に戻る
    ///   (元の攻撃も、逆転した側の攻撃も成立しない)
    /// - 確定: 窓が終わった時点の圧力が Threshold 以上なら、守備側の盤面への攻撃として成立する。
    ///   未満なら不成立で、何も起きずに待機中に戻る
    ///
    /// 両方のクライアントが、同じ得点の流れから同じ計算を独立に行う想定
    /// (通信の遅れで、窓の開始が数百ミリ秒ずれる程度で、結果はほぼ一致する)。
    /// </summary>
    public class AttackContest
    {
        /// <summary>攻撃が始まってから確定するまでの秒数。</summary>
        public float WindowSeconds = 10f;

        /// <summary>
        /// 攻撃が成立するための基準点。「起点(1回の得点)」「確定(窓が終わったときの圧力)」
        /// 「逆転(超過分)」の3か所に、共通で使う。
        /// </summary>
        public int Threshold = 2000;

        /// <summary>符号付きの圧力。正=自分が攻撃側、負=相手が攻撃側、0=待機中。</summary>
        public int Pressure { get; private set; }

        /// <summary>現在の窓が始まってからの経過秒数(待機中は0)。</summary>
        public float WindowElapsed { get; private set; }

        /// <summary>現在の窓が終わるまでの残り秒数(待機中は0)。</summary>
        public float WindowRemaining => Pressure == 0 ? 0f : Math.Max(0f, WindowSeconds - WindowElapsed);

        public bool IsIdle => Pressure == 0;
        public bool IsAttacking => Pressure > 0;
        public bool IsDefending => Pressure < 0;

        /// <summary>自分が受ける攻撃が確定した(圧力の大きさ)。自分の盤面が圧迫される。</summary>
        public event Action<int> OnAttackLanded;

        /// <summary>自分が仕掛けた攻撃が確定した(相手に与えた圧力の大きさ)。</summary>
        public event Action<int> OnAttackSent;

        /// <summary>自分が稼いだ点数を加える。攻撃側なら圧力が増え、守備側なら相殺になる。</summary>
        public void AddMyPoints(int points)
        {
            if (points > 0) Apply(+points);
        }

        /// <summary>相手が稼いだ点数を加える。相手が攻撃側なら圧力が増え、自分が攻撃側なら相殺になる。</summary>
        public void AddOpponentPoints(int points)
        {
            if (points > 0) Apply(-points);
        }

        void Apply(int signedDelta)
        {
            int before = Pressure;

            if (before == 0)
            {
                // 待機中: 1回の得点が基準点以上のときだけ、それが起点になって窓が始まる。
                // 基準点未満の得点(ソフトドロップの得点など)は、待機中は無視する。
                if (Math.Abs(signedDelta) < Threshold) return;

                Pressure = signedDelta;
                WindowElapsed = 0f;
                return;
            }

            int after = before + signedDelta;

            if (after == 0)
            {
                // ちょうど打ち消し合った: 攻撃は成立せず、待機中に戻る
                Pressure = 0;
                WindowElapsed = 0f;
                return;
            }

            if (Math.Sign(before) != Math.Sign(after))
            {
                // 守備側が圧力を上回った: 超過分が基準点以上のときだけ逆転が成立する
                if (Math.Abs(after) >= Threshold)
                {
                    // 役割が入れ替わるので、新しい窓をやり直す(超過分がそのまま圧力になる)
                    Pressure = after;
                    WindowElapsed = 0f;
                }
                else
                {
                    // 超過分が基準点に届かない: 逆転にならず、どちらの攻撃も成立しないまま待機中に戻る
                    Pressure = 0;
                    WindowElapsed = 0f;
                }
                return;
            }

            // 同じ側のまま増減しただけ: 窓はそのまま続ける(この間は得点の大きさを問わない)
            Pressure = after;
        }

        /// <summary>毎フレーム呼ぶ。窓の時間を進め、終わったら攻撃を確定させて待機中に戻す。</summary>
        public void Tick(float deltaTime)
        {
            if (Pressure == 0) return;

            WindowElapsed += deltaTime;
            if (WindowElapsed < WindowSeconds) return;

            int magnitude = Math.Abs(Pressure);
            bool imDefending = Pressure < 0;

            Pressure = 0;
            WindowElapsed = 0f;

            // 窓が終わった時点の圧力が基準点に届いていなければ、攻撃は成立しない
            if (magnitude < Threshold) return;

            if (imDefending) OnAttackLanded?.Invoke(magnitude);
            else OnAttackSent?.Invoke(magnitude);
        }
    }
}