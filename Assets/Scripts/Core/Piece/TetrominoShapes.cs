using UnityEngine;

namespace SandTetris
{
    public static class TetrominoShapes
    {
        // 標準テトリスと同じ「ブロック単位」の相対座標(4ブロック)
        public static readonly Vector2Int[][] Shapes =
        {
            new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0) }, // I
            new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) }, // O
            new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(1, 1) }, // T
            new[] { new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) }, // S
            new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(2, 1) }, // Z
            new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(1, 2) }, // J(縦向き・逆)
            new[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(1, 0) }, // L(縦向き・逆)
        };

        public static readonly Color32[] Colors =
        {
            new Color32(230, 70, 70, 255),   // 赤
            new Color32(70, 150, 230, 255),  // 青
            new Color32(230, 200, 60, 255),  // 黄
            new Color32(80, 200, 120, 255),  // 緑
        };
    }
}