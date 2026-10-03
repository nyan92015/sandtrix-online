using System.Collections.Generic;
using UnityEngine;

namespace SandTetris
{
    /// <summary>
    /// 特別なミノの、落下中だけの見た目(縁取りが全体で揃って呼吸する明滅、色の彩度調整)を計算する。
    /// 実際にBufferへ書き込むのはBoardRenderer側の役目で、このクラスは色の計算だけを行う。
    /// </summary>
    public class SpecialPieceRenderer
    {
        public Color32 EdgeColor = new Color32(255, 255, 255, 255);
        public float EdgeMinBrightness = 0.3f; // 縁の明滅の、一番暗いときの強さ(0〜1)
        public float EdgeMaxBrightness = 1.0f; // 縁の明滅の、一番明るいときの強さ(0〜1)
        public float PulseSpeed = 3f;          // 明滅の速さ(大きいほど速く点滅する)
        public float BrightBias = 0.6f;        // 1より小さいほど、明るい側に長く留まるようになる(1なら左右対称)

        /// <summary>
        /// 特別なミノを、落下中にどの色で表示するか(色ごと、4色ぶん)。
        /// インデックスは piece.ColorIndex と対応(0=赤、1=青、2=黄、3=緑)。
        /// 着地してグリッドに焼き込まれる色(piece.PixelColors)には一切影響しない、描画の瞬間だけの見た目。
        /// </summary>
        public Color32[] Colors =
        {
            new Color32(150, 30, 30, 255),  // 赤(濃い)
            new Color32(30, 80, 150, 255),  // 青(濃い)
            new Color32(150, 120, 20, 255), // 黄(濃い)
            new Color32(30, 120, 60, 255),  // 緑(濃い)
        };

        /// <summary>
        /// Colors の4色全部に対して、まとめてどれだけ彩度を上げるか(0以上)。
        /// グレー(無彩色)からその色本来の方向へ、さらに突き放すことで、
        /// 明るさはあまり変えずに「色そのものを濃く・鮮やかに」する。0ならColorsをそのまま使う。
        /// </summary>
        public float SaturationBoost = 0f;

        /// <summary>
        /// piece の各ピクセル(piece.Offsets と同じ順序・同じ数)について、表示色を計算して colorsOut に書き込む。
        /// colorsOut は、呼び出し側が piece.Offsets.Count 以上の長さで用意しておくこと。
        /// </summary>
        public void ComputeColors(FallingPiece piece, Color32[] colorsOut)
        {
            var offsetSet = new HashSet<Vector2Int>(piece.Offsets);

            // 0〜1で、全体が同じタイミングで揃って呼吸するように揺れる(粒ごとの位相ズレはなし)
            float wave = (Mathf.Sin(Time.time * PulseSpeed) + 1f) * 0.5f;
            wave = Mathf.Pow(wave, BrightBias); // 1より小さい指数ほど、値が1(明るい)側に寄る時間が長くなる
            float edgeBrightness = Mathf.Lerp(EdgeMinBrightness, EdgeMaxBrightness, wave);

            for (int i = 0; i < piece.Offsets.Count; i++)
            {
                var o = piece.Offsets[i];

                Color32 color = Colors[piece.ColorIndex % Colors.Length];
                if (SaturationBoost > 0f)
                {
                    color = BoostSaturation(color, SaturationBoost);
                }

                bool isEdge = !offsetSet.Contains(o + new Vector2Int(1, 0))
                    || !offsetSet.Contains(o + new Vector2Int(-1, 0))
                    || !offsetSet.Contains(o + new Vector2Int(0, 1))
                    || !offsetSet.Contains(o + new Vector2Int(0, -1));
                if (isEdge)
                {
                    color = Color32.Lerp(color, EdgeColor, edgeBrightness);
                }

                colorsOut[i] = color;
            }
        }

        /// <summary>
        /// 色の彩度を上げる。グレー(R・G・Bの平均)を基準点として、そこから元の色への方向を
        /// (1+amount)倍に引き伸ばす。明るさの中心はあまり動かさず、色の鮮やかさだけを強める。
        /// </summary>
        static Color32 BoostSaturation(Color32 color, float amount)
        {
            float gray = (color.r + color.g + color.b) / 3f;
            float scale = 1f + amount;

            float r = gray + (color.r - gray) * scale;
            float g = gray + (color.g - gray) * scale;
            float b = gray + (color.b - gray) * scale;

            return new Color32(
                (byte)Mathf.Clamp(r, 0, 255),
                (byte)Mathf.Clamp(g, 0, 255),
                (byte)Mathf.Clamp(b, 0, 255),
                255);
        }
    }
}