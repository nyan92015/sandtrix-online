using System;
using System.Collections.Generic;
using UnityEngine;

namespace SandTetris
{
    /// <summary>
    /// 盤面のModel。Unity非依存(MonoBehaviourを継承しない)で、SandGridとFallingPieceを内包する。
    ///
    /// 責務:
    /// - ミノの移動・回転・落下・着地の「ルール」を持つ(タイミング制御はPresenterの仕事)
    /// - 状態が変化したら、その事実をイベントとして発行するだけ。誰が聞いているか、
    ///   聞いた側が何をするか(描画・SE・ネットワーク送信など)は一切関知しない
    ///
    /// これにより、同じBoardModelを「自分の盤面」にも「(将来的に)リプレイやテスト用の
    /// 決定論的シミュレーション」にも使い回せる。ViewやPresenterを差し替えるだけでいい。
    /// </summary>
    public class BoardModel
    {
        public SandGrid Grid { get; }
        public FallingPiece CurrentPiece { get; private set; }
        public FallingPiece NextPiece { get; private set; }
        public bool IsGameOver { get; private set; }

        readonly int _blockSize;
        readonly Vector2Int _spawnAnchor;

        // --- イベント: 「何が起きたか」だけを伝える。以後の反応(SE・演出・通信)は購読側の仕事 ---

        /// <summary>新しいミノが出現した</summary>
        public event Action OnPieceSpawned;

        /// <summary>ミノが左右に移動した</summary>
        public event Action OnPieceMoved;

        /// <summary>ミノが回転した</summary>
        public event Action OnPieceRotated;

        /// <summary>
        /// ミノが着地してグリッドに焼き込まれた。
        /// 引数は着地した位置・色・色グループ(着地フラッシュ演出などに使える)。
        /// </summary>
        public event Action<IReadOnlyList<Vector2Int>, IReadOnlyList<Color32>, byte> OnPieceLanded;

        /// <summary>
        /// 同色の連結ラインが見つかった(まだ消していない、光らせる演出用のタイミング)。
        /// 引数は消去対象のセルインデックス一覧。
        /// </summary>
        public event Action<IReadOnlyList<int>> OnLinesFound;

        /// <summary>見つかったラインが実際に消去された</summary>
        public event Action OnLinesCleared;

        /// <summary>新しいミノを配置できず、ゲームオーバーになった</summary>
        public event Action OnGameOver;

        public BoardModel(int widthPx, int heightPx, int blockSize, Vector2Int spawnAnchor)
        {
            Grid = new SandGrid(widthPx, heightPx);
            _blockSize = blockSize;
            _spawnAnchor = spawnAnchor;

            NextPiece = FallingPiece.CreateRandom(blockSize, Vector2Int.zero);
            SpawnNext();
        }

        /// <summary>
        /// 待機中だった NextPiece を CurrentPiece に昇格させ、新しい NextPiece を用意する。
        /// ミノは盤面の外(上端よりさらに上)から出現するため、生成した瞬間に
        /// 何かにぶつかることは基本的にない。ゲームオーバー判定は LockCurrentPiece 側で行う。
        /// </summary>
        public void SpawnNext()
        {
            CurrentPiece = NextPiece;
            CurrentPiece.Anchor = _spawnAnchor;
            NextPiece = FallingPiece.CreateRandom(_blockSize, Vector2Int.zero);

            OnPieceSpawned?.Invoke();
        }

        /// <summary>
        /// 指定オフセット群をその位置に置けるかどうかを判定する。
        /// 左右の壁・盤面の底は通常通りブロックするが、盤面の上端より上(y &lt; 0)は
        /// まだ何もない空間として扱い、常に「置ける」とみなす
        /// (ミノは盤面の外側、上端よりさらに上から出現して落ちてくるため)。
        /// </summary>
        public bool CanPlace(List<Vector2Int> offsets, Vector2Int anchor)
        {
            foreach (var o in offsets)
            {
                var p = anchor + o;
                if (p.x < 0 || p.x >= Grid.Width) return false; // 左右の壁
                if (p.y >= Grid.Height) return false;           // 盤面の底
                if (p.y < 0) continue;                          // 盤面より上は障害物なし
                if (Grid.IsOccupied(p.x, p.y)) return false;
            }
            return true;
        }

