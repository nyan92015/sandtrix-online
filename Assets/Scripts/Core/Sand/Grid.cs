using System.Collections.Generic;
using UnityEngine;

namespace SandTetris
{
    /// <summary>
    /// 【ステージ2】砂の土台。
    /// Cells配列そのものの所有と、基本的な問い合わせ(Index/InBounds/IsOccupied)、
    /// ミノの焼き込み(Bake)を担当する「本体」。
    ///
    /// 物理演算・ライン消去・地面の高さ管理・感染処理は、それぞれ専門のクラス
    /// (Physics/LineClear/Ground/Infection)に分かれている。Gridはそれらをまとめて持ち、
    /// 呼び出し側からは今まで通りの窓口メソッド(SimulateStepなど)で使えるようにしてある。
    /// </summary>
    public class Grid
    {
        public readonly int Width;
        public readonly int Height;
        public Cell[] Cells;

        /// <summary>砂の重力・崩れる物理を担当する。</summary>
        public PhysicsSimulator Physics { get; }

        /// <summary>ライン消去の判定・実行を担当する。</summary>
        public LineClearDetector LineClear { get; }

        /// <summary>地面(コンクリート)の高さ管理・灰化を担当する。</summary>
        public ConcreteGrid Ground { get; }

        /// <summary>特別なミノの、色の伝染処理を担当する。</summary>
        public InfectionSpreader Infection { get; }

        public Grid(int width, int height)
        {
            Width = width;
            Height = height;
            Cells = new Cell[width * height];

            Physics = new PhysicsSimulator(this);
            LineClear = new LineClearDetector(this);
            Ground = new ConcreteGrid(this);
            Infection = new InfectionSpreader(this);
        }

        public int Index(int x, int y) => y * Width + x;

        public bool InBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

        public bool IsOccupied(int x, int y) => InBounds(x, y) && Cells[Index(x, y)].Occupied;

        /// <summary>
        /// ミノが着地したとき、そのピクセル群をそのままグリッドに焼き込む。
        /// positions と colors は同じ順序・同じ数である必要がある。
        /// colorIndex はピース全体で共通の「色グループ」で、ライン消去の同色判定に使う。
        /// </summary>
        public void Bake(IReadOnlyList<Vector2Int> positions, IReadOnlyList<Color32> colors, byte colorIndex)
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

        // --- ここから、呼び出し側の利便性のための窓口。中身は、それぞれの専門クラスへそのまま転送する ---

        public float DiagonalMoveChance { get => Physics.DiagonalMoveChance; set => Physics.DiagonalMoveChance = value; }
        public float FallMoveChance { get => Physics.FallMoveChance; set => Physics.FallMoveChance = value; }
        public float AshGravityInterval { get => Physics.AshGravityInterval; set => Physics.AshGravityInterval = value; }
        public float AshLifetimeSeconds { get => Physics.AshLifetimeSeconds; set => Physics.AshLifetimeSeconds = value; }
        public void SimulateStep(float deltaTime) => Physics.SimulateStep(deltaTime);

        public List<int> LastClearedIndices => LineClear.LastClearedIndices;
        public bool CheckAndClearConnectedLine() => LineClear.CheckAndClearConnectedLine();
        public void ApplyClear() => LineClear.ApplyClear();

        public int GroundHeightPx => Ground.GroundHeightPx;
        public Color32 AshColor { get => Ground.AshColor; set => Ground.AshColor = value; }
        public void SetGroundHeightPx(int heightPx, Color32 groundColor) => Ground.SetGroundHeightPx(heightPx, groundColor);
        public void ConvertGroundToAsh() => Ground.ConvertGroundToAsh();

        public List<int> InfectConnectedRegions(IReadOnlyList<Vector2Int> originCells, Color32 newColor, byte newColorIndex, out List<Color32> oldColors)
            => Infection.InfectConnectedRegions(originCells, newColor, newColorIndex, out oldColors);
    }
}