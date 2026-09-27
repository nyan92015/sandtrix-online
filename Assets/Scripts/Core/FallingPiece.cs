using System.Collections.Generic;
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

    /// <summary>
    /// 「決まった構成の袋をシャッフルして、端から1個ずつ配る」方式のランダマイザー。
    /// 袋が空になったら、また同じ構成で作り直してシャッフルする。
    /// これにより「長い目で見ると必ず決まった比率で出る」かつ「短期的にはランダムに見える」を両立できる。
    /// </summary>
    public class ShuffleBag
    {
        readonly List<int> _template; // 1回分の袋の中身(例: 形なら7種、色なら4種×3個の12個)
        readonly List<int> _bag = new List<int>();

        public ShuffleBag(List<int> template)
        {
            _template = template;
        }

        public int Next()
        {
            if (_bag.Count == 0) Refill();
            int lastIndex = _bag.Count - 1;
            int value = _bag[lastIndex];
            _bag.RemoveAt(lastIndex);
            return value;
        }

        void Refill()
        {
            _bag.AddRange(_template);
            // Fisher-Yatesシャッフル
            for (int i = _bag.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (_bag[i], _bag[j]) = (_bag[j], _bag[i]);
            }
        }
    }

    /// <summary>
    /// 【ステージ1】操作中のミノ。
    /// ブロック単位の形を blockSize×blockSize のピクセル群に展開し、
    /// 重心をピボットとして相対座標(Offsets)に変換して持つ。
    /// 今の段階ではまだ「砂として崩れる」処理はなく、着地したらそのまま塊として固定される。
    /// </summary>
    public class FallingPiece
    {
        public Vector2Int Anchor;         // グリッド上の基準座標(ピボット)
        public List<Vector2Int> Offsets;  // Anchor からの相対オフセット(ピクセル単位)
        public List<Color32> PixelColors; // Offsets と対応する、粒ごとの色(単色。全ピクセル同じ色)
        public Color32 Color;             // プレビューのフチ表示や、色の代表値として使う基準色
        public byte ColorIndex;           // 色グループ(ライン消去の同色判定に使う)
        public int ShapeIndex;            // どの形か(O型かどうかの判定などに使う)
        public int RotationSteps;         // 生成時から何回90度回転したか(0〜3)。ネットワーク同期用

        // TetrominoShapes.Shapes の中での O型(2x2の正方形)のインデックス
        public const int OShapeIndex = 1;

        public bool IsSquare => ShapeIndex == OShapeIndex;

        // 形バッグ: 7種類を1個ずつ、シャッフルして配る(いわゆる7-bag方式)
        static readonly ShuffleBag ShapeBag = new ShuffleBag(BuildShapeTemplate());
        // 色バッグ: 4色を3個ずつ(計12個)、シャッフルして配る
        static readonly ShuffleBag ColorBag = new ShuffleBag(BuildColorTemplate());

        static List<int> BuildShapeTemplate()
        {
            var list = new List<int>();
            for (int i = 0; i < TetrominoShapes.Shapes.Length; i++) list.Add(i);
            return list;
        }

        static List<int> BuildColorTemplate()
        {
            const int countPerColor = 3;
            var list = new List<int>();
            for (int c = 0; c < TetrominoShapes.Colors.Length; c++)
            {
                for (int n = 0; n < countPerColor; n++) list.Add(c);
            }
            return list;
        }

        public static FallingPiece CreateRandom(int blockSize, Vector2Int spawnAnchor)
        {
            int shapeIndex = ShapeBag.Next();
            int colorIndex = ColorBag.Next();
            return CreateFromShape(shapeIndex, colorIndex, blockSize, spawnAnchor);
        }

        /// <summary>
        /// 形・色・位置を指定してミノを生成する(ランダム要素なし)。
        /// ネットワーク越しに「形index・色index・回転数・位置」という軽いデータだけを受け取り、
        /// 受信側で同じ見た目のミノを再現するために使う。
        /// </summary>
        public static FallingPiece CreateFromShape(int shapeIndex, int colorIndex, int blockSize, Vector2Int spawnAnchor)
        {
            var piece = new FallingPiece();
            piece.ShapeIndex = shapeIndex;
            var blocks = TetrominoShapes.Shapes[shapeIndex];

            var pixels = new List<Vector2Int>(blocks.Length * blockSize * blockSize);
            long sumX = 0, sumY = 0;

            foreach (var b in blocks)
            {
                for (int dx = 0; dx < blockSize; dx++)
                {
                    for (int dy = 0; dy < blockSize; dy++)
                    {
                        int px = b.x * blockSize + dx;
                        int py = b.y * blockSize + dy;
                        pixels.Add(new Vector2Int(px, py));
                        sumX += px;
                        sumY += py;
                    }
                }
            }

            var pivot = new Vector2Int((int)(sumX / pixels.Count), (int)(sumY / pixels.Count));

            var baseColor = TetrominoShapes.Colors[colorIndex];

            piece.Offsets = new List<Vector2Int>(pixels.Count);
            piece.PixelColors = new List<Color32>(pixels.Count);

            foreach (var p in pixels)
            {
                piece.Offsets.Add(p - pivot);
                piece.PixelColors.Add(baseColor);
            }

            piece.Anchor = spawnAnchor;
            piece.Color = baseColor;
            piece.ColorIndex = (byte)colorIndex;
            piece.RotationSteps = 0;
            return piece;
        }

        /// <summary>
        /// CreateFromShape で基本形を作った直後に、指定回数だけ回転を適用する。
        /// ネットワーク越しに届いた RotationSteps を再現するために使う。
        /// </summary>
        public static FallingPiece CreateFromShapeWithRotation(int shapeIndex, int colorIndex, int blockSize, Vector2Int spawnAnchor, int rotationSteps)
        {
            var piece = CreateFromShape(shapeIndex, colorIndex, blockSize, spawnAnchor);
            for (int i = 0; i < rotationSteps; i++)
            {
                piece.Offsets = piece.GetRotatedOffsets(1);
            }
            piece.RotationSteps = ((rotationSteps % 4) + 4) % 4;
            return piece;
        }

        public IEnumerable<Vector2Int> WorldPositions()
        {
            foreach (var o in Offsets) yield return Anchor + o;
        }

        /// <summary>
        /// dir = 1: 反時計回り, -1: 時計回り。実際に適用するかは呼び出し側が衝突判定してから決める。
        /// 回転してもピクセルの対応関係(Offsets と PixelColors のインデックス)はそのまま保たれる。
        /// </summary>
        public List<Vector2Int> GetRotatedOffsets(int dir)
        {
            var result = new List<Vector2Int>(Offsets.Count);
            foreach (var o in Offsets)
            {
                result.Add(dir > 0 ? new Vector2Int(-o.y, o.x) : new Vector2Int(o.y, -o.x));
            }
            return result;
        }
    }
}