using System.Collections.Generic;
using UnityEngine;

namespace SandTetris
{
    /// <summary>
    /// 特別なミノが着地した瞬間の、色の伝染処理を担当する。
    /// </summary>
    public class InfectionSpreader
    {
        readonly Grid _grid;

        public InfectionSpreader(Grid grid)
        {
            _grid = grid;
        }

        /// <summary>
        /// originCells(そのミノ自身が今まさに焼き込まれたマス)の8方向のどこかに触れている
        /// 色付きの砂があれば、その砂と同じ色で繋がっている連結領域(ライン消去の判定と同じ、
        /// 8方向のBFS)を丸ごと newColor/newColorIndex に塗り替える。
        /// 複数の異なる色に同時に触れていた場合は、それぞれの色の領域が別々に塗り替えられる。
        ///
        /// 戻り値: 実際に色が変わったセルのインデックス一覧。
        /// oldColors: 戻り値と同じ順番で対応する、変化前の色の一覧(演出側が「元の色」を知るために使う)。
        /// </summary>
        public List<int> InfectConnectedRegions(
            IReadOnlyList<Vector2Int> originCells,
            Color32 newColor,
            byte newColorIndex,
            out List<Color32> oldColors)
        {
            var cells = _grid.Cells;
            var changedIndices = new List<int>();
            var changedOldColors = new List<Color32>();

            var visited = new bool[cells.Length];
            var queue = new Queue<Vector2Int>();

            // 特別なミノ自身が占めたマスは、起点探しの対象にも、BFSの対象にもしない
            foreach (var o in originCells)
            {
                if (_grid.InBounds(o.x, o.y)) visited[_grid.Index(o.x, o.y)] = true;
            }

            int[] dxs = { 1, -1, 0, 0, 1, 1, -1, -1 };
            int[] dys = { 0, 0, 1, -1, 1, -1, 1, -1 };

            foreach (var o in originCells)
            {
                for (int i = 0; i < dxs.Length; i++)
                {
                    int nx = o.x + dxs[i];
                    int ny = o.y + dys[i];
                    if (!_grid.InBounds(nx, ny)) continue;

                    int nidx = _grid.Index(nx, ny);
                    if (visited[nidx]) continue;
                    if (!cells[nidx].Occupied || cells[nidx].IsConcrete || cells[nidx].IsAsh) continue;

                    // ここが、まだ見つけていない新しい色の連結領域の起点
                    byte targetColorIndex = cells[nidx].ColorIndex;
                    var region = new List<int>();

                    queue.Clear();
                    queue.Enqueue(new Vector2Int(nx, ny));
                    visited[nidx] = true;

                    while (queue.Count > 0)
                    {
                        var p = queue.Dequeue();
                        region.Add(_grid.Index(p.x, p.y));

                        TryEnqueue(p.x + 1, p.y, targetColorIndex, visited, queue);
                        TryEnqueue(p.x - 1, p.y, targetColorIndex, visited, queue);
                        TryEnqueue(p.x, p.y + 1, targetColorIndex, visited, queue);
                        TryEnqueue(p.x, p.y - 1, targetColorIndex, visited, queue);
                        TryEnqueue(p.x + 1, p.y + 1, targetColorIndex, visited, queue);
                        TryEnqueue(p.x + 1, p.y - 1, targetColorIndex, visited, queue);
                        TryEnqueue(p.x - 1, p.y + 1, targetColorIndex, visited, queue);
                        TryEnqueue(p.x - 1, p.y - 1, targetColorIndex, visited, queue);
                    }

                    foreach (var idx in region)
                    {
                        changedIndices.Add(idx);
                        changedOldColors.Add(cells[idx].Color); // 塗り替える前に、元の色を記録しておく

                        cells[idx].Color = newColor;
                        cells[idx].ColorIndex = newColorIndex;
                    }
                }
            }

            oldColors = changedOldColors;
            return changedIndices;
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
    }
}