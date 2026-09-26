using UnityEngine;

namespace SandTetris
{
    /// <summary>
    /// グリッド上の1マス分のデータ。今の段階では「占有されているか」と「色」だけを持つ。
    /// </summary>
    public struct Cell
    {
        public bool Occupied;
        public Color32 Color;
        public byte ColorIndex; // 見た目の色(明暗ノイズあり)とは別に、同色判定に使う「色グループ」
    }

    /// <summary>
    /// 【ステージ2】砂の土台 + 崩れる物理。
    /// SimulateStep を一定間隔で呼ぶことで、着地した塊がサラサラと崩れていく。
    /// </summary>
    public class SandGrid
    {
        public readonly int Width;
        public readonly int Height;
        public Cell[] Cells;

        /// <summary>
        /// 斜め移動が実際に発生する確率(0〜1)。
        /// 値を小さくするほど、山は横に広がりにくくなり、より尖った形になる。
        /// </summary>
        public float DiagonalMoveChance = 0.35f;

        /// <summary>
        /// 真下への落下が実際に発生する確率(0〜1)。DiagonalMoveChanceより高くしておくのが基本。
        /// 100%(決定的)にすると、宙に浮いた一列がまるごと同じタイミングで動いてしまい、
        /// 「剛体のまま並行にスライドしているだけ」に見えてしまう。
        /// 各粒がバラバラなタイミングで動くことで、初めて自然な「崩れ落ちる」見た目になる。
        /// (Scratch版のMoveプロシージャが、真下方向にも確率を掛けていたのと同じ発想)
        /// </summary>
        public float FallMoveChance = 0.85f;

        /// <summary>
        /// CheckAndClearConnectedLine で見つかった、消去対象のセルのインデックス一覧。
        /// 光らせる演出などに使う。
        /// </summary>
        public System.Collections.Generic.List<int> LastClearedIndices = new System.Collections.Generic.List<int>();

        int _frameParity; // 毎ステップ左右のスキャン方向を反転させ、偏りを防ぐ

        public SandGrid(int width, int height)
        {
            Width = width;
            Height = height;
            Cells = new Cell[width * height];
        }

        public int Index(int x, int y) => y * Width + x;

        public bool InBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

        public bool IsOccupied(int x, int y) => InBounds(x, y) && Cells[Index(x, y)].Occupied;

        public bool IsFree(int x, int y) => InBounds(x, y) && !Cells[Index(x, y)].Occupied;

        /// <summary>
        /// 砂物理を1ステップ進める。
        /// 各セルについて「下が空なら落ちる、ダメなら斜め左右どちらか空いている方へ」を適用する。
        /// 下の行から上の行へスキャンすることで、同一ステップ内で二重に移動させてしまうのを防ぐ。
        /// </summary>
        public void SimulateStep()
        {
            _frameParity ^= 1;

            for (int y = Height - 2; y >= 0; y--)
            {
                bool leftToRight = ((y + _frameParity) & 1) == 0;
                for (int xi = 0; xi < Width; xi++)
                {
                    int x = leftToRight ? xi : Width - 1 - xi;
                    int idx = Index(x, y);
                    if (!Cells[idx].Occupied) continue;

                    if (IsFree(x, y + 1))
                    {
                        // 真下も確率で間引く。100%決定的にすると、宙に浮いた一列が
                        // まるごと同時に動いてしまい「剛体が並行にスライドしているだけ」に見える。
                        if (Random.value < FallMoveChance)
                        {
                            MoveCell(x, y, x, y + 1);
                        }
                        continue;
                    }

                    bool leftFree = IsFree(x - 1, y + 1);
                    bool rightFree = IsFree(x + 1, y + 1);

                    if (!leftFree && !rightFree) continue; // どちらも塞がっていれば静止

                    // 斜め移動は確率で間引く(真下と違って、毎ティック確実には動かさない)
                    if (Random.value >= DiagonalMoveChance) continue;

                    if (leftFree && rightFree)
                    {
                        // 両方空いているときはランダムに崩す(偏った山になるのを防ぐ)
                        if (Random.value < 0.5f) MoveCell(x, y, x - 1, y + 1);
                        else MoveCell(x, y, x + 1, y + 1);
                    }
                    else if (leftFree)
                    {
                        MoveCell(x, y, x - 1, y + 1);
                    }
                    else if (rightFree)
                    {
                        MoveCell(x, y, x + 1, y + 1);
                    }
                }
            }
        }

