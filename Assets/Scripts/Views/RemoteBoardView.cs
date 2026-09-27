using UnityEngine;
using UnityEngine.UI;

namespace SandTetris
{
    /// <summary>
    /// 相手の盤面を表示するだけのView。BoardModelを持たず、シミュレーションも一切行わない。
    /// ネットワークから届いたスナップショットをそのまま描画するだけの、いわば「ただのモニター」。
    /// </summary>
    public class RemoteBoardView : MonoBehaviour
    {
        [Header("Grid Settings(自分の盤面と同じ値にすること)")]
        [SerializeField] int gridWidthInBlocks = 10;
        [SerializeField] int gridHeightInBlocks = 20;
        [SerializeField] int blockSize = 6;

        [Header("Rendering")]
        [SerializeField] RawImage displayImage;
        [SerializeField] Color32 backgroundColor = new Color32(18, 18, 24, 255);

        BoardRenderer _renderer;
        byte[] _latestSnapshot;
        FallingPiece _latestPiece;

        void Start()
        {
            int widthPx = gridWidthInBlocks * blockSize;
            int heightPx = gridHeightInBlocks * blockSize;

            _renderer = new BoardRenderer(widthPx, heightPx) { BackgroundColor = backgroundColor };
            if (displayImage != null) displayImage.texture = _renderer.Texture;
        }

        /// <summary>PlayerNetworkSync.Render から、デコード済みのスナップショットを渡してもらう想定。</summary>
        public void ApplySnapshot(byte[] snapshot)
        {
            _latestSnapshot = snapshot;
        }

        /// <summary>
        /// PlayerNetworkSync.Render から、軽量チャンネルの情報を元に再構築したミノを渡してもらう想定。
        /// null なら「今は表示するミノがない」という意味(着地直後の一瞬など)。
        /// </summary>
        public void ApplyPiece(FallingPiece piece)
        {
            _latestPiece = piece;
        }

        void Update()
        {
            if (_latestSnapshot == null || _renderer == null) return;

            _renderer.DrawFromSnapshot(_latestSnapshot, TetrominoShapes.Colors);
            _renderer.DrawPiece(_latestPiece);
            _renderer.Upload();
        }
    }
}