using UnityEngine;

namespace SandTetris
{
    /// <summary>
    /// 新しいミノの生成・出現を担当する。
    /// </summary>
    public class PieceSpawner
    {
        readonly BoardModel _model;
        readonly int _blockSize;
        readonly Vector2Int _spawnAnchor;

        public PieceSpawner(BoardModel model, int blockSize, Vector2Int spawnAnchor)
        {
            _model = model;
            _blockSize = blockSize;
            _spawnAnchor = spawnAnchor;
        }

        /// <summary>
        /// 待機中だった NextPiece を CurrentPiece に昇格させ、新しい NextPiece を用意する。
        /// ミノは盤面の外(上端よりさらに上)から出現するため、生成した瞬間に
        /// 何かにぶつかることは基本的にない。ゲームオーバー判定は PieceLocker 側で行う。
        ///
        /// 生成位置のy座標(盤面上端からどれだけ上にはみ出すか)は、本家と同じ考え方で決める。
        /// 本家は、各ミノごとに「どの回転状態でも収まる正方形のキャンバス」を持っていて、
        /// その一辺の半分だけ上にはみ出す仕組みになっている。90度回転すると幅と高さが入れ替わるだけなので、
        /// この正方形の一辺は「生成直後(回転前)の、幅と高さのうち大きい方」と等しい。
        /// x座標(横位置)は、これまで通り固定の中央値をそのまま使う。
        /// </summary>
        public void SpawnNext()
        {
            _model.CurrentPiece = _model.NextPiece;

            // フィーバー開始の直前に、まだ特別ではないネクストがすでに用意されていた場合に備えて、
            // 昇格の瞬間にも念のため変換しておく(フィーバー中は、例外なく全部が特別なミノになるように)
            if (_model.Fever.IsFeverActive && !_model.CurrentPiece.IsSpecial)
            {
                _model.CurrentPiece = FallingPiece.CreateRandomSpecial(_blockSize, Vector2Int.zero);
            }

            int canvasSize = ComputePieceCanvasSize(_model.CurrentPiece);
            _model.CurrentPiece.Anchor = new Vector2Int(_spawnAnchor.x, -(canvasSize / 2));

            _model.NextPiece = _model.Fever.IsFeverActive
                ? FallingPiece.CreateRandomSpecial(_blockSize, Vector2Int.zero)
                : FallingPiece.CreateRandom(_blockSize, Vector2Int.zero);

            _model.RaisePieceSpawned();
        }

        /// <summary>
        /// デバッグ用: 今操作中のミノを、その場(同じ位置)で特別なミノに差し替える。
        /// 形・色はランダムに選び直す。
        /// </summary>
        public void DebugReplaceCurrentPieceWithSpecial()
        {
            if (_model.CurrentPiece == null) return;
            _model.CurrentPiece = FallingPiece.CreateRandomSpecial(_blockSize, _model.CurrentPiece.Anchor);
        }

        /// <summary>
        /// ミノの、回転前(生成直後)の幅・高さのうち大きい方(ピクセル)を、Offsetsの範囲から求める。
        /// 「どの回転状態でも収まる正方形」の一辺に相当する。
        /// </summary>
        static int ComputePieceCanvasSize(FallingPiece piece)
        {
            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;
            foreach (var o in piece.Offsets)
            {
                if (o.x < minX) minX = o.x;
                if (o.x > maxX) maxX = o.x;
                if (o.y < minY) minY = o.y;
                if (o.y > maxY) maxY = o.y;
            }
            int width = maxX - minX + 1;
            int height = maxY - minY + 1;
            return Mathf.Max(width, height);
        }
    }
}