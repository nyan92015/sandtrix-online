using UnityEngine;

namespace SandTetris
{
    /// <summary>
    /// 自分が守備側で、かつ基準点を超えているときの「今の圧力のまま確定したら、どこまで上がるか」の
    /// 表示用の値を、なめらかに追従させる。
    ///
    /// せり上がりが確定している間(GroundRiseAnimator.IsRising)は、その目標の高さで表示を静止させ、
    /// せり上がりが完了した瞬間に0へ戻す。それ以外のときは、せめぎ合いの状況から計算した目標へ
    /// なめらかに追いかける。
    /// </summary>
    public class GroundWarningTracker
    {
        readonly AttackContest _contest;
        readonly GroundRiseAnimator _riseAnimator;
        readonly int _maxLevelBlocks;

        /// <summary>
        /// 圧力から高さを決めるときの、1段目の基準コスト。
        /// 1回の攻撃(せめぎ合いの窓1つ分)で溜まる圧力に対する値。
        /// </summary>
        public double PushBaseCost = 3000.0;

        /// <summary>1段上がるごとに追加コストが何倍になるか。</summary>
        public double CostRatio = GroundLevelCurve.DefaultRatio;

        /// <summary>
        /// 警告表示の高さが、目標(今の圧力から計算した値)を追いかける速さ(1秒あたり段数)。
        /// 大きいほど、得点が入ったときの反応がきびきびする。
        /// </summary>
        public float WarningFollowSpeed = 3f;

        float _warningDisplayLevel;

        /// <summary>表示用の、警告の高さ(段数)。</summary>
        public float PendingWarningLevel => _warningDisplayLevel;

        public GroundWarningTracker(AttackContest contest, GroundRiseAnimator riseAnimator, int maxLevelBlocks)
        {
            _contest = contest;
            _riseAnimator = riseAnimator;
            _maxLevelBlocks = maxLevelBlocks;

            // せり上がりが完了した瞬間、警告表示をスッと0に戻す
            // (完了直後のなめらかな追従だと、一瞬だけ中途半端な高さが見えてしまうため)
            _riseAnimator.OnRiseCompleted += () => _warningDisplayLevel = 0f;
        }

        /// <summary>攻撃が確定した瞬間に呼ぶ。最終到達高さで、表示をいったん静止させる。</summary>
        public void LockToConfirmedLevel(float level)
        {
            _warningDisplayLevel = level;
        }

        public void Tick(float deltaTime)
        {
            if (_riseAnimator.IsRising)
            {
                // せり上がり確定演出の最中は、確定した高さで静止させ続ける
                _warningDisplayLevel = _riseAnimator.TargetLevel;
                return;
            }

            float target = ComputeWarningTargetLevel();
            _warningDisplayLevel = Mathf.MoveTowards(_warningDisplayLevel, target, WarningFollowSpeed * deltaTime);
        }

        float ComputeWarningTargetLevel()
        {
            if (!_contest.IsDefending) return 0f;
            int magnitude = System.Math.Abs(_contest.Pressure);
            if (magnitude < _contest.Threshold) return 0f;

            float level = GroundLevelCurve.GetLevel(magnitude, PushBaseCost, CostRatio);
            return System.Math.Min(level, _maxLevelBlocks);
        }
    }
}