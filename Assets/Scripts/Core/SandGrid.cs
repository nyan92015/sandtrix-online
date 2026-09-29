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
        public byte ColorIndex; // 色グループ(ライン消去の同色判定に使う)
        public bool IsConcrete; // シーソー式の地面。壊れない・動かない・ライン消去に参加しない
        public bool IsAsh;      // 地面が崩れた灰。砂と同じ物理で落ちるが、盤面の底に触れると消える。ライン消去に参加しない
        public float AshAge;    // 灰になってからの経過時間(秒)。IsAshがtrueのときだけ意味を持つ
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
        /// 灰が1マス落ちる間隔(秒)。砂の重力(呼び出し側の間隔)とは無関係の、固定値。
        /// ライン数による加速(SpeedMultiplier)の影響を受けない
        /// (ゲームを通してずっとこの速さのまま。初期値だけ砂の重力とは別に設定する)。
        /// </summary>
        public float AshGravityInterval = 0.15f;

        /// <summary>
        /// 灰が生まれてから、消えるまでの時間(秒)。
        /// 砂の下に埋もれて盤面の底まで届かないまま止まってしまった灰も、
        /// この時間が経てば強制的に消える(取り残し対策)。
        /// </summary>
        public float AshLifetimeSeconds = 6f;

        float _ashAccumulator;

        /// <summary>
        /// 砂物理を1ステップ進める。
        /// 各セルについて「下が空なら落ちる、ダメなら斜め左右どちらか空いている方へ」を適用する。
        /// 下の行から上の行へスキャンすることで、同一ステップ内で二重に移動させてしまうのを防ぐ。
        ///
        /// deltaTime: この1回の呼び出しが表す実時間(秒)。呼び出し側(BoardModel経由でPlayingState)が
        /// 「砂の重力間隔ぶんの時間が経過するたびに1回呼ぶ」という使い方をしているので、
        /// その間隔をそのまま渡してもらう。灰は、この値を独自に積み立てて AshGravityInterval ごとに
        /// 動かすことで、砂とは別の(固定の)速さで落ちるようにする。
        /// </summary>
        public void SimulateStep(float deltaTime)
        {
            _frameParity ^= 1;

            _ashAccumulator += deltaTime;
            bool allowAshMove = false;
            if (_ashAccumulator >= AshGravityInterval)
            {
                _ashAccumulator -= AshGravityInterval;
                allowAshMove = true;
            }

            for (int y = Height - 2; y >= 0; y--)
            {
                bool leftToRight = ((y + _frameParity) & 1) == 0;
                for (int xi = 0; xi < Width; xi++)
                {
                    int x = leftToRight ? xi : Width - 1 - xi;
                    int idx = Index(x, y);
                    if (!Cells[idx].Occupied || Cells[idx].IsConcrete) continue; // コンクリートは動かない

                    if (Cells[idx].IsAsh)
                    {
                        // 砂の下に埋もれて動けないままでも、時間切れなら強制的に消える(取り残し対策)。
                        // 動けるかどうかに関わらず毎ステップ年を取らせるので、埋もれて止まっていても消える。
                        Cells[idx].AshAge += deltaTime;
                        if (Cells[idx].AshAge >= AshLifetimeSeconds)
                        {
                            Cells[idx] = default;
                            continue;
                        }
                        if (!allowAshMove) continue; // 灰は、独自の間隔が溜まるまで動かさない
                    }

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

            // 灰は砂と同じ物理で落ちるが、砂と違って底で止まらず、盤面の底に触れた瞬間に消える。
            // 移動のループの「後」に置くのは、底の行に着いた同じステップの中で消すため
            // (先に置くと、着いてから次のステップまで1回分だけ底に残ってしまう)。
            int bottomY = Height - 1;
            for (int x = 0; x < Width; x++)
            {
                int idx = Index(x, bottomY);
                if (Cells[idx].IsAsh) Cells[idx] = default;
            }
        }

        /// <summary>
        /// 「同色の連結領域が左端から右端まで到達しているか」をBFSで判定する。
        /// 連結は上下左右に加えて斜めも含む8方向。
        /// 砂山の縁は階段状に斜めで接することが多く、4方向だと斜めにしか接していない粒が
        /// 「繋がっていない」とみなされて、消去のあとに孤立した残骸として残ってしまうため。
        /// 検出と消去は同じ領域を使うので、この判定が両方に効く。
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
                if (!Cells[startIdx].Occupied || Cells[startIdx].IsConcrete || Cells[startIdx].IsAsh || visited[startIdx]) continue;

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
            if (visited[idx] || !Cells[idx].Occupied || Cells[idx].IsConcrete || Cells[idx].IsAsh) return;
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

        /// <summary>現在の地面(コンクリート)の高さ(ピクセル)。</summary>
        public int GroundHeightPx { get; private set; }

        /// <summary>
        /// 地面(コンクリート)の高さを指定のピクセル数に設定する。
        /// 増える方向のとき、その分だけ既存の中身(砂)を丸ごと上にずらしてから底をコンクリートにする
        /// (呑み込んで消すのではなく、底上げされて持ち上がるイメージ)。
        /// 一番上からはみ出た分は行き場がなく消える(盤面の外に押し出された扱い)。
        /// 減る方向のときは、空いた場所は何もしない(既存の重力処理で自然に砂が落ちてくる)。
        /// </summary>
        public void SetGroundHeightPx(int heightPx, Color32 groundColor)
        {
            if (heightPx < 0) heightPx = 0;
            if (heightPx > Height) heightPx = Height;
            if (heightPx == GroundHeightPx) return;

            int delta = heightPx - GroundHeightPx;
            if (delta > 0)
            {
                ShiftContentUp(delta);
            }

            int thresholdY = Height - heightPx; // この行(y >= thresholdY)以降がコンクリート
            for (int y = 0; y < Height; y++)
            {
                bool shouldBeConcrete = y >= thresholdY;
                for (int x = 0; x < Width; x++)
                {
                    int idx = Index(x, y);
                    if (shouldBeConcrete)
                    {
                        if (!Cells[idx].IsConcrete)
                        {
                            Cells[idx] = new Cell { Occupied = true, IsConcrete = true, Color = groundColor };
                        }
                    }
                    else if (Cells[idx].IsConcrete)
                    {
                        Cells[idx] = default;
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
            for (int y = 0; y < Height; y++)
            {
                int srcY = y + delta;
                for (int x = 0; x < Width; x++)
                {
                    int dstIdx = Index(x, y);
                    Cells[dstIdx] = srcY < Height ? Cells[Index(x, srcY)] : default;
                }
            }
        }

        /// <summary>
        /// 地面(コンクリート)の全体を、灰に変える。地面は無くなり、高さは0に戻る。
        /// 灰は、以降は砂と同じ物理で落ちていき、盤面の底に触れると消える(SimulateStep を参照)。
        /// </summary>
        public void ConvertGroundToAsh(Color32 ashColor)
        {
            for (int i = 0; i < Cells.Length; i++)
            {
                if (!Cells[i].IsConcrete) continue;
                Cells[i] = new Cell { Occupied = true, IsAsh = true, Color = ashColor };
            }
            GroundHeightPx = 0;
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
        /// positions と colors は同じ順序・同じ数である必要がある。
        /// colorIndex はピース全体で共通の「色グループ」で、ライン消去の同色判定に使う。
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