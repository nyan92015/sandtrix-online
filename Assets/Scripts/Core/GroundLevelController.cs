using System;
using UnityEngine;

namespace SandTetris
{
    /// <summary>
    /// 「シーソー式コンクリート地面」の高さと、その最期(灰になって流れ出る)を管理する。
    ///
    /// 流れ:
    /// 1. 自分と相手の得点の増分を AttackContest(せめぎ合いのルール)に流し込む
    /// 2. 自分が受ける攻撃が確定したら、その圧力を GroundLevelCurve(段ごとに重くなるカーブ)で
    ///    段数に変換し、RiseSeconds かけてその高さまで地面をせり上げる(砂も一緒に持ち上がる)
    /// 3. 上がりきったら、地面(コンクリート)の全体が灰に変わる。止まっている時間はない
    /// 4. 灰は砂と同じ物理で落ちていき、盤面の底に触れた瞬間に消える(SandGrid.SimulateStep)
    ///
    /// 自分の得点は、自分の地面を直接下げることには使わず、せめぎ合いでの相殺にだけ使う。
    /// </summary>
    public class GroundLevelController
    {
        readonly SandGrid _grid;
        readonly int _blockSize;
        readonly int _maxLevelBlocks;

        /// <summary>せめぎ合いのルール本体。UI表示(圧力・残り時間)などから参照できる。</summary>
        public AttackContest Contest { get; } = new AttackContest();

        /// <summary>攻撃が確定してから、地面が目標の高さまで上がりきるのにかける秒数。</summary>
        public float RiseSeconds = 3f;

        /// <summary>
        /// 圧力から高さを決めるときの、1段目の基準コスト。
        /// 1回の攻撃(せめぎ合いの窓1つ分)で溜まる圧力に対する値。
        /// </summary>
        public double PushBaseCost = 3000.0;

        /// <summary>1段上がるごとに追加コストが何倍になるか。</summary>
        public double CostRatio = GroundLevelCurve.DefaultRatio;

        public Color32 GroundColor = new Color32(90, 90, 95, 255);
        public Color32 AshColor = new Color32(150, 150, 155, 255);

        // せり上がりの状態
        bool _rising;
        float _riseStartLevel;
        float _riseTargetLevel;
        float _riseElapsed;

        int _lastOwnScore;
        int _lastOpponentScore;
        int _opponentScore;
        bool _opponentBaselineSet;
        float _currentLevel;

        /// <summary>現在の地面の高さ(段数、小数を含む)。せり上がっていない間は0。</summary>
        public float CurrentLevel => _currentLevel;

        /// <summary>
        /// 警告表示の高さが、目標(今の圧力から計算した値)を追いかける速さ(1秒あたり段数)。
        /// 大きいほど、得点が入ったときの反応がきびきびする。
        /// </summary>
        public float WarningFollowSpeed = 3f;

        float _warningDisplayLevel;
        bool _warningConfirmActive;

        /// <summary>
        /// 自分が守備側で、かつ基準点を超えているときの「今の圧力のまま確定したら、どこまで上がるか」を、
        /// 目標の変化に対してなめらかに追いかけた、表示用の値。警告表示に使う想定。
        /// 攻撃側/待機中、または基準点未満のときは、目標が0になるので、この値も0へなめらかに戻る。
        /// </summary>
        public float PendingWarningLevel => _warningDisplayLevel;

        float ComputeWarningTargetLevel()
        {
            if (!Contest.IsDefending) return 0f;
            int magnitude = Math.Abs(Contest.Pressure);
            if (magnitude < Contest.Threshold) return 0f;

            float level = GroundLevelCurve.GetLevel(magnitude, PushBaseCost, CostRatio);
            return Math.Min(level, _maxLevelBlocks);
        }

        /// <summary>地面が盤面全体に達した(=これ以上プレイできない)瞬間に発行される。</summary>
        public event Action OnGroundFull;

        public GroundLevelController(SandGrid grid, int blockSize)
        {
            _grid = grid;
            _blockSize = blockSize;
            _maxLevelBlocks = grid.Height / blockSize;

            // 自分が受ける攻撃が確定したら、その分だけ地面をせり上げる
            Contest.OnAttackLanded += HandleAttackLanded;
        }

        /// <summary>
        /// 相手の合計スコアを伝える。ネットワークから受け取った値を毎フレーム渡す想定。
        /// 最初に受け取った値は「途中参加などで、すでに持っていた点数」かもしれないので、
        /// 得点の増加とは数えず、基準としてだけ使う。
        /// </summary>
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
            float height = GroundLevelCurve.GetLevel(pressure, PushBaseCost, CostRatio);
            if (height <= 0f) return;

            // すでにせり上がっている最中に次の攻撃が確定した場合は、いまの高さを起点に、
            // 目標を積み増して、時間を最初からやり直す。
            // (窓は最短でも10秒あるので、通常は前のせり上がり(3秒)より先に次が来ることはない)
            _riseStartLevel = _rising ? _currentLevel : 0f;
            _riseTargetLevel = _riseStartLevel + height;
            _riseElapsed = 0f;
            _rising = true;

            // 警告表示を、確定した最終到達高さで静止させる
            // (「ここまで上がる」と示してから消える演出。せめぎ合いが待機中に戻ったことで
            // 警告がスッと下がっていく、という見え方を防ぐため。
            // 消すタイミングは固定時間ではなく、実際のせり上がりがこの高さに追いつくまで、下のTickで判定する)
            _warningDisplayLevel = _riseTargetLevel;
            _warningConfirmActive = true;
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

            // 警告表示の高さの更新。
            // 確定演出の最中は、確定した高さで静止させ続ける(消すのはこの下、せり上がりが
            // 目標に追いついた瞬間)。それ以外のときだけ、通常通り「今の圧力から計算した目標」へ
            // なめらかに追いかける。
            if (_warningConfirmActive)
            {
                _warningDisplayLevel = _riseTargetLevel;
            }
            else
            {
                float warningTarget = ComputeWarningTargetLevel();
                _warningDisplayLevel = Mathf.MoveTowards(_warningDisplayLevel, warningTarget, WarningFollowSpeed * deltaTime);
            }

            if (!_rising) return;

            _riseElapsed += deltaTime;
            float t = RiseSeconds <= 0f ? 1f : _riseElapsed / RiseSeconds;
            if (t > 1f) t = 1f;

            float level = _riseStartLevel + (_riseTargetLevel - _riseStartLevel) * t;
            _currentLevel = Math.Min(level, _maxLevelBlocks);

            int heightPx = (int)Math.Round(_currentLevel * _blockSize);
            _grid.SetGroundHeightPx(heightPx, GroundColor);

            // 盤面全体に達したら、灰になる前にゲームオーバー(はみ出たときと同じ扱い)
            if (_grid.GroundHeightPx >= _grid.Height)
            {
                OnGroundFull?.Invoke();
            }

            if (t >= 1f)
            {
                // 実際のせり上がりが、警告が示していた高さにちょうど追いついた瞬間。
                // ここで初めて警告表示を消す(確定演出の終わり)。
                _warningConfirmActive = false;
                _warningDisplayLevel = 0f;

                // 上がりきった: 止まらずにそのまま、地面の全体が灰に変わる
                _grid.ConvertGroundToAsh(AshColor);
                _currentLevel = 0f;
                _rising = false;
            }
        }
    }
}