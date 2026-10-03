using UnityEngine;

namespace SandTetris
{
    /// <summary>
    /// BoardModelの状態をTexture2Dに描画する「本体」。バッファ・テクスチャの所有、
    /// 自分の盤面の描画(DrawBoard/DrawPiece)、専門クラス(Frame/SpecialPiece/Snapshot/GroundWarning)
    /// の組み立てを担当する。
    /// </summary>
    public class BoardRenderer
    {
        /// <summary>実際のゲーム内容の幅(盤面そのもの)。</summary>
        public readonly int ContentWidthPx;

        /// <summary>額縁の太さ(ピクセル)。左右それぞれに、この幅ぶんの帯を確保する。</summary>
        public readonly int FrameThicknessPx;

        /// <summary>絵全体の幅(ContentWidthPx + FrameThicknessPx×2)。Textureの実際の幅。</summary>
        public readonly int WidthPx;

        public readonly int HeightPx;

        /// <summary>直接書き込める作業用バッファ。演出Observerがここに追加で重ね書きしてもいい。</summary>
        public readonly Color32[] Buffer;

        readonly Color32[] _flipped;

        public Texture2D Texture { get; }
        public Color32 BackgroundColor = new Color32(18, 18, 24, 255);

        /// <summary>額縁の、警告が出ていないときの色。</summary>
        public Color32 FrameColor = new Color32(30, 30, 36, 255);

        /// <summary>額縁の塗りつぶしを担当する。</summary>
        public FillFrameRenderer Frame { get; }

        /// <summary>特別なミノの見た目(縁取りの明滅・濃い色)を担当する。</summary>
        public SpecialPieceRenderer SpecialPiece { get; }

        /// <summary>相手の盤面(スナップショット)描画を担当する。</summary>
        public SnapshotRenderer Snapshot { get; }

        /// <summary>せめぎ合いの警告表示を担当する。</summary>
        public GroundWarningRenderer GroundWarning { get; }

        public BoardRenderer(int contentWidthPx, int heightPx, int frameThicknessPx)
        {
            ContentWidthPx = contentWidthPx;
            FrameThicknessPx = frameThicknessPx;
            WidthPx = contentWidthPx + frameThicknessPx * 2;
            HeightPx = heightPx;

            Buffer = new Color32[WidthPx * HeightPx];
            _flipped = new Color32[WidthPx * HeightPx];

            Texture = new Texture2D(WidthPx, heightPx, TextureFormat.RGBA32, false);
            Texture.filterMode = FilterMode.Point;
            Texture.wrapMode = TextureWrapMode.Clamp;

            Frame = new FillFrameRenderer(this);
            SpecialPiece = new SpecialPieceRenderer();
            Snapshot = new SnapshotRenderer(this);
            GroundWarning = new GroundWarningRenderer(this);
        }

        /// <summary>コンテンツ座標(盤面内のxy)を、額縁ぶんのオフセットを含めたBufferの添字に変換する。</summary>
        public int BufferIndex(int contentX, int y) => y * WidthPx + (contentX + FrameThicknessPx);

        /// <summary>左右の額縁を、指定した色で塗りつぶす。</summary>
        public void FillFrame(Color32 color) => Frame.FillFrame(color);

        /// <summary>盤面(グリッド + 操作中ミノ)をBufferに描く。額縁は、警告が出ていない通常の色で塗る。</summary>
        public void DrawBoard(BoardModel model)
        {
            FillFrame(FrameColor);

            var grid = model.Grid;
            for (int y = 0; y < HeightPx; y++)
            {
                for (int cx = 0; cx < ContentWidthPx; cx++)
                {
                    var cell = grid.Cells[grid.Index(cx, y)];
                    Buffer[BufferIndex(cx, y)] = cell.Occupied ? cell.Color : BackgroundColor;
                }
            }

            DrawPiece(model.CurrentPiece);
        }

        public void DrawPiece(FallingPiece piece)
        {
            if (piece == null) return;

            Color32[] specialColors = null;
            if (piece.IsSpecial)
            {
                specialColors = new Color32[piece.Offsets.Count];
                SpecialPiece.ComputeColors(piece, specialColors);
            }

            for (int i = 0; i < piece.Offsets.Count; i++)
            {
                var p = piece.Anchor + piece.Offsets[i];
                if (p.x < 0 || p.x >= ContentWidthPx || p.y < 0 || p.y >= HeightPx) continue;

                Buffer[BufferIndex(p.x, p.y)] = piece.IsSpecial ? specialColors[i] : piece.PixelColors[i];
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

        // --- ここから、呼び出し側の利便性のための窓口。中身は、それぞれの専門クラスへそのまま転送する ---

        public Color32 ConcreteColor { get => Snapshot.ConcreteColor; set => Snapshot.ConcreteColor = value; }
        public Color32 AshColor { get => Snapshot.AshColor; set => Snapshot.AshColor = value; }
        public void DrawFromSnapshot(byte[] snapshot, Color32[] palette) => Snapshot.DrawFromSnapshot(snapshot, palette);

        public Color32 WarningColor { get => GroundWarning.WarningColor; set => GroundWarning.WarningColor = value; }
        public void DrawGroundWarning(int heightPx, float timeSeconds) => GroundWarning.DrawGroundWarning(heightPx, timeSeconds);

        public Color32 SpecialPieceEdgeColor { get => SpecialPiece.EdgeColor; set => SpecialPiece.EdgeColor = value; }
        public float SpecialPieceEdgeMinBrightness { get => SpecialPiece.EdgeMinBrightness; set => SpecialPiece.EdgeMinBrightness = value; }
        public float SpecialPieceEdgeMaxBrightness { get => SpecialPiece.EdgeMaxBrightness; set => SpecialPiece.EdgeMaxBrightness = value; }
        public float SpecialPiecePulseSpeed { get => SpecialPiece.PulseSpeed; set => SpecialPiece.PulseSpeed = value; }
        public float SpecialPieceBrightBias { get => SpecialPiece.BrightBias; set => SpecialPiece.BrightBias = value; }
        public Color32[] SpecialPieceColors { get => SpecialPiece.Colors; set => SpecialPiece.Colors = value; }
        public float SpecialPieceSaturationBoost { get => SpecialPiece.SaturationBoost; set => SpecialPiece.SaturationBoost = value; }
    }
}