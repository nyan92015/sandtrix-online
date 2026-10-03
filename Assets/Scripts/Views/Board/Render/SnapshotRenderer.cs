using UnityEngine;

namespace SandTetris
{
    /// <summary>
    /// ネットワーク経由で受け取ったスナップショット(BoardSnapshotCodec の出力)から、
    /// 相手の盤面を描画する処理を担当する。
    /// </summary>
    public class SnapshotRenderer
    {
        readonly BoardRenderer _renderer;

        /// <summary>相手の盤面表示で、コンクリート(スナップショット値5)を描くときの色。</summary>
        public Color32 ConcreteColor = new Color32(90, 90, 95, 255);
        public const byte ConcreteSnapshotValue = 5;

        /// <summary>相手の盤面表示で、灰(スナップショット値6)を描くときの色。</summary>
        public Color32 AshColor = new Color32(150, 150, 155, 255);
        public const byte AshSnapshotValue = 6;

        public SnapshotRenderer(BoardRenderer renderer)
        {
            _renderer = renderer;
        }

        /// <summary>
        /// スナップショット(ContentWidthPx×HeightPx分の長さ)から直接描画する。相手の盤面表示用。
        /// 0=背景、1〜4=paletteの色、5=コンクリート、6=灰、7=白。ベタ塗り。額縁は通常の色で塗る。
        /// </summary>
        public void DrawFromSnapshot(byte[] snapshot, Color32[] palette)
        {
            _renderer.FillFrame(_renderer.FrameColor);

            int contentWidthPx = _renderer.ContentWidthPx;
            int heightPx = _renderer.HeightPx;
            int count = Mathf.Min(contentWidthPx * heightPx, snapshot.Length);

            for (int i = 0; i < count; i++)
            {
                int cx = i % contentWidthPx;
                int y = i / contentWidthPx;

                byte v = snapshot[i];
                Color32 c;
                if (v == 0) c = _renderer.BackgroundColor;
                else if (v == ConcreteSnapshotValue) c = ConcreteColor;
                else if (v == AshSnapshotValue) c = AshColor;
                else if (v == BoardSnapshotCodec.WhiteValue) c = new Color32(255, 255, 255, 255);
                else c = palette[(v - 1) % palette.Length];

                _renderer.Buffer[_renderer.BufferIndex(cx, y)] = c;
            }
        }
    }
}