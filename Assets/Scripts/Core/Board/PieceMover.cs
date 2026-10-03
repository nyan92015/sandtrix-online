using System.Collections.Generic;
using UnityEngine;

namespace SandTetris
{
    /// <summary>
    /// ミノの左右移動・回転・落下の判定を担当する。
    /// </summary>
    public class PieceMover
    {
        readonly BoardModel _model;

        public PieceMover(BoardModel model)
        {
            _model = model;
        }

        public bool CanPlace(List<Vector2Int> offsets, Vector2Int anchor)
        {
            var grid = _model.Grid;
            foreach (var o in offsets)
            {
                var p = anchor + o;
                if (p.x < 0 || p.x >= grid.Width) return false; // 左右の壁
                if (p.y >= grid.Height) return false;           // 盤面の底
                if (p.y < 0) continue;                          // 盤面より上は障害物なし
                if (grid.IsOccupied(p.x, p.y)) return false;
            }
            return true;
        }

        public bool TryMove(int dx)
        {
            if (_model.IsGameOver || _model.CurrentPiece == null) return false;

            var newAnchor = _model.CurrentPiece.Anchor + new Vector2Int(dx, 0);
            if (!CanPlace(_model.CurrentPiece.Offsets, newAnchor)) return false;

            _model.CurrentPiece.Anchor = newAnchor;
            _model.RaisePieceMoved();
            return true;
        }

        public bool TryRotate(int dir)
        {
            if (_model.IsGameOver || _model.CurrentPiece == null) return false;

            var piece = _model.CurrentPiece;
            var rotated = piece.GetRotatedOffsets(dir);
            var anchor = piece.Anchor;

            int minX = int.MaxValue;
            int maxX = int.MinValue;
            foreach (var o in rotated)
            {
                int x = anchor.x + o.x;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
            }

            if (minX < 0) anchor.x -= minX;                                    // 左にはみ出た分だけ右へ
            if (maxX >= _model.Grid.Width) anchor.x -= (maxX - _model.Grid.Width + 1); // 右にはみ出た分だけ左へ

            piece.Offsets = rotated;
            piece.Anchor = anchor;
            piece.RotationSteps = ((piece.RotationSteps + (dir > 0 ? 1 : -1)) % 4 + 4) % 4;
            _model.RaisePieceRotated();
            return true;
        }

        public bool TryStepDown()
        {
            if (_model.IsGameOver || _model.CurrentPiece == null) return false;

            var below = _model.CurrentPiece.Anchor + new Vector2Int(0, 1);
            if (!CanFall(_model.CurrentPiece.Offsets, below)) return false;

            _model.CurrentPiece.Anchor = below;
            return true;
        }

        /// <summary>
        /// 落下専用の衝突判定。横方向のはみ出しは無視し、盤面の底と、
        /// 盤面内にある既存の砂とだけ衝突するかを見る。
        /// </summary>
        bool CanFall(List<Vector2Int> offsets, Vector2Int anchor)
        {
            var grid = _model.Grid;
            foreach (var o in offsets)
            {
                var p = anchor + o;
                if (p.x < 0 || p.x >= grid.Width) continue; // 横のはみ出しは無視
                if (p.y >= grid.Height) return false;       // 盤面の底
                if (p.y < 0) continue;                      // 盤面より上は障害物なし
                if (grid.IsOccupied(p.x, p.y)) return false;
            }
            return true;
        }
    }
}