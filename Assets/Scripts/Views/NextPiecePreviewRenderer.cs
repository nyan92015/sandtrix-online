using UnityEngine;

namespace SandTetris
{
    /// <summary>
    /// BoardModel.NextPiece を、盤面とは別の小さなテクスチャに中央寄せで描画するだけのクラス。
    /// BoardRendererと同様、ゲームロジックは一切持たない。
    /// </summary>
    public class NextPiecePreviewRenderer
    {
        public readonly int SizePx;
        readonly Color32[] _buffer;
        readonly Color32[] _flipped;

        public Texture2D Texture { get; }
        public Color32 BackgroundColor = new Color32(30, 30, 38, 255);

        public NextPiecePreviewRenderer(int sizePx)
        {
            SizePx = sizePx;
            _buffer = new Color32[sizePx * sizePx];
            _flipped = new Color32[sizePx * sizePx];

            Texture = new Texture2D(sizePx, sizePx, TextureFormat.RGBA32, false);
            Texture.filterMode = FilterMode.Point;
            Texture.wrapMode = TextureWrapMode.Clamp;
        }

        /// <summary>piece が null なら背景色だけで塗りつぶす。</summary>
        public void Render(FallingPiece piece)
        {
            for (int i = 0; i < _buffer.Length; i++) _buffer[i] = BackgroundColor;

            if (piece != null)
            {
                int center = SizePx / 2;
                for (int i = 0; i < piece.Offsets.Count; i++)
                {
                    var o = piece.Offsets[i];
                    int x = center + o.x;
                    int y = center + o.y;
                    if (x >= 0 && x < SizePx && y >= 0 && y < SizePx)
                    {
                        _buffer[y * SizePx + x] = piece.PixelColors[i];
                    }
                }
            }

            // Texture2D は左下原点なので、上下反転させてから書き込む
            for (int y = 0; y < SizePx; y++)
            {
                int srcRow = y * SizePx;
                int dstRow = (SizePx - 1 - y) * SizePx;
                System.Array.Copy(_buffer, srcRow, _flipped, dstRow, SizePx);
            }

            Texture.SetPixels32(_flipped);
            Texture.Apply(false);
        }
    }
}