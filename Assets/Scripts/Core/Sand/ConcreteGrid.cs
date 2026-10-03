using UnityEngine;

namespace SandTetris
{
    /// <summary>
    /// シーソー式の地面(コンクリート)の高さ管理と、崩れて灰になる処理を担当する。
    /// </summary>
    public class ConcreteGrid
    {
        readonly Grid _grid;

        /// <summary>現在の地面(コンクリート)の高さ(ピクセル)。</summary>
        public int GroundHeightPx { get; private set; }

        /// <summary>
        /// 灰の色。せり上がっている間の地面の色と同じものを使う(ConvertGroundToAsh呼び出し側が、
        /// 同じ値をGroundColorにもAshColorにも渡すことで、2つの色を統一している)。
        /// </summary>
        public Color32 AshColor = new Color32(150, 150, 155, 255);

        public ConcreteGrid(Grid grid)
        {
            _grid = grid;
        }

        /// <summary>
        /// 地面(コンクリート)の高さを指定のピクセル数に設定する。
        /// 増える方向のとき、その分だけ既存の中身(砂)を丸ごと上にずらしてから底をコンクリートにする
        /// (呑み込んで消すのではなく、底上げされて持ち上がるイメージ)。
        /// 一番上からはみ出た分は行き場がなく消える(盤面の外に押し出された扱い)。
        /// 減る方向のときは、空いた場所は何もしない(既存の重力処理で自然に砂が落ちてくる)。
        /// </summary>
        public void SetGroundHeightPx(int heightPx, Color32 groundColor)
        {
            int width = _grid.Width;
            int height = _grid.Height;
            var cells = _grid.Cells;

            if (heightPx < 0) heightPx = 0;
            if (heightPx > height) heightPx = height;
            if (heightPx == GroundHeightPx) return;

            int delta = heightPx - GroundHeightPx;
            if (delta > 0)
            {
                ShiftContentUp(delta);
            }

            int thresholdY = height - heightPx; // この行(y >= thresholdY)以降がコンクリート
            for (int y = 0; y < height; y++)
            {
                bool shouldBeConcrete = y >= thresholdY;
                for (int x = 0; x < width; x++)
                {
                    int idx = _grid.Index(x, y);
                    if (shouldBeConcrete)
                    {
                        if (!cells[idx].IsConcrete)
                        {
                            cells[idx] = new Cell { Occupied = true, IsConcrete = true, Color = groundColor };
                        }
                    }
                    else if (cells[idx].IsConcrete)
                    {
                        cells[idx] = default;
                    }
                }
            }
            GroundHeightPx = heightPx;
        }

        /// <summary>
        /// グリッド全体の中身を、指定ピクセル数だけ上にずらす。
        /// 一番上からはみ出た分(行き場のない部分)は消える。
        /// </summary>
        void ShiftContentUp(int delta)
        {
            int width = _grid.Width;
            int height = _grid.Height;
            var cells = _grid.Cells;

            for (int y = 0; y < height; y++)
            {
                int srcY = y + delta;
                for (int x = 0; x < width; x++)
                {
                    int dstIdx = _grid.Index(x, y);
                    cells[dstIdx] = srcY < height ? cells[_grid.Index(x, srcY)] : default;
                }
            }
        }

        /// <summary>
        /// 地面(コンクリート)の全体を、灰に変える。地面は無くなり、高さは0に戻る。
        /// 灰は、以降は砂と同じ物理で落ちていき、盤面の底に触れると消える(PhysicsSimulator を参照)。
        /// </summary>
        public void ConvertGroundToAsh()
        {
            var cells = _grid.Cells;
            for (int i = 0; i < cells.Length; i++)
            {
                if (!cells[i].IsConcrete) continue;
                cells[i] = new Cell { Occupied = true, IsAsh = true, Color = AshColor };
            }
            GroundHeightPx = 0;
        }
    }
}