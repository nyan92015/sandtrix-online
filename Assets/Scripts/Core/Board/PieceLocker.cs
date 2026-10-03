using System.Collections.Generic;
using UnityEngine;

namespace SandTetris
{
    /// <summary>
    /// ミノの着地処理(グリッドへの焼き込み・感染・ゲームオーバー判定)を担当する。
    /// </summary>
    public class PieceLocker
    {
        readonly BoardModel _model;
        readonly PieceSpawner _spawner;

        public PieceLocker(BoardModel model, PieceSpawner spawner)
        {
            _model = model;
            _spawner = spawner;
        }

        /// <summary>
        /// 現在のミノをグリッドに焼き込み、次のミノを出現させる。
        /// 着地した瞬間、盤面の上端(y=0)より上にはみ出ている部分が1つでもあれば、
        /// そこでゲームオーバーとする(はみ出た部分自体は Grid.Bake 側で自動的に無視される)。
        /// </summary>
        public void LockCurrentPiece()
        {
            if (_model.IsGameOver || _model.CurrentPiece == null) return;

            var piece = _model.CurrentPiece;
            var positions = new List<Vector2Int>(piece.Offsets.Count);
            bool overflowsTop = false;
            foreach (var o in piece.Offsets)
            {
                var p = piece.Anchor + o;
                positions.Add(p);
                if (p.y < 0) overflowsTop = true;
            }

            _model.Grid.Bake(positions, piece.PixelColors, piece.ColorIndex);
            _model.RaisePieceLanded(positions, piece.PixelColors, piece.ColorIndex);

            if (piece.IsSpecial)
            {
                // 自分が触れた、同じ色の連結領域を、自分の色に塗り替える。
                // 着地して焼き込んだ直後なので、positions自身はもう普通の色付き砂として存在している。
                var changedIndices = _model.Grid.InfectConnectedRegions(positions, piece.PixelColors[0], piece.ColorIndex, out var oldColors);
                if (changedIndices.Count > 0)
                {
                    _model.RaiseInfection(changedIndices, oldColors, piece.Anchor);
                }
            }

            if (overflowsTop)
            {
                _model.ForceGameOver();
                return;
            }

            _spawner.SpawnNext();
        }
    }
}