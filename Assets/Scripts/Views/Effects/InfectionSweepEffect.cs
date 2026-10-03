using System.Collections.Generic;
using UnityEngine;

namespace SandTetris
{
    /// <summary>
    /// 特別なミノが着地して、周囲の砂を感染させた瞬間の演出。
    /// LineClearSweepFlashEffect(ライン消去の点滅)と同じ発想だが、
    /// 「左から右」ではなく「着地点を中心に、円形に外側へ広がっていく」点が違う。
    ///
    /// 実際のデータ(SandGrid.Cells)は、BoardModel.OnInfectionが発火した時点で、
    /// すでに新しい色に変わっている。この演出は、まだ「自分の番」が来ていないセルだけを、
    /// 白と元の色で点滅させながら上書きし続けることで、新しい色が後から追いついてくる
    /// ように見せているだけ(データ自体を遅らせているわけではない)。
    /// </summary>
    public class InfectionSweepEffect
    {
        readonly int _gridWidth;

        /// <summary>中心から外側まで広がりきるまでの、全体の時間(秒)。</summary>
        public float Duration = 0.4f;

        /// <summary>まだ届いていない場所の、柔らかい明滅の速さ。</summary>
        public float FlashSpeed = 10f;

        /// <summary>まだ届いていない場所の明滅で、白をどれだけ混ぜるか(0〜1)の最大値。1だと真っ白まで行く。</summary>
        public float FlashMaxBlend = 0.6f;

        /// <summary>
        /// 衝撃波の輪の太さ(0〜1の割合、広がり全体に対する比率)。
        /// 届いたばかりの場所がこの太さぶんだけ強く白く光り、外側へ広がりながら消えていく。
        /// </summary>
        public float RingWidth = 0.35f;

        List<int> _indices;
        List<Color32> _oldColors;
        Vector2 _center;
        float _maxDistance;
        float _elapsed;
        bool _active;

        public InfectionSweepEffect(BoardModel model)
        {
            _gridWidth = model.Grid.Width;
            model.OnInfection += HandleInfection;
        }

        void HandleInfection(IReadOnlyList<int> changedIndices, IReadOnlyList<Color32> oldColors, Vector2Int center)
        {
            _indices = new List<int>(changedIndices);
            _oldColors = new List<Color32>(oldColors);
            _center = new Vector2(center.x, center.y);
            _elapsed = 0f;
            _active = _indices.Count > 0;

            _maxDistance = 0.001f; // 0除算を避けるための、ごく小さい下限
            foreach (var idx in _indices)
            {
                int x = idx % _gridWidth;
                int y = idx / _gridWidth;
                float d = Vector2.Distance(_center, new Vector2(x, y));
                if (d > _maxDistance) _maxDistance = d;
            }
        }

        /// <summary>毎フレーム呼ぶ。</summary>
        public void Tick(float deltaTime)
        {
            if (!_active) return;
            _elapsed += deltaTime;
            if (_elapsed >= Duration) _active = false;
        }

        /// <summary>
        /// 描画バッファに重ね書きする。BoardRenderer.DrawBoard の後、Upload の前に呼ぶこと。
        /// </summary>
        static readonly Color32 White = new Color32(255, 255, 255, 255);

        public void ApplyOverlay(BoardRenderer renderer)
        {
            if (!_active) return;

            float progress = Mathf.Clamp01(_elapsed / Duration); // 0(中心のみ)〜1(外側まで広がりきった)

            // まだ届いていない場所の、なめらかな明滅(パキッと切り替えず、サイン波で白をブレンドする)
            float wave = (Mathf.Sin(_elapsed * FlashSpeed) + 1f) * 0.5f; // 0〜1
            float softBlend = wave * FlashMaxBlend;

            for (int i = 0; i < _indices.Count; i++)
            {
                int idx = _indices[i];
                int x = idx % _gridWidth;
                int y = idx / _gridWidth;

                float distance = Vector2.Distance(_center, new Vector2(x, y));
                float cellProgress = distance / _maxDistance; // 0(中心)〜1(一番遠い)

                int bufIdx = renderer.BufferIndex(x, y);

                if (cellProgress > progress)
                {
                    // まだ届いていない: 柔らかく明滅する元の色
                    renderer.Buffer[bufIdx] = Color32.Lerp(_oldColors[i], White, softBlend);
                }
                else
                {
                    // もう届いている: 本物の新しい色の上から、届いた直後だけ強く白く光らせ、
                    // 輪が通り過ぎるにつれてフェードさせる(衝撃波の輪)
                    float sinceReached = progress - cellProgress; // 0(届いた瞬間)〜大きい(ずっと前に届いた)
                    if (sinceReached < RingWidth)
                    {
                        float ringIntensity = 1f - sinceReached / RingWidth; // 1(届いた瞬間)→0(輪が過ぎた)
                        renderer.Buffer[bufIdx] = Color32.Lerp(renderer.Buffer[bufIdx], White, ringIntensity);
                    }
                    // 輪が過ぎたあとは何もしない(すでにDrawBoardで本物の色が描かれている)
                }
            }
        }

        /// <summary>
        /// 今まさに「白に近い」見た目になっているセルのインデックス一覧を返す。
        /// 連続的なブレンドなので、「半分以上白寄り」を閾値として、白とみなすかどうかを決める。
        /// ネットワーク送信用のスナップショットに、白を反映させるために使う。
        /// </summary>
        public List<int> GetCurrentlyWhiteIndices()
        {
            var result = new List<int>();
            if (!_active) return result;

            float progress = Mathf.Clamp01(_elapsed / Duration);
            float wave = (Mathf.Sin(_elapsed * FlashSpeed) + 1f) * 0.5f;
            float softBlend = wave * FlashMaxBlend;

            for (int i = 0; i < _indices.Count; i++)
            {
                int idx = _indices[i];
                int x = idx % _gridWidth;
                int y = idx / _gridWidth;
                float distance = Vector2.Distance(_center, new Vector2(x, y));
                float cellProgress = distance / _maxDistance;

                if (cellProgress > progress)
                {
                    if (softBlend > 0.5f) result.Add(idx);
                }
                else
                {
                    float sinceReached = progress - cellProgress;
                    if (sinceReached < RingWidth)
                    {
                        float ringIntensity = 1f - sinceReached / RingWidth;
                        if (ringIntensity > 0.5f) result.Add(idx);
                    }
                }
            }
            return result;
        }
    }
}