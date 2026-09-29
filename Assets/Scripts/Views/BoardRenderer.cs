using System.Collections.Generic;
using UnityEngine;

namespace SandTetris
{
    /// <summary>
    /// BoardModelの状態をTexture2Dに描画するだけのクラス。ゲームロジックは一切持たない。
    /// 自分の盤面・相手の盤面のどちらにも同じインスタンスの作り方で使い回せる。
    ///
    /// 絵の左右には、常設の「額縁」を含めて描く(FrameThicknessPx 分)。
    /// 盤面そのもの(ContentWidthPx)の大きさは変えないので、ミノや砂の見た目・当たり判定に
    /// 影響しない。絵全体(WidthPx)は額縁の分だけ広くなるので、これを表示する側の
    /// RawImage の幅も、同じ比率だけ広げる必要がある
    /// (新しいRawImageの幅 = 今のRawImageの幅 × WidthPx ÷ ContentWidthPx)。
    ///
    /// せめぎ合いの警告は、この額縁を WarningColor で塗り替えることで表現する
    /// (盤面の外側が赤くなる)。あわせて、盤面の内側の上端にも境界線を1本描く。
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

        /// <summary>警告表示中の色(額縁・上端の境界線・流れる縞、共通で使う)。</summary>
        public Color32 WarningColor = new Color32(220, 40, 40, 255);

        /// <summary>相手の盤面表示で、コンクリート(スナップショット値5)を描くときの色。</summary>
        public Color32 ConcreteColor = new Color32(90, 90, 95, 255);
        public const byte ConcreteSnapshotValue = 5;

