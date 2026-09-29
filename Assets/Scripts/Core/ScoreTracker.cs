using System;
using System.Collections.Generic;

namespace SandTetris
{
    /// <summary>
    /// 倍率ボーナス(コンボ)を「穴の空いたバケツ」のスタックとして管理するスコア計算機。
    /// BoardModel.OnLinesFound を購読するだけの、他のObserverと同じ立ち位置のクラス。
    ///
    /// 仕組み:
    /// - ライン消去のたびに、現在の倍率+1(上限10)の新しいバケツが積まれる
    /// - バケツの中身(消した量)は時間経過で減っていく。0になる前に次のライン消去ができれば倍率が上がる
    /// - 上限(×10)に達している間は、昇格せず×10のバケツが積み上がっていくだけ
    /// - 一番上のバケツが空になると、それは捨てられ、その下にあったバケツが「残っていた分」から再開する
    /// - スタックが空になったら倍率は×1(基本状態)に戻る
    /// </summary>
    public class ScoreTracker
    {
        class Bucket
        {
            public int Multiplier;
            public float Remaining;
            public float Capacity; // 生成時のRemaining初期値。ゲージ表示の割合計算に使う
        }

        public const int MaxMultiplier = 10;

        /// <summary>バケツが1秒あたりに減る量。仮の値なので、遊びながら調整する想定。</summary>
        public float DrainRatePerSecond = 200f;

        readonly Stack<Bucket> _buckets = new Stack<Bucket>();

        public int TotalScore { get; private set; }
        public int CurrentMultiplier => _buckets.Count > 0 ? _buckets.Peek().Multiplier : 1;
        public int LinesCleared { get; private set; }

        /// <summary>一番上のバケツの残量割合(0〜1)。バケツが無ければ0。ゲージ表示用。</summary>
        public float CurrentBucketFillRatio
        {
            get
            {
                if (_buckets.Count == 0) return 0f;
                var top = _buckets.Peek();
                if (top.Capacity <= 0f) return 0f;
                float ratio = top.Remaining / top.Capacity;
                if (ratio < 0f) return 0f;
                if (ratio > 1f) return 1f;
                return ratio;
            }
        }

        /// <summary>合計スコアが変化した(更新後の合計値を渡す)</summary>
        public event Action<int> OnScoreChanged;

        /// <summary>1回のライン消去で得点が入った(獲得スコア, そのとき適用された倍率)</summary>
        public event Action<int, int> OnComboEarned;

        /// <summary>倍率が変化した(更新後の倍率)</summary>
        public event Action<int> OnMultiplierChanged;

        /// <summary>消去したライン数が変化した(更新後の合計本数)</summary>
        public event Action<int> OnLinesClearedChanged;

        /// <summary>
        /// 10ライン消すごとに0.1ずつ足されていく速度倍率(上限なし)。
        /// 10ライン=1.1倍、20ライン=1.2倍、30ライン=1.3倍…と直線的に増えていく。
        /// </summary>
        public float SpeedMultiplier => 1f + 0.1f * (LinesCleared / 10);

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

        public ScoreTracker(BoardModel model)
        {
            model.OnLinesFound += HandleLinesFound;
        }

        /// <summary>
        /// ソフトドロップを1秒押し続けたときに入る点数。
        /// コンボの倍率は掛からず、バケツにも入らない、素の得点として合計スコアにだけ加算される。
        /// </summary>
        public float SoftDropPointsPerSecond = 100f;

        // 1フレームあたりの得点は小数(60fpsなら約1.67点)になるので、端数を貯めておき、
        // 1点以上になった分だけ合計スコアに加算する(切り捨てで取りこぼさないため)。
        float _softDropPointAccumulator;

        /// <summary>
        /// ソフトドロップを押していた時間(秒)を渡すと、その分の得点を合計スコアに加算する。
        /// ソフトドロップ中のフレームだけ、毎フレーム呼ぶ想定。
        /// </summary>
        public void AddSoftDropTime(float deltaTime)
        {
            _softDropPointAccumulator += SoftDropPointsPerSecond * deltaTime;

            int whole = (int)_softDropPointAccumulator;
            if (whole <= 0) return;

            _softDropPointAccumulator -= whole;
            TotalScore += whole;
            OnScoreChanged?.Invoke(TotalScore);
        }

        void HandleLinesFound(IReadOnlyList<int> clearedIndices)
        {
            int rawScore = clearedIndices.Count; // 1ピクセル = 1点

            int previousMultiplier = CurrentMultiplier;
            int effectiveMultiplier = _buckets.Count > 0
                ? Math.Min(_buckets.Peek().Multiplier + 1, MaxMultiplier)
                : 1;

            int awarded = rawScore * effectiveMultiplier;
            TotalScore += awarded;

            _buckets.Push(new Bucket { Multiplier = effectiveMultiplier, Remaining = rawScore, Capacity = rawScore });

            LinesCleared++;

            OnComboEarned?.Invoke(awarded, effectiveMultiplier);
            OnScoreChanged?.Invoke(TotalScore);
            OnLinesClearedChanged?.Invoke(LinesCleared);

            if (effectiveMultiplier != previousMultiplier)
            {
                OnMultiplierChanged?.Invoke(effectiveMultiplier);
            }
        }

        /// <summary>
        /// 毎フレーム呼ぶ。一番上のバケツだけを時間経過で減らし、空になったら捨てて次のバケツに移る。
        /// 減る速さ自体にも SpeedMultiplier がかかるので、ライン数が増えるほどゲージも速く減っていく。
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (_buckets.Count == 0) return;

            int before = CurrentMultiplier;

            _buckets.Peek().Remaining -= DrainRatePerSecond * SpeedMultiplier * deltaTime;
            while (_buckets.Count > 0 && _buckets.Peek().Remaining <= 0f)
            {
                _buckets.Pop();
            }

            int after = CurrentMultiplier;
            if (after != before)
            {
                OnMultiplierChanged?.Invoke(after);
            }
        }
    }
}