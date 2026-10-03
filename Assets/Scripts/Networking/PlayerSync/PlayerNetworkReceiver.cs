using UnityEngine;

namespace SandTetris
{
    /// <summary>
    /// 相手(StateAuthorityを持たない側)から届いたNetworked状態を読み取り、
    /// RemoteBoardViewへ反映する。
    /// </summary>
    public class PlayerNetworkReceiver
    {
        readonly PlayerNetworkSync _sync;
        readonly RemoteBoardView _remoteView;
        readonly int _blockSize;
        readonly MatchResultController _matchResultController;
        readonly BoardView _myOwnBoardView;

        bool _opponentGameOverNotified;

        public PlayerNetworkReceiver(PlayerNetworkSync sync, RemoteBoardView remoteView, int blockSize, MatchResultController matchResultController, BoardView myOwnBoardView)
        {
            _sync = sync;
            _remoteView = remoteView;
            _blockSize = blockSize;
            _matchResultController = matchResultController;
            _myOwnBoardView = myOwnBoardView;
        }

        /// <summary>描画フレームごとに呼ぶ。</summary>
        public void Apply()
        {
            if (_remoteView == null) return;

            // 固定された砂
            if (_sync.PackedLength > 0 && _sync.GridWidth > 0 && _sync.GridHeight > 0)
            {
                var packed = new byte[_sync.PackedLength];
                for (int i = 0; i < _sync.PackedLength; i++) packed[i] = _sync.PackedBoard[i];

                int cellCount = _sync.GridWidth * _sync.GridHeight;
                byte[] unpacked = BoardSnapshotCodec.DecodePacked(packed, cellCount);
                _remoteView.ApplySnapshot(unpacked, _sync.GridWidth, _sync.GridHeight);
            }

            // 操作中ミノ(軽量チャンネルから毎フレーム再構築する)
            if (_sync.PieceShapeIndex >= 0 && _blockSize > 0)
            {
                var anchor = new Vector2Int(_sync.PieceAnchorX, _sync.PieceAnchorY);
                var piece = FallingPiece.CreateFromShapeWithRotation(
                    _sync.PieceShapeIndex, _sync.PieceColorIndex, _blockSize, anchor, _sync.PieceRotationSteps, _sync.PieceIsSpecial);
                _remoteView.ApplyPiece(piece);
            }
            else
            {
                _remoteView.ApplyPiece(null);
            }

            // ネクストミノ(プレビュー表示用)
            if (_sync.NextPieceShapeIndex >= 0 && _blockSize > 0)
            {
                var nextPiece = FallingPiece.CreateFromShape(_sync.NextPieceShapeIndex, _sync.NextPieceColorIndex, _blockSize, Vector2Int.zero);
                _remoteView.ApplyNextPiece(nextPiece);
            }
            else
            {
                _remoteView.ApplyNextPiece(null);
            }

            // スコア関連
            _remoteView.ApplyScore(_sync.NetScore, _sync.NetMultiplier, _sync.NetLinesCleared, _sync.NetBucketFillRatio);

            // 警告表示(相手が守備側で、基準点を超えているときの「予定の高さ」)
            _remoteView.ApplyGroundWarning(_sync.NetGroundWarningLevel);

            // 相手のスコアを、自分自身の盤面にも伝える(シーソー式の地面の高さを決めるため)
            _myOwnBoardView?.SetOpponentScore(_sync.NetScore);

            // 相手がゲームオーバーになった(=自分の勝ち)瞬間を検知する。1回だけ通知すればいいので、
            // まだ通知していないときだけ発火させる。
            if (_sync.NetIsGameOver && !_opponentGameOverNotified)
            {
                _opponentGameOverNotified = true;
                _matchResultController?.NotifyOpponentGameOver();
            }
        }
    }
}