using System;
using UnityEngine;

namespace SandTetris
{
    /// <summary>
    /// フィーバーシステム。
    ///
    /// 4色(赤・青・黄・緑)を、毎回ランダムな順番で1つ用意する(CurrentOrder)。
    /// その順番通りに、その色のラインを消すと、該当する区画が点灯する(Progressが進む)。
    /// 順番と違う色を消しても、進行は崩れない(その回はただ無視されるだけ)。
    /// 4色すべて点灯したら、自動的にフィーバーが始まり、同時に次の回の新しい順番を用意する。
    ///
    /// フィーバー中(FeverDuration秒間)は、BoardModel.SpawnNext が、通常のミノの代わりに
    /// 特別なミノを生成するようになる(IsFeverActiveを見て判断する)。
    /// </summary>
    public class FeverSystem
    {
        const int ColorCount = 4;

        /// <summary>フィーバーが続く時間(秒)。</summary>
        public float FeverDuration = 10f;

        /// <summary>今回の、4色のランダムな並び順(ColorIndexの配列、長さ4)。</summary>
        public byte[] CurrentOrder { get; private set; }

        /// <summary>CurrentOrder のうち、すでに点灯した数(0〜4)。次に点灯させるべき位置でもある。</summary>
        public int Progress { get; private set; }

        public bool IsFeverActive { get; private set; }

        /// <summary>フィーバー中の、残り時間(秒)。フィーバー中でなければ0。</summary>
        public float FeverTimeRemaining { get; private set; }

        /// <summary>新しい順番(CurrentOrder)が決まった(ゲージがリセットされた)瞬間に発火する。</summary>
        public event Action OnOrderChanged;

        /// <summary>CurrentOrder内の、指定インデックスの区画が点灯した瞬間に発火する。</summary>
        public event Action<int> OnLampLit;

        public event Action OnFeverStarted;
        public event Action OnFeverEnded;

        readonly BoardModel _model;

        public FeverSystem(BoardModel model)
        {
            _model = model;
            model.OnLinesFound += HandleLinesFound;
            ShuffleNewOrder();
        }

        void ShuffleNewOrder()
        {
            var order = new byte[ColorCount] { 0, 1, 2, 3 };
            // Fisher-Yatesシャッフル
            for (int i = order.Length - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }

            CurrentOrder = order;
            Progress = 0;
            OnOrderChanged?.Invoke();
        }

        void HandleLinesFound(System.Collections.Generic.IReadOnlyList<int> clearedIndices)
        {
            if (clearedIndices.Count == 0) return;
            if (Progress >= CurrentOrder.Length) return; // 念のための安全策(通常は起きない)

            // この時点ではまだ ApplyClear 前なので、Cells に色が残っている
            byte clearedColor = _model.Grid.Cells[clearedIndices[0]].ColorIndex;

            if (clearedColor != CurrentOrder[Progress]) return; // 順番と違う色: 何もせず無視するだけ

            int litSlot = Progress;
            Progress++;
            OnLampLit?.Invoke(litSlot);

            if (Progress >= CurrentOrder.Length)
            {
                StartFever();
            }
        }

        void StartFever()
        {
            IsFeverActive = true;
            FeverTimeRemaining = FeverDuration;
            OnFeverStarted?.Invoke();

            // フィーバー中であっても、次の回のゲージをすぐに溜め始められるようにしておく
            ShuffleNewOrder();
        }

        /// <summary>毎フレーム呼ぶ。フィーバー中の残り時間だけを進める。</summary>
        public void Tick(float deltaTime)
        {
            if (!IsFeverActive) return;

            FeverTimeRemaining -= deltaTime;
            if (FeverTimeRemaining <= 0f)
            {
                IsFeverActive = false;
                FeverTimeRemaining = 0f;
                OnFeverEnded?.Invoke();
            }
        }
    }
}