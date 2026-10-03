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
        /// Upload の前に呼ぶこと。座標変換(額縁のオフセット)は renderer に任せるので、
        /// このクラス自身は盤面の実際の描画レイアウト(額縁の有無・太さ)を知らなくていい。
        /// </summary>
        public void ApplyOverlay(BoardRenderer renderer, Grid grid)
        {
            if (_timer <= 0f) return;

            float t = Mathf.Clamp01(_timer / _duration);
            foreach (var p in _positions)
            {
                if (!grid.InBounds(p.x, p.y)) continue;
                int gridIdx = grid.Index(p.x, p.y);
                if (!grid.Cells[gridIdx].Occupied) continue; // すでに崩れて移動した粒はスキップ

                int bufIdx = renderer.BufferIndex(p.x, p.y);
                renderer.Buffer[bufIdx] = Color32.Lerp(renderer.Buffer[bufIdx], FlashColor, t);
            }
        }

        /// <summary>
        /// 今まさに「白に近い」見た目になっている位置の一覧を返す。
        /// 全体が同じ割合でブレンドされるので、閾値(半分以上)を超えていれば、まとめて白とみなす。
        /// ネットワーク送信用のスナップショットに、白を反映させるために使う。
        /// </summary>
        public IReadOnlyList<Vector2Int> GetCurrentlyWhitePositions()
        {
            if (_timer <= 0f) return System.Array.Empty<Vector2Int>();
            float t = Mathf.Clamp01(_timer / _duration);
            return t > 0.5f ? _positions : (IReadOnlyList<Vector2Int>)System.Array.Empty<Vector2Int>();
        }
    }
}