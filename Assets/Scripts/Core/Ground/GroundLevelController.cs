using System;
using UnityEngine;

namespace SandTetris
{
    public class GroundLevelController
    {
        /// <summary>せめぎ合いのルール本体。UI表示(圧力・残り時間)などから参照できる。</summary>
        public AttackContest Contest { get; } = new AttackContest();

        /// <summary>地面がせり上がる、実際のアニメーションを担当する。</summary>
        public GroundRiseAnimator RiseAnimator { get; }

        /// <summary>警告表示の値を、なめらかに追従させる。</summary>
        public GroundWarningTracker WarningTracker { get; }

        int _lastOwnScore;
        int _lastOpponentScore;
        int _opponentScore;
        bool _opponentBaselineSet;

        /// <summary>現在の地面の高さ(段数、小数を含む)。せり上がっていない間は0。</summary>
        public float CurrentLevel => RiseAnimator.CurrentLevel;

        /// <summary>表示用の、警告の高さ(段数)。</summary>
        public float PendingWarningLevel => WarningTracker.PendingWarningLevel;

        // --- ここから、呼び出し側の利便性のための窓口。中身は、それぞれの専門クラスへそのまま転送する ---

        public float RiseSeconds { get => RiseAnimator.RiseSeconds; set => RiseAnimator.RiseSeconds = value; }
        public Color32 GroundColor { get => RiseAnimator.GroundColor; set => RiseAnimator.GroundColor = value; }
        public double PushBaseCost { get => WarningTracker.PushBaseCost; set => WarningTracker.PushBaseCost = value; }
        public double CostRatio { get => WarningTracker.CostRatio; set => WarningTracker.CostRatio = value; }
        public float WarningFollowSpeed { get => WarningTracker.WarningFollowSpeed; set => WarningTracker.WarningFollowSpeed = value; }

        /// <summary>地面が盤面全体に達した(=これ以上プレイできない)瞬間に発行される。</summary>
        public event Action OnGroundFull
        {
            add => RiseAnimator.OnGroundFull += value;
            remove => RiseAnimator.OnGroundFull -= value;
        }

        public GroundLevelController(Grid grid, int blockSize)
        {
            int maxLevelBlocks = grid.Height / blockSize;

            RiseAnimator = new GroundRiseAnimator(grid, blockSize);
            WarningTracker = new GroundWarningTracker(Contest, RiseAnimator, maxLevelBlocks);

            // 自分が受ける攻撃が確定したら、その分だけ地面をせり上げる
            Contest.OnAttackLanded += HandleAttackLanded;
        }

        public void SetOpponentScore(int opponentTotalScore)
        {
            if (!_opponentBaselineSet)
            {
                _lastOpponentScore = opponentTotalScore;
                _opponentBaselineSet = true;
            }
            _opponentScore = opponentTotalScore;
        }

        void HandleAttackLanded(int pressure)
        {
            float height = GroundLevelCurve.GetLevel(pressure, WarningTracker.PushBaseCost, WarningTracker.CostRatio);
            if (height <= 0f) return;

            RiseAnimator.BeginRise(height);

            // 警告表示を、確定した最終到達高さで静止させる
            // (「ここまで上がる」と示してから消える演出。せめぎ合いが待機中に戻ったことで
            // 警告がスッと下がっていく、という見え方を防ぐため)
            WarningTracker.LockToConfirmedLevel(RiseAnimator.TargetLevel);
        }

        /// <summary>毎フレーム呼ぶ。自分の合計スコアは呼び出し側(Presenter)から渡してもらう。</summary>
        public void Tick(float deltaTime, int ownTotalScore)
        {
            // 得点の「増えた分」だけをせめぎ合いに流し込む(合計スコアそのものではなく差分)
            int ownDelta = ownTotalScore - _lastOwnScore;
            _lastOwnScore = ownTotalScore;
            if (ownDelta > 0) Contest.AddMyPoints(ownDelta);

            if (_opponentBaselineSet)
            {
                int opponentDelta = _opponentScore - _lastOpponentScore;
                _lastOpponentScore = _opponentScore;
                if (opponentDelta > 0) Contest.AddOpponentPoints(opponentDelta);
            }

            Contest.Tick(deltaTime);
            WarningTracker.Tick(deltaTime);
            RiseAnimator.Tick(deltaTime);
        }
    }
}