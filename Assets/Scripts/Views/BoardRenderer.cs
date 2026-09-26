using UnityEngine;

namespace SandTetris
{
    /// <summary>
    /// BoardModelの状態をTexture2Dに描画するだけのクラス。ゲームロジックは一切持たない。
    /// 自分の盤面・相手の盤面のどちらにも同じインスタンスの作り方で使い回せる。
    /// </summary>
    public class BoardRenderer
    {
        public readonly int WidthPx;
        public readonly int HeightPx;

        /// <summary>直接書き込める作業用バッファ。演出Observerがここに追加で重ね書きしてもいい。</summary>
        public readonly Color32[] Buffer;

        readonly Color32[] _flipped;

        public Texture2D Texture { get; }
        public Color32 BackgroundColor = new Color32(18, 18, 24, 255);

        public BoardRenderer(int widthPx, int heightPx)
        {
            WidthPx = widthPx;
            HeightPx = heightPx;
            Buffer = new Color32[widthPx * heightPx];
            _flipped = new Color32[widthPx * heightPx];

            Texture = new Texture2D(widthPx, heightPx, TextureFormat.RGBA32, false);
            Texture.filterMode = FilterMode.Point;
            Texture.wrapMode = TextureWrapMode.Clamp;
        }

        /// <summary>盤面(グリッド + 操作中ミノ)をBufferに描く。</summary>
        public void DrawBoard(BoardModel model)
        {
            model.Grid.WriteTo(Buffer, BackgroundColor);

            var piece = model.CurrentPiece;
            if (piece == null) return;

            // 操作中ミノは常に描画する(ライン消去中かどうかで分岐させないこと。
            // 分岐させると「消去中に操作中ミノが消える」バグを再発させる)。
            for (int i = 0; i < piece.Offsets.Count; i++)
            {
                var p = piece.Anchor + piece.Offsets[i];
                if (model.Grid.InBounds(p.x, p.y))
                {
                    Buffer[model.Grid.Index(p.x, p.y)] = piece.PixelColors[i];
                }
            }
        }

        /// <summary>指定インデックスのセルを単色で塗りつぶす(ライン消去のハイライト用)。</summary>
        public void DrawHighlight(System.Collections.Generic.IReadOnlyList<int> indices, Color32 color)
        {
            foreach (var idx in indices) Buffer[idx] = color;
        }

        /// <summary>Bufferの内容を上下反転させてTextureに反映する。毎フレーム最後に呼ぶ。</summary>
        public void Upload()
        {
            for (int y = 0; y < HeightPx; y++)
            {
                int srcRow = y * WidthPx;
                int dstRow = (HeightPx - 1 - y) * WidthPx;
                System.Array.Copy(Buffer, srcRow, _flipped, dstRow, WidthPx);
            }

            Texture.SetPixels32(_flipped);
            Texture.Apply(false);
        }
    }
}