using System.Collections.Generic;
using UnityEngine;

namespace SandTetris
{
    /// <summary>
    /// 着地したミノを一瞬だけ光らせる演出。BoardModel.OnPieceLanded を購読するだけで、
    /// ゲームの進行(Presenter/State)には一切関与しない、完全に独立したオブザーバー。
    /// </summary>
    public class LandingFlashEffect
    {
        readonly List<Vector2Int> _positions = new List<Vector2Int>();
        readonly float _duration;
        float _timer;

        public Color32 FlashColor = new Color32(255, 255, 255, 255);

        public LandingFlashEffect(BoardModel model, float duration)
        {
            _duration = duration;
            model.OnPieceLanded += HandlePieceLanded;
        }

        void HandlePieceLanded(IReadOnlyList<Vector2Int> positions, IReadOnlyList<Color32> colors, byte colorIndex)
        {
            _positions.Clear();
            _positions.AddRange(positions);
            _timer = _duration;
        }

        /// <summary>毎フレーム呼ぶ。時間経過だけを進める。</summary>
        public void Tick(float deltaTime)
        {
            if (_timer > 0f) _timer -= deltaTime;
        }

        /// <summary>
        /// 描画バッファにフラッシュを重ね書きする。BoardRenderer.DrawBoard の後、
        /// Upload の前に呼ぶこと。
        /// </summary>
        public void ApplyOverlay(Color32[] buffer, SandGrid grid)
        {
            if (_timer <= 0f) return;

            float t = Mathf.Clamp01(_timer / _duration);
            foreach (var p in _positions)
            {
                if (!grid.InBounds(p.x, p.y)) continue;
                int idx = grid.Index(p.x, p.y);
                if (!grid.Cells[idx].Occupied) continue; // すでに崩れて移動した粒はスキップ
                buffer[idx] = Color32.Lerp(buffer[idx], FlashColor, t);
            }
        }
    }
}