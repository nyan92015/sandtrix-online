using UnityEngine;

namespace SandTetris
{
    /// <summary>
    /// 自分(StateAuthorityを持つ側)の盤面の状態を、PlayerNetworkSyncのNetworked配列・
    /// プロパティへ書き込む。
    /// </summary>
    public class PlayerNetworkSender
    {
        readonly PlayerNetworkSync _sync;
        readonly float _writeInterval;
        float _timer;

        public PlayerNetworkSender(PlayerNetworkSync sync, float writeInterval)
        {
            _sync = sync;
            _writeInterval = writeInterval;
        }

        /// <summary>
        /// 着地した瞬間に呼ぶ。タイマーを間引き間隔いっぱいまで進めておくことで、
        /// 次の Write で確実に盤面(固定砂)を送らせる。
        /// </summary>
        public void ForceSendSoon()
        {
            _timer = _writeInterval;
        }

        /// <summary>毎ティック呼ぶ。軽いデータ(ミノ・スコア)は毎回、重いデータ(盤面)は間引いて送る。</summary>
        public void Write(BoardModel model, ScoreTracker score, GroundLevelController ground, BoardView myOwnBoardView, float deltaTime)
        {
            if (model == null) return;

            // ミノの位置・形・回転は軽いデータなので、間引かずに毎ティック更新する。
            // これにより相手側でも「ワープ」せず、なめらかに動いて見える。
            var piece = model.CurrentPiece;
            if (piece != null)
            {
                _sync.PieceShapeIndex = piece.ShapeIndex;
                _sync.PieceColorIndex = piece.ColorIndex;
                _sync.PieceRotationSteps = piece.RotationSteps;
                _sync.PieceAnchorX = piece.Anchor.x;
                _sync.PieceAnchorY = piece.Anchor.y;
                _sync.PieceIsSpecial = piece.IsSpecial;
            }
            else
            {
                _sync.PieceShapeIndex = -1;
            }

            var nextPiece = model.NextPiece;
            if (nextPiece != null)
            {
                _sync.NextPieceShapeIndex = nextPiece.ShapeIndex;
                _sync.NextPieceColorIndex = nextPiece.ColorIndex;
            }
            else
            {
                _sync.NextPieceShapeIndex = -1;
            }

            if (score != null)
            {
                _sync.NetScore = score.TotalScore;
                _sync.NetMultiplier = score.CurrentMultiplier;
                _sync.NetLinesCleared = score.LinesCleared;
                _sync.NetBucketFillRatio = score.CurrentBucketFillRatio;
            }

            if (ground != null)
            {
                _sync.NetGroundWarningLevel = ground.PendingWarningLevel;
            }

            _sync.NetIsGameOver = model.IsGameOver;

            // 固定された砂(重いデータ)は、間引いて送る。
            _timer += deltaTime;
            if (_timer < _writeInterval) return;
            _timer = 0f;

            // 今まさにフラッシュ演出中(白)・もう消えた扱いのセルがあれば、それも一緒に送る
            var whiteIndices = myOwnBoardView != null ? myOwnBoardView.GetCurrentWhiteIndices() : null;
            var goneIndices = myOwnBoardView != null ? myOwnBoardView.GetCurrentGoneIndices() : null;
            byte[] packed = BoardSnapshotCodec.EncodePacked(model.Grid, whiteIndices, goneIndices);
            if (packed.Length > PlayerNetworkSync.MaxPackedBytes)
            {
                return;
            }

            _sync.PackedLength = packed.Length;
            for (int i = 0; i < packed.Length; i++)
            {
                _sync.PackedBoard.Set(i, packed[i]);
            }
        }
    }
}