        /// <summary>相手の盤面表示で、灰(スナップショット値6)を描くときの色。</summary>
        public Color32 AshColor = new Color32(150, 150, 155, 255);
        public const byte AshSnapshotValue = 6;

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
        }

        /// <summary>コンテンツ座標(盤面内のxy)を、額縁ぶんのオフセットを含めたBufferの添字に変換する。</summary>
        public int BufferIndex(int contentX, int y) => y * WidthPx + (contentX + FrameThicknessPx);

        /// <summary>左右の額縁を、指定した色で塗りつぶす。</summary>
        void FillFrame(Color32 color)
        {
            if (FrameThicknessPx <= 0) return;

            for (int y = 0; y < HeightPx; y++)
            {
                int rowStart = y * WidthPx;
                for (int fx = 0; fx < FrameThicknessPx; fx++)
                {
                    Buffer[rowStart + fx] = color;
                    Buffer[rowStart + WidthPx - 1 - fx] = color;
                }
            }
        }

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
                if (p.x >= 0 && p.x < ContentWidthPx && p.y >= 0 && p.y < HeightPx)
                {
                    Buffer[BufferIndex(p.x, p.y)] = piece.PixelColors[i];
                }
            }
        }

        /// <summary>
        /// 指定インデックス(グリッドの Index(x,y) = y*ContentWidthPx+x で計算されたもの)を
        /// 単色で塗りつぶす(ライン消去のハイライト用)。
        /// </summary>
        public void DrawHighlight(IReadOnlyList<int> indices, Color32 color)
        {
            foreach (var idx in indices)
            {
                int cx = idx % ContentWidthPx;
                int y = idx / ContentWidthPx;
                Buffer[BufferIndex(cx, y)] = color;
            }
        }

        /// <summary>
        /// ネットワーク経由で受け取ったスナップショット(BoardSnapshotCodec.Encode の出力、
        /// ContentWidthPx×HeightPx分の長さ)から直接描画する。相手の盤面表示用。
        /// 0=背景、1〜4=paletteの色、5=コンクリート、6=灰。ベタ塗り。額縁は通常の色で塗る。
        /// </summary>
        public void DrawFromSnapshot(byte[] snapshot, Color32[] palette)
        {
            FillFrame(FrameColor);

            int count = Mathf.Min(ContentWidthPx * HeightPx, snapshot.Length);
            for (int i = 0; i < count; i++)
            {
                int cx = i % ContentWidthPx;
                int y = i / ContentWidthPx;

                byte v = snapshot[i];
                Color32 c;
                if (v == 0) c = BackgroundColor;
                else if (v == ConcreteSnapshotValue) c = ConcreteColor;
                else if (v == AshSnapshotValue) c = AshColor;
                else c = palette[(v - 1) % palette.Length];

                Buffer[BufferIndex(cx, y)] = c;
            }
        }

        /// <summary>
        /// BoardSnapshotCodec.EncodeCoarse の出力(マス単位のデータ)を、
        /// 1マスあたり blockSize×blockSize ピクセルに拡大して描画する。額縁は通常の色で塗る。
        /// </summary>
        public void DrawFromBlockSnapshot(byte[] snapshot, int blockSize, int widthInBlocks, int heightInBlocks, Color32[] palette)
        {
            FillFrame(FrameColor);

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
                            int cx = startX + dx;
                            int y = startY + dy;
                            if (cx < ContentWidthPx && y < HeightPx)
                            {
                                Buffer[BufferIndex(cx, y)] = color;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// せめぎ合いで攻撃が成立しそうなとき、「ここまで上がる予定」を警告表示する。
        /// - 左右の額縁(盤面の外側に最初から確保してある帯)を、高さぶんだけ WarningColor に塗り替える
        /// - 盤面の内側には、上端の境界線(1本、額縁と同じ太さ)と、その下に流れる斜め縞を描く
        /// heightPx: 盤面の底からの高さ(ピクセル)。0以下なら何もしない(額縁は通常の色のまま)。
        /// timeSeconds: 縞を流すための経過時間(秒)。呼び出し側が Time.time などを渡す想定。
        /// </summary>
        public void DrawGroundWarning(int heightPx, float timeSeconds)
        {
            if (heightPx <= 0) return;
            if (heightPx > HeightPx) heightPx = HeightPx;

            int top = HeightPx - heightPx; // この行(y >= top)から下が警告範囲
            const int stripeSpacing = 8;    // 縞の間隔(ピクセル)
            const float stripeSpeed = 24f;  // 縞が流れる速さ(1秒あたり何ピクセル分)
            const float stripeAlpha = 0.5f; // 縞の不透明度

            int scroll = Mathf.FloorToInt(timeSeconds * stripeSpeed);

            // 左右の額縁を、警告範囲の高さぶんだけ塗り替える
            for (int y = top; y < HeightPx; y++)
            {
                int rowStart = y * WidthPx;
                for (int fx = 0; fx < FrameThicknessPx; fx++)
                {
                    Buffer[rowStart + fx] = WarningColor;
                    Buffer[rowStart + WidthPx - 1 - fx] = WarningColor;
                }
            }

            // 上端の境界線(盤面の内側。額縁と同じ太さにして、額縁と一体に見せる)
            // 上端の境界線は、額縁(左右)の半分の太さにする
            int lineThickness = Mathf.Max(1, FrameThicknessPx / 2);
            for (int b = 0; b < lineThickness && top + b < HeightPx; b++)
            {
                int y = top + b;
                for (int cx = 0; cx < ContentWidthPx; cx++)
                {
                    Buffer[BufferIndex(cx, y)] = WarningColor;
                }
            }

            // 境界線の下、警告範囲の内部を、流れる斜め縞で軽く色づける
            for (int y = top + lineThickness; y < HeightPx; y++)
            {
                for (int cx = 0; cx < ContentWidthPx; cx++)
                {
                    int stripePhase = ((cx + y - scroll) % stripeSpacing + stripeSpacing) % stripeSpacing;
                    if (stripePhase < stripeSpacing / 2)
                    {
                        int idx = BufferIndex(cx, y);
                        Buffer[idx] = BlendWarning(Buffer[idx], stripeAlpha);
                    }
                }
            }
        }

        Color32 BlendWarning(Color32 baseColor, float alpha)
        {
            byte r = (byte)Mathf.RoundToInt(Mathf.Lerp(baseColor.r, WarningColor.r, alpha));
            byte g = (byte)Mathf.RoundToInt(Mathf.Lerp(baseColor.g, WarningColor.g, alpha));
            byte b = (byte)Mathf.RoundToInt(Mathf.Lerp(baseColor.b, WarningColor.b, alpha));
            return new Color32(r, g, b, 255);
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