using UnityEngine;

namespace SandTetris
{
    /// 砂の重力・崩れる物理を担当する。Gridが持つCells配列を直接書き換える。
    public class PhysicsSimulator
    {
        readonly Grid _grid;

        /// 斜め移動が実際に発生する確率(0〜1)。
        public float DiagonalMoveChance = 0.5f;

        /// 真下への落下が実際に発生する確率(0〜1)。
        public float FallMoveChance = 1.0f;

        /// <summary>
        /// 灰が1マス落ちる間隔(秒)。砂の重力(呼び出し側の間隔)とは無関係の、固定値。
        /// ライン数による加速(SpeedMultiplier)の影響を受けない
        /// (ゲームを通してずっとこの速さのまま。初期値だけ砂の重力とは別に設定する)。
        /// </summary>
        public float AshGravityInterval = 0.15f;

        /// 灰が生まれてから、消えるまでの時間(秒)。
        public float AshLifetimeSeconds = 6f;

        int _frameParity; // 毎ステップ左右のスキャン方向を反転させ、偏りを防ぐ
        float _ashAccumulator;

        public PhysicsSimulator(Grid grid)
        {
            _grid = grid;
        }

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
            var cells = _grid.Cells;
            int width = _grid.Width;
            int height = _grid.Height;

            _frameParity ^= 1;

            _ashAccumulator += deltaTime;
            bool allowAshMove = false;
            if (_ashAccumulator >= AshGravityInterval)
            {
                _ashAccumulator -= AshGravityInterval;
                allowAshMove = true;
            }

            for (int y = height - 2; y >= 0; y--)
            {
                bool leftToRight = ((y + _frameParity) & 1) == 0;
                for (int xi = 0; xi < width; xi++)
                {
                    int x = leftToRight ? xi : width - 1 - xi;
                    int idx = _grid.Index(x, y);
                    if (!cells[idx].Occupied || cells[idx].IsConcrete) continue; // コンクリートは動かない

                    if (cells[idx].IsAsh)
                    {
                        // 砂の下に埋もれて動けないままでも、時間切れなら強制的に消える(取り残し対策)。
                        // 動けるかどうかに関わらず毎ステップ年を取らせるので、埋もれて止まっていても消える。
                        cells[idx].AshAge += deltaTime;
                        if (cells[idx].AshAge >= AshLifetimeSeconds)
                        {
                            cells[idx] = default;
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
            int bottomY = height - 1;
            for (int x = 0; x < width; x++)
            {
                int idx = _grid.Index(x, bottomY);
                if (cells[idx].IsAsh) cells[idx] = default;
            }
        }

        bool IsFree(int x, int y) => _grid.InBounds(x, y) && !_grid.Cells[_grid.Index(x, y)].Occupied;

        void MoveCell(int x1, int y1, int x2, int y2)
        {
            int i1 = _grid.Index(x1, y1);
            int i2 = _grid.Index(x2, y2);
            _grid.Cells[i2] = _grid.Cells[i1];
            _grid.Cells[i1] = default;
        }
    }
}