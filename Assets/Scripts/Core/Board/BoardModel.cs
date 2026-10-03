using System;
using System.Collections.Generic;
using UnityEngine;

namespace SandTetris
{
    /// <summary>
    /// 盤面のModel。Unity非依存(MonoBehaviourを継承しない)で、GridとFallingPieceを内包する。
    /// - ミノの移動・回転・落下・着地の「ルール」を持つ(タイミング制御はPresenterの仕事)
    /// </summary>
    public class BoardModel
    {
        public Grid Grid { get; }
        public FallingPiece CurrentPiece { get; internal set; }
        public FallingPiece NextPiece { get; internal set; }
        public bool IsGameOver { get; private set; }

        /// <summary>フィーバーシステム。ライン消去の色を見て、ゲージの進行・フィーバーの開始を管理する。</summary>
        public FeverSystem Fever { get; private set; }

        readonly PieceSpawner _spawner;
        readonly PieceMover _mover;
        readonly PieceLocker _locker;

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
        /// 特別なミノが着地して、周囲を感染させた瞬間に発火する。
        /// 引数: 変化したセルのインデックス一覧、それぞれの変化前の色、感染の中心地点(着地したミノの位置)。
        /// 円形に広がる演出などに使う想定。
        /// </summary>
        public event Action<IReadOnlyList<int>, IReadOnlyList<Color32>, Vector2Int> OnInfection;

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
            Grid = new Grid(widthPx, heightPx);

            // OnLinesFound を購読するので、NextPiece/SpawnNext より前に用意しておく
            Fever = new FeverSystem(this);

            _spawner = new PieceSpawner(this, blockSize, spawnAnchor);
            _mover = new PieceMover(this);
            _locker = new PieceLocker(this, _spawner);

            NextPiece = FallingPiece.CreateRandom(blockSize, Vector2Int.zero);
            _spawner.SpawnNext();
        }

        // --- 専門クラスから呼んでもらう、イベント発行用の窓口 ---
        internal void RaisePieceSpawned() => OnPieceSpawned?.Invoke();
        internal void RaisePieceMoved() => OnPieceMoved?.Invoke();
        internal void RaisePieceRotated() => OnPieceRotated?.Invoke();
        internal void RaisePieceLanded(IReadOnlyList<Vector2Int> positions, IReadOnlyList<Color32> colors, byte colorIndex)
            => OnPieceLanded?.Invoke(positions, colors, colorIndex);
        internal void RaiseInfection(IReadOnlyList<int> changedIndices, IReadOnlyList<Color32> oldColors, Vector2Int center)
            => OnInfection?.Invoke(changedIndices, oldColors, center);

        /// <summary>
        /// 強制的にゲームオーバーにする。すでにゲームオーバーなら何もしない(二重発行防止)。
        /// ミノがはみ出たとき以外に、シーソー式の地面が盤面全体に達したときなどにも使う。
        /// </summary>
        public void ForceGameOver()
        {
            if (IsGameOver) return;
            IsGameOver = true;
            OnGameOver?.Invoke();
        }

        /// <summary>
        /// 砂の物理シミュレーションを1ステップ進める。呼ぶ間隔はPresenterが管理する。
        /// deltaTime: この1回が表す実時間(秒)。灰専用の固定間隔の積み立てに使うので、
        /// 呼び出し側が実際に使っている間隔(砂の重力間隔)をそのまま渡すこと。
        /// </summary>
        public void SimulatePhysicsStep(float deltaTime) => Grid.SimulateStep(deltaTime);

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

        // --- ここから、呼び出し側の利便性のための窓口。中身は、それぞれの専門クラスへそのまま転送する ---

        public void SpawnNext() => _spawner.SpawnNext();
        public void DebugReplaceCurrentPieceWithSpecial() => _spawner.DebugReplaceCurrentPieceWithSpecial();
        public bool CanPlace(List<Vector2Int> offsets, Vector2Int anchor) => _mover.CanPlace(offsets, anchor);
        public bool TryMove(int dx) => _mover.TryMove(dx);
        public bool TryRotate(int dir) => _mover.TryRotate(dir);
        public bool TryStepDown() => _mover.TryStepDown();
        public void LockCurrentPiece() => _locker.LockCurrentPiece();
    }
}