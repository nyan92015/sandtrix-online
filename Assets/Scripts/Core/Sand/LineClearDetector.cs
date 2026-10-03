using System.Collections.Generic;
using UnityEngine;

namespace SandTetris
{
    /// <summary>
    /// 「同色の連結領域が左端から右端まで到達しているか」を判定し、見つかった範囲を消す。
    /// </summary>
    public class LineClearDetector
    {
        readonly Grid _grid;

        /// <summary>
        /// CheckAndClearConnectedLine で見つかった、消去対象のセルのインデックス一覧。
        /// 光らせる演出などに使う。
        /// </summary>
        public List<int> LastClearedIndices { get; } = new List<int>();

        public LineClearDetector(Grid grid)
        {
            _grid = grid;
        }

        /// <summary>
        /// 連結は上下左右に加えて斜めも含む8方向。
        /// 砂山の縁は階段状に斜めで接することが多く、4方向だと斜めにしか接していない粒が
        /// 「繋がっていない」とみなされて、消去のあとに孤立した残骸として残ってしまうため。
        /// 検出と消去は同じ領域を使うので、この判定が両方に効く。
        /// 見つかった最初の1本を LastClearedIndices に記録し、true を返す。
        /// 実際に消すのは ApplyClear を呼ぶまで行わない(先に光らせる演出を挟めるように)。
        /// </summary>
        public bool CheckAndClearConnectedLine()
        {
            var cells = _grid.Cells;
            int width = _grid.Width;
            int height = _grid.Height;

            var visited = new bool[cells.Length];
            var queue = new Queue<Vector2Int>();
            LastClearedIndices.Clear();

            for (int y = 0; y < height; y++)
            {
                int startIdx = _grid.Index(0, y);
                if (!cells[startIdx].Occupied || cells[startIdx].IsConcrete || cells[startIdx].IsAsh || visited[startIdx]) continue;

                byte targetColorIndex = cells[startIdx].ColorIndex;
                var region = new List<Vector2Int>();
                bool reachedRight = false;

                queue.Clear();
                queue.Enqueue(new Vector2Int(0, y));
                visited[startIdx] = true;

                while (queue.Count > 0)
                {
                    var p = queue.Dequeue();
                    region.Add(p);
                    if (p.x == width - 1) reachedRight = true;

                    // 上下左右
                    TryEnqueue(p.x + 1, p.y, targetColorIndex, visited, queue);
                    TryEnqueue(p.x - 1, p.y, targetColorIndex, visited, queue);
                    TryEnqueue(p.x, p.y + 1, targetColorIndex, visited, queue);
                    TryEnqueue(p.x, p.y - 1, targetColorIndex, visited, queue);
                    // 斜め
                    TryEnqueue(p.x + 1, p.y + 1, targetColorIndex, visited, queue);
                    TryEnqueue(p.x + 1, p.y - 1, targetColorIndex, visited, queue);
                    TryEnqueue(p.x - 1, p.y + 1, targetColorIndex, visited, queue);
                    TryEnqueue(p.x - 1, p.y - 1, targetColorIndex, visited, queue);
                }

                if (reachedRight)
                {
                    foreach (var p in region) LastClearedIndices.Add(_grid.Index(p.x, p.y));
                    return true;
                }
            }
            return false;
        }

        void TryEnqueue(int x, int y, byte targetColorIndex, bool[] visited, Queue<Vector2Int> queue)
        {
            if (!_grid.InBounds(x, y)) return;
            int idx = _grid.Index(x, y);
            var cells = _grid.Cells;
            if (visited[idx] || !cells[idx].Occupied || cells[idx].IsConcrete || cells[idx].IsAsh) return;
            if (cells[idx].ColorIndex != targetColorIndex) return;
            visited[idx] = true;
            queue.Enqueue(new Vector2Int(x, y));
        }

        /// <summary>
        /// CheckAndClearConnectedLine が見つけた範囲を実際に消す。
        /// </summary>
        public void ApplyClear()
        {
            foreach (var idx in LastClearedIndices) _grid.Cells[idx] = default;
        }
    }
}