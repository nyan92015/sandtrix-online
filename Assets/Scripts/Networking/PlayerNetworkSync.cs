using Fusion;
using UnityEngine;

namespace SandTetris
{
    /// <summary>
    /// プレイヤーごとにアタッチされるネットワークオブジェクト(Shared Mode前提)。
    ///
    /// Shared Modeでは、自分が生成したオブジェクトには自動的に StateAuthority が与えられるため、
    /// 自分の盤面データを Networked配列に直接書き込める。RPCは使わない。
    /// 配列はビットパッキング済み(1マス3ビット)のデータを保持し、Fusionが変化した部分だけを
    /// 自動的に検出して同期してくれる。
    /// </summary>
    public class PlayerNetworkSync : NetworkBehaviour
    {
        // 想定される最大の盤面サイズに余裕を持たせた固定長(Networked配列は容量を実行時に変えられないため)。
        // 注意: Fusionの NetworkArray<byte> は1要素につき1バイトではなく1ワード(4バイト)を消費し、
        // かつ1オブジェクトあたりの状態サイズには 8192ワード(32KB)という上限がある。
        // 標準盤面(10x20マス、blockSize=8→80x160ピクセル)なら3ビットパッキングで4800バイト必要なので、
        // 余裕を見て5120としている。これより盤面を大きくする場合はここも調整すること。
        public const int MaxPackedBytes = 5120;

        [Tooltip("Networked配列に書き込む間隔(秒)。小さいほど滑らかだが、Fusionの差分検出コストが増える。")]
        [SerializeField] float writeInterval = 0.15f;

        [Networked, Capacity(MaxPackedBytes)]
        public NetworkArray<byte> PackedBoard => default;

        [Networked] public int GridWidth { get; set; }
        [Networked] public int GridHeight { get; set; }
        [Networked] public int PackedLength { get; set; }

        // --- ここからミノ専用の軽量チャンネル。毎ティック更新するので滑らかに動く ---
        [Networked] public int PieceShapeIndex { get; set; } = -1; // -1 = ミノなし(着地直後の空白フレームなど)
        [Networked] public int PieceColorIndex { get; set; }
        [Networked] public int PieceRotationSteps { get; set; }
        [Networked] public int PieceAnchorX { get; set; }
        [Networked] public int PieceAnchorY { get; set; }

        // ネクストミノ(プレビュー表示用)。位置・回転は不要(常にプレビュー中央に表示するだけなので)
        [Networked] public int NextPieceShapeIndex { get; set; } = -1;
        [Networked] public int NextPieceColorIndex { get; set; }

        /// <summary>ローカル(自分)のBoardModel。Spawned時に自分のBoardViewから取得する。</summary>
        public BoardModel LocalModel;

        int _blockSize;
        RemoteBoardView _remoteView;
        float _timer;

        /// <summary>
        /// 生成された瞬間に呼ばれる。StateAuthorityを持っている(=自分自身の分身)かどうかで
        /// 送信側になるか受信側になるかを自動的に決める。
        /// </summary>
        public override void Spawned()
        {
            // blockSizeは送信側・受信側どちらでもミノの再構築に必要なので、
            // StateAuthorityの有無に関わらずシーン上のBoardViewから取得しておく。
            var localView = FindFirstObjectByType<BoardView>();
            if (localView != null) _blockSize = localView.BlockSize;

            if (Object.HasStateAuthority)
            {
                if (localView != null)
                {
                    LocalModel = localView.Model;
                    GridWidth = LocalModel.Grid.Width;
                    GridHeight = LocalModel.Grid.Height;
                }
            }
            else
            {
                _remoteView = FindFirstObjectByType<RemoteBoardView>();
            }
        }

        /// <summary>
        /// シミュレーションステップごとに呼ばれる(ネットワークの正式な状態を更新する場所)。
        /// StateAuthorityを持つ側だけが、自分の盤面をNetworked配列に書き込む。
        /// </summary>
        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority || LocalModel == null) return;

            // ミノの位置・形・回転は軽いデータなので、間引かずに毎ティック更新する。
            // これにより相手側でも「ワープ」せず、なめらかに動いて見える。
            var piece = LocalModel.CurrentPiece;
            if (piece != null)
            {
                PieceShapeIndex = piece.ShapeIndex;
                PieceColorIndex = piece.ColorIndex;
                PieceRotationSteps = piece.RotationSteps;
                PieceAnchorX = piece.Anchor.x;
                PieceAnchorY = piece.Anchor.y;
            }
            else
            {
                PieceShapeIndex = -1;
            }

            var nextPiece = LocalModel.NextPiece;
            if (nextPiece != null)
            {
                NextPieceShapeIndex = nextPiece.ShapeIndex;
                NextPieceColorIndex = nextPiece.ColorIndex;
            }
            else
            {
                NextPieceShapeIndex = -1;
            }

            // 固定された砂(重いデータ)は、今まで通り間引いて送る。
            _timer += Runner.DeltaTime;
            if (_timer < writeInterval) return;
            _timer = 0f;

            byte[] packed = BoardSnapshotCodec.EncodePacked(LocalModel.Grid);
            if (packed.Length > MaxPackedBytes)
            {
                Debug.LogError($"[PlayerNetworkSync] 盤面データが MaxPackedBytes({MaxPackedBytes})を超えています: {packed.Length}バイト。MaxPackedBytesを増やしてください。");
                return;
            }

            PackedLength = packed.Length;
            for (int i = 0; i < packed.Length; i++)
            {
                PackedBoard.Set(i, packed[i]);
            }
        }

        /// <summary>
        /// 描画フレームごとに呼ばれる(見た目の更新はここで行うのがFusion推奨)。
        /// StateAuthorityを持たない側(=相手の分身)だけが、届いたデータをデコードして
        /// RemoteBoardViewに渡す。
        /// </summary>
        public override void Render()
        {
            if (Object.HasStateAuthority) return;
            if (_remoteView == null) return;

            // 固定された砂
            if (PackedLength > 0 && GridWidth > 0 && GridHeight > 0)
            {
                var packed = new byte[PackedLength];
                for (int i = 0; i < PackedLength; i++) packed[i] = PackedBoard[i];

                int cellCount = GridWidth * GridHeight;
                byte[] unpacked = BoardSnapshotCodec.DecodePacked(packed, cellCount);
                _remoteView.ApplySnapshot(unpacked, GridWidth, GridHeight);
            }

            // 操作中ミノ(軽量チャンネルから毎フレーム再構築する)
            if (PieceShapeIndex >= 0 && _blockSize > 0)
            {
                var anchor = new Vector2Int(PieceAnchorX, PieceAnchorY);
                var piece = FallingPiece.CreateFromShapeWithRotation(
                    PieceShapeIndex, PieceColorIndex, _blockSize, anchor, PieceRotationSteps);
                _remoteView.ApplyPiece(piece);
            }
            else
            {
                _remoteView.ApplyPiece(null);
            }

            // ネクストミノ(プレビュー表示用)
            if (NextPieceShapeIndex >= 0 && _blockSize > 0)
            {
                var nextPiece = FallingPiece.CreateFromShape(NextPieceShapeIndex, NextPieceColorIndex, _blockSize, Vector2Int.zero);
                _remoteView.ApplyNextPiece(nextPiece);
            }
            else
            {
                _remoteView.ApplyNextPiece(null);
            }
        }
    }
}