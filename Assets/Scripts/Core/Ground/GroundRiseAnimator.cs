using System;
using UnityEngine;

namespace SandTetris
{
    /// <summary>
    /// 地面がせり上がる、実際のアニメーションを担当する。
    /// 攻撃が確定した高さまで、RiseSeconds かけてなめらかに上げ、上がりきったら灰に変える。
    /// </summary>
    public class GroundRiseAnimator
    {
        readonly Grid _grid;
        readonly int _blockSize;
        readonly int _maxLevelBlocks;

        /// <summary>攻撃が確定してから、地面が目標の高さまで上がりきるのにかける秒数。</summary>
        public float RiseSeconds = 3f;

        /// <summary>
        /// せり上がっている間の地面の色。灰になった後も、この同じ色をそのまま使う。
        /// </summary>
        public Color32 GroundColor = new Color32(90, 90, 95, 255);

        bool _rising;
        float _riseStartLevel;
        float _riseTargetLevel;
        float _riseElapsed;
        float _currentLevel;

        public bool IsRising => _rising;

        /// <summary>今向かっている、確定した目標の高さ(段数)。IsRisingがfalseのときは意味を持たない。</summary>
        public float TargetLevel => _riseTargetLevel;

        /// <summary>現在の地面の高さ(段数、小数を含む)。せり上がっていない間は0。</summary>
        public float CurrentLevel => _currentLevel;

        /// <summary>地面が盤面全体に達した(=これ以上プレイできない)瞬間に発行される。</summary>
        public event Action OnGroundFull;

        /// <summary>せり上がりが完了した瞬間に発行される(警告表示のリセットなどに使う)。</summary>
        public event Action OnRiseCompleted;

        public GroundRiseAnimator(Grid grid, int blockSize)
        {
            _grid = grid;
            _blockSize = blockSize;
            _maxLevelBlocks = grid.Height / blockSize;
        }

        /// <summary>
        /// 新しい高さまでのせり上がりを開始する。すでにせり上がっている最中なら、
        /// いまの高さを起点に、目標を積み増して、時間を最初からやり直す。
        /// </summary>
        public void BeginRise(float height)
        {
            _riseStartLevel = _rising ? _currentLevel : 0f;
            _riseTargetLevel = _riseStartLevel + height;
            _riseElapsed = 0f;
            _rising = true;
        }

        public void Tick(float deltaTime)
        {
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
                // 上がりきった: 止まらずにそのまま、地面の全体が灰に変わる
                _grid.AshColor = GroundColor; // オブジェクト初期化子で後から設定されるため、使う直前に同期させる
                _grid.ConvertGroundToAsh();
                _currentLevel = 0f;
                _rising = false;
                OnRiseCompleted?.Invoke();
            }
        }
    }
}