        /// <summary>
        /// 「同色の連結領域が左端から右端まで到達しているか」をBFSで判定する。
        /// 見つかった最初の1本を LastClearedIndices に記録し、true を返す。
        /// 実際に消すのは ApplyClear を呼ぶまで行わない(先に光らせる演出を挟めるように)。
        /// </summary>
        public bool CheckAndClearConnectedLine()
        {
            var visited = new bool[Cells.Length];
            var queue = new System.Collections.Generic.Queue<Vector2Int>();
            LastClearedIndices.Clear();

            for (int y = 0; y < Height; y++)
            {
                int startIdx = Index(0, y);
                if (!Cells[startIdx].Occupied || visited[startIdx]) continue;

                byte targetColorIndex = Cells[startIdx].ColorIndex;
                var region = new System.Collections.Generic.List<Vector2Int>();
                bool reachedRight = false;

                queue.Clear();
                queue.Enqueue(new Vector2Int(0, y));
                visited[startIdx] = true;

                while (queue.Count > 0)
                {
                    var p = queue.Dequeue();
                    region.Add(p);
                    if (p.x == Width - 1) reachedRight = true;

                    TryEnqueue(p.x + 1, p.y, targetColorIndex, visited, queue);
                    TryEnqueue(p.x - 1, p.y, targetColorIndex, visited, queue);
                    TryEnqueue(p.x, p.y + 1, targetColorIndex, visited, queue);
                    TryEnqueue(p.x, p.y - 1, targetColorIndex, visited, queue);
                }

                if (reachedRight)
                {
                    foreach (var p in region) LastClearedIndices.Add(Index(p.x, p.y));
                    return true;
                }
            }
            return false;
        }

        void TryEnqueue(int x, int y, byte targetColorIndex, bool[] visited, System.Collections.Generic.Queue<Vector2Int> queue)
        {
            if (!InBounds(x, y)) return;
            int idx = Index(x, y);
            if (visited[idx] || !Cells[idx].Occupied) return;
            if (Cells[idx].ColorIndex != targetColorIndex) return;
            visited[idx] = true;
            queue.Enqueue(new Vector2Int(x, y));
        }

        /// <summary>
        /// CheckAndClearConnectedLine が見つけた範囲を実際に消す。
        /// </summary>
        public void ApplyClear()
        {
            foreach (var idx in LastClearedIndices) Cells[idx] = default;
        }

        void MoveCell(int x1, int y1, int x2, int y2)
        {
            int i1 = Index(x1, y1);
            int i2 = Index(x2, y2);
            Cells[i2] = Cells[i1];
            Cells[i1] = default;
        }

        /// <summary>
        /// ミノが着地したとき、そのピクセル群をそのままグリッドに焼き込む。
        /// positions と colors は同じ順序・同じ数である必要がある(粒ごとに個別の色を持たせるため)。
        /// colorIndex はピース全体で共通の「色グループ」で、ライン消去の同色判定に使う
        /// (見た目の色は粒ごとに明暗が違っても、色グループとしては同じものとして繋がる)。
        /// </summary>
        public void Bake(System.Collections.Generic.IReadOnlyList<Vector2Int> positions, System.Collections.Generic.IReadOnlyList<Color32> colors, byte colorIndex)
        {
            for (int i = 0; i < positions.Count; i++)
            {
                var p = positions[i];
                if (!InBounds(p.x, p.y)) continue;
                int idx = Index(p.x, p.y);
                Cells[idx].Occupied = true;
                Cells[idx].Color = colors[i];
                Cells[idx].ColorIndex = colorIndex;
            }
        }

        /// <summary>
        /// 配列全体を、そのまま Color32 の配列として書き出す。
        /// これを Texture2D.SetPixels32 に渡すだけで画面に反映できる。
        /// </summary>
        public void WriteTo(Color32[] buffer, Color32 backgroundColor)
        {
            for (int i = 0; i < Cells.Length; i++)
            {
                buffer[i] = Cells[i].Occupied ? Cells[i].Color : backgroundColor;
            }
        }
    }
}
