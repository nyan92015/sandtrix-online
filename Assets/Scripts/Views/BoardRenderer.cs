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
            DrawPiece(model.CurrentPiece);
        }

        /// <summary>
        /// ミノ(操作中、またはネットワーク越しに再構築したもの)をBufferに重ね描きする。
        /// piece が null なら何もしない。ライン消去中かどうかで分岐させないこと
        /// (分岐させると「消去中に操作中ミノが消える」バグを再発させる)。
        /// </summary>
        public void DrawPiece(FallingPiece piece)
        {
            if (piece == null) return;

            for (int i = 0; i < piece.Offsets.Count; i++)
            {
                var p = piece.Anchor + piece.Offsets[i];
                if (p.x >= 0 && p.x < WidthPx && p.y >= 0 && p.y < HeightPx)
                {
                    Buffer[p.y * WidthPx + p.x] = piece.PixelColors[i];
                }
            }
        }

        /// <summary>指定インデックスのセルを単色で塗りつぶす(ライン消去のハイライト用)。</summary>
        public void DrawHighlight(System.Collections.Generic.IReadOnlyList<int> indices, Color32 color)
        {
            foreach (var idx in indices) Buffer[idx] = color;
        }

        /// <summary>
        /// ネットワーク経由で受け取ったスナップショット(BoardSnapshotCodec.Encode の出力)から
        /// 直接描画する。相手の盤面表示用。0=背景、1〜N=paletteのN-1番目の色。ベタ塗り。
        /// </summary>
        public void DrawFromSnapshot(byte[] snapshot, Color32[] palette)
        {
            int count = Mathf.Min(Buffer.Length, snapshot.Length);
            for (int i = 0; i < count; i++)
            {
                byte v = snapshot[i];
                Buffer[i] = v == 0 ? BackgroundColor : palette[(v - 1) % palette.Length];
            }
        }

        /// <summary>
        /// BoardSnapshotCodec.EncodeCoarse の出力(マス単位のデータ)を、
        /// 1マスあたり blockSize×blockSize ピクセルに拡大して描画する。
        /// </summary>
        public void DrawFromBlockSnapshot(byte[] snapshot, int blockSize, int widthInBlocks, int heightInBlocks, Color32[] palette)
        {
            for (int by = 0; by < heightInBlocks; by++)
            {
                for (int bx = 0; bx < widthInBlocks; bx++)
                {
                    byte v = snapshot[by * widthInBlocks + bx];
                    Color32 color = v == 0 ? BackgroundColor : palette[(v - 1) % palette.Length];

                    int startX = bx * blockSize;
                    int startY = by * blockSize;
                    for (int dx = 0; dx < blockSize; dx++)
                    {
                        for (int dy = 0; dy < blockSize; dy++)
                        {
                            int x = startX + dx;
                            int y = startY + dy;
                            if (x < WidthPx && y < HeightPx)
                            {
                                Buffer[y * WidthPx + x] = color;
                            }
                        }
                    }
                }
            }
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