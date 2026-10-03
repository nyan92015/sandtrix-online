using UnityEngine;

namespace SandTetris
{
    /// <summary>
    /// せめぎ合いで攻撃が成立しそうなときの、警告表示(額縁・境界線・流れる縞)を担当する。
    /// </summary>
    public class GroundWarningRenderer
    {
        readonly BoardRenderer _renderer;

        /// <summary>警告表示中の色(額縁・上端の境界線・流れる縞、共通で使う)。</summary>
        public Color32 WarningColor = new Color32(220, 40, 40, 255);

        public GroundWarningRenderer(BoardRenderer renderer)
        {
            _renderer = renderer;
        }

        /// <summary>
        /// 「ここまで上がる予定」を警告表示する。
        /// - 左右の額縁(盤面の外側に最初から確保してある帯)を、高さぶんだけ WarningColor に塗り替える
        /// - 盤面の内側には、上端の境界線(1本、額縁と同じ太さ)と、その下に流れる斜め縞を描く
        /// heightPx: 盤面の底からの高さ(ピクセル)。0以下なら何もしない(額縁は通常の色のまま)。
        /// timeSeconds: 縞を流すための経過時間(秒)。呼び出し側が Time.time などを渡す想定。
        /// </summary>
        public void DrawGroundWarning(int heightPx, float timeSeconds)
        {
            int widthPx = _renderer.WidthPx;
            int heightPxTotal = _renderer.HeightPx;
            int frameThicknessPx = _renderer.FrameThicknessPx;
            int contentWidthPx = _renderer.ContentWidthPx;
            var buffer = _renderer.Buffer;

            if (heightPx <= 0) return;
            if (heightPx > heightPxTotal) heightPx = heightPxTotal;

            int top = heightPxTotal - heightPx; // この行(y >= top)から下が警告範囲
            const int stripeSpacing = 8;    // 縞の間隔(ピクセル)
            const float stripeSpeed = 24f;  // 縞が流れる速さ(1秒あたり何ピクセル分)
            const float stripeAlpha = 0.5f; // 縞の不透明度

            int scroll = Mathf.FloorToInt(timeSeconds * stripeSpeed);

            // 左右の額縁を、警告範囲の高さぶんだけ塗り替える
            for (int y = top; y < heightPxTotal; y++)
            {
                int rowStart = y * widthPx;
                for (int fx = 0; fx < frameThicknessPx; fx++)
                {
                    buffer[rowStart + fx] = WarningColor;
                    buffer[rowStart + widthPx - 1 - fx] = WarningColor;
                }
            }

            // 上端の境界線(盤面の内側。額縁の半分の太さにして、額縁と一体に見せる)
            int lineThickness = Mathf.Max(1, frameThicknessPx / 2);
            for (int b = 0; b < lineThickness && top + b < heightPxTotal; b++)
            {
                int y = top + b;
                for (int cx = 0; cx < contentWidthPx; cx++)
                {
                    buffer[_renderer.BufferIndex(cx, y)] = WarningColor;
                }
            }

            // 境界線の下、警告範囲の内部を、流れる斜め縞で軽く色づける
            for (int y = top + lineThickness; y < heightPxTotal; y++)
            {
                for (int cx = 0; cx < contentWidthPx; cx++)
                {
                    int stripePhase = ((cx + y - scroll) % stripeSpacing + stripeSpacing) % stripeSpacing;
                    if (stripePhase < stripeSpacing / 2)
                    {
                        int idx = _renderer.BufferIndex(cx, y);
                        buffer[idx] = BlendWarning(buffer[idx], stripeAlpha);
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
    }
}