        public bool TryMove(int dx)
        {
            if (IsGameOver || CurrentPiece == null) return false;

            var newAnchor = CurrentPiece.Anchor + new Vector2Int(dx, 0);
            if (!CanPlace(CurrentPiece.Offsets, newAnchor)) return false;

            CurrentPiece.Anchor = newAnchor;
            OnPieceMoved?.Invoke();
            return true;
        }

        public bool TryRotate(int dir)
        {
            if (IsGameOver || CurrentPiece == null || CurrentPiece.IsSquare) return false;

            var rotated = CurrentPiece.GetRotatedOffsets(dir);
            if (!CanPlace(rotated, CurrentPiece.Anchor)) return false;

            CurrentPiece.Offsets = rotated;
            CurrentPiece.RotationSteps = ((CurrentPiece.RotationSteps + (dir > 0 ? 1 : -1)) % 4 + 4) % 4;
            OnPieceRotated?.Invoke();
            return true;
        }

        /// <summary>
        /// 1マス下に移動を試みる。移動できれば true、着地(移動不可)なら false を返すだけで、
        /// 着地処理そのものは行わない(呼び出し側が false を見て LockCurrentPiece を呼ぶ)。
        /// 「落下タイミングの管理」と「落下できるかの判定」を分けることで、
        /// 通常落下・ソフトドロップ・(将来の)ハードドロップから同じメソッドを再利用できる。
        /// </summary>
        public bool TryStepDown()
        {
            if (IsGameOver || CurrentPiece == null) return false;

            var below = CurrentPiece.Anchor + new Vector2Int(0, 1);
            if (!CanPlace(CurrentPiece.Offsets, below)) return false;

            CurrentPiece.Anchor = below;
            return true;
        }

        /// <summary>
        /// 現在のミノをグリッドに焼き込み、次のミノを出現させる。
        /// 着地した瞬間、盤面の上端(y=0)より上にはみ出ている部分が1つでもあれば、
        /// そこでゲームオーバーとする(はみ出た部分自体は Grid.Bake 側で自動的に無視される)。
        /// </summary>
        public void LockCurrentPiece()
        {
            if (IsGameOver || CurrentPiece == null) return;

            var positions = new List<Vector2Int>(CurrentPiece.Offsets.Count);
            bool overflowsTop = false;
            foreach (var o in CurrentPiece.Offsets)
            {
                var p = CurrentPiece.Anchor + o;
                positions.Add(p);
                if (p.y < 0) overflowsTop = true;
            }

            Grid.Bake(positions, CurrentPiece.PixelColors, CurrentPiece.ColorIndex);
            OnPieceLanded?.Invoke(positions, CurrentPiece.PixelColors, CurrentPiece.ColorIndex);

            if (overflowsTop)
            {
                IsGameOver = true;
                OnGameOver?.Invoke();
                return;
            }

            SpawnNext();
        }

        /// <summary>砂の物理シミュレーションを1ステップ進める。呼ぶ間隔はPresenterが管理する。</summary>
        public void SimulatePhysicsStep()
        {
            Grid.SimulateStep();
        }

        /// <summary>
        /// 同色の連結ラインを探す。見つかれば OnLinesFound を発行して true を返す。
        /// 実際に消すのは ApplyLineClear が呼ばれるまで行わない(光らせる演出の時間を確保するため)。
        /// </summary>
        public bool CheckForLineClear()
        {
            bool found = Grid.CheckAndClearConnectedLine();
            if (found) OnLinesFound?.Invoke(Grid.LastClearedIndices);
            return found;
        }

        /// <summary>CheckForLineClear が見つけたラインを実際に消去する。</summary>
        public void ApplyLineClear()
        {
            Grid.ApplyClear();
            OnLinesCleared?.Invoke();
        }
    }
}