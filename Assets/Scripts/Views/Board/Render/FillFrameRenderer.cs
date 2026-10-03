using UnityEngine;

namespace SandTetris
{
    /// <summary>
    /// 額縁(盤面の左右に確保された帯)の塗りつぶしを担当する。
    /// 自分の盤面描画(BoardRenderer.DrawBoard)・相手のスナップショット描画(SnapshotRenderer)、
    /// 両方から共通で使われる。
    /// </summary>
    public class FillFrameRenderer
    {
        readonly BoardRenderer _renderer;

        public FillFrameRenderer(BoardRenderer renderer)
        {
            _renderer = renderer;
        }

        /// <summary>左右の額縁を、指定した色で塗りつぶす。</summary>
        public void FillFrame(Color32 color)
        {
            int widthPx = _renderer.WidthPx;
            int heightPx = _renderer.HeightPx;
            int frameThicknessPx = _renderer.FrameThicknessPx;
            var buffer = _renderer.Buffer;

            if (frameThicknessPx <= 0) return;

            for (int y = 0; y < heightPx; y++)
            {
                int rowStart = y * widthPx;
                for (int fx = 0; fx < frameThicknessPx; fx++)
                {
                    buffer[rowStart + fx] = color;
                    buffer[rowStart + widthPx - 1 - fx] = color;
                }
            }
        }
    }
}