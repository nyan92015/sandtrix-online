using System.Collections.Generic;
using UnityEngine;

namespace SandTetris
{
    /// <summary>
    /// ライン消去が確定した瞬間(BoardModel.OnLinesFound)から、対象の砂を
    /// 「白と元の色で点滅させながら、左から右へ順に消えていく」演出。
    ///
    /// 独自のタイマーは持たない。LineClearState自身が「実際に消去するまでの
    /// 経過時間・合計時間」をすでに管理しているので、それをそのまま毎フレーム
    /// 渡してもらう(ApplyOverlayの引数)。
    /// 独自にタイマーを持つと、ステート切り替え直後の1フレームだけ「演出側は時間が
    /// 進んでいるのに、実際の消去タイマーはまだ動いていない」というズレが起き、
    /// 消去直前に一瞬だけ元の砂が見えてしまうバグの原因になるため。
    /// </summary>
    public class LineClearSweepFlashEffect
    {
        readonly Grid _grid;

        /// <summary>白と元の色を切り替える間隔(秒)。</summary>
        public float BlinkInterval = 0.08f;

        /// <summary>
        /// 「左から右」という大まかな流れは保ちつつ、粒ごとに消えるタイミングをどれだけ前後させるか(0〜1程度)。
        /// 0にすると、きれいに縦一直線で消えていく(ズレなし)。大きくするほどバラバラ感が増す。
        /// </summary>
        public float ScatterAmount = 0.15f;

        List<int> _indices;
        List<float> _jitter; // 粒ごとに、消去が確定した瞬間だけ決めるランダムなズレ。毎フレーム変えるとチラつくため
        int _minX;
        int _maxX;
        bool _hasData;

        public LineClearSweepFlashEffect(BoardModel model)
        {
            _grid = model.Grid;
            model.OnLinesFound += HandleLinesFound;
        }

        void HandleLinesFound(IReadOnlyList<int> clearedIndices)
        {
            _indices = new List<int>(clearedIndices);
            _hasData = _indices.Count > 0;

            _minX = int.MaxValue;
            _maxX = int.MinValue;
            foreach (var idx in _indices)
            {
                int x = idx % _grid.Width;
                if (x < _minX) _minX = x;
                if (x > _maxX) _maxX = x;
            }

            _jitter = new List<float>(_indices.Count);
            foreach (var _ in _indices)
            {
                _jitter.Add(Random.Range(-ScatterAmount, ScatterAmount));
            }
        }

        /// <summary>
        /// 描画バッファに重ね書きする。BoardRenderer.DrawBoard の後、Upload の前に呼ぶこと。
        /// elapsed・duration は、呼び出し側(LineClearState)が持つ本物のタイマーの値を、
        /// そのまま渡してもらう想定。
        /// </summary>
        public void ApplyOverlay(BoardRenderer renderer, float elapsed, float duration)
        {
            if (!_hasData) return;
            if (duration <= 0f) return;
            if (elapsed >= duration) return; // すでに演出の対象期間を過ぎている

            float progress = Mathf.Clamp01(elapsed / duration); // 0(まだ)〜1(右端まで消え終わった)
            float width = Mathf.Max(1, _maxX - _minX);
            bool blinkOn = ((int)(elapsed / BlinkInterval) & 1) == 0;

            for (int i = 0; i < _indices.Count; i++)
            {
                int idx = _indices[i];
                int x = idx % _grid.Width;
                int y = idx / _grid.Width;
                // 大まかな「左から右」の位置に、粒ごとの固定ズレ(_jitter)を加えて、バラバラ感を出す
                float cellProgress = (x - _minX) / width + _jitter[i];

                int bufIdx = renderer.BufferIndex(x, y);

                if (cellProgress <= progress)
                {
                    // 進行ラインが通り過ぎたので、もう消えた扱い
                    renderer.Buffer[bufIdx] = renderer.BackgroundColor;
                }
                else
                {
                    // まだ消えていない: 白と元の色を交互に点滅
                    renderer.Buffer[bufIdx] = blinkOn ? new Color32(255, 255, 255, 255) : _grid.Cells[idx].Color;
                }
            }
        }

        /// <summary>
        /// 今まさに「白」で表示されているセルのインデックス一覧を返す。
        /// ネットワーク送信用のスナップショットに、白を反映させるために使う
        /// (elapsed・duration は ApplyOverlay と同じく、呼び出し側のLineClearStateから渡してもらう)。
        /// </summary>
        public List<int> GetCurrentlyWhiteIndices(float elapsed, float duration)
        {
            var result = new List<int>();
            if (!_hasData || duration <= 0f || elapsed >= duration) return result;

            float progress = Mathf.Clamp01(elapsed / duration);
            float width = Mathf.Max(1, _maxX - _minX);
            bool blinkOn = ((int)(elapsed / BlinkInterval) & 1) == 0;
            if (!blinkOn) return result; // 今は「元の色」のフェーズ: 白いセルはない

            for (int i = 0; i < _indices.Count; i++)
            {
                int idx = _indices[i];
                int x = idx % _grid.Width;
                float cellProgress = (x - _minX) / width + _jitter[i];
                if (cellProgress > progress) result.Add(idx); // まだ消えていない(=白く点滅中)
            }
            return result;
        }

        /// <summary>
        /// 今まさに「もう消えた(背景色)」ように見えているセルのインデックス一覧を返す。
        /// 本物のデータ(SandGrid.Cells)自体は、まだ消去が確定するまで変わらないままなので、
        /// 相手側のスナップショットには、このセルを「空(0)」として送ってもらう必要がある。
        /// そうしないと、相手の画面では「点滅はしているのに、本物の消去が確定した瞬間に
        /// 全部まとめて一気に消える」という見え方になってしまう。
        /// </summary>
        public List<int> GetCurrentlyGoneIndices(float elapsed, float duration)
        {
            var result = new List<int>();
            if (!_hasData || duration <= 0f || elapsed >= duration) return result;

            float progress = Mathf.Clamp01(elapsed / duration);
            float width = Mathf.Max(1, _maxX - _minX);

            for (int i = 0; i < _indices.Count; i++)
            {
                int idx = _indices[i];
                int x = idx % _grid.Width;
                float cellProgress = (x - _minX) / width + _jitter[i];
                if (cellProgress <= progress) result.Add(idx); // 進行ラインが通り過ぎた(もう消えた扱い)
            }
            return result;
        }
    }
}