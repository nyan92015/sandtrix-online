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

        [Header("Next Piece Preview")]
        [SerializeField] RawImage nextPieceDisplayImage;
        [SerializeField] Color32 previewBackgroundColor = new Color32(30, 30, 38, 255);

        BoardRenderer _renderer;
        NextPiecePreviewRenderer _previewRenderer;
        byte[] _latestSnapshot;
        FallingPiece _latestPiece;
        FallingPiece _latestNextPiece;
        int _currentWidthPx;
        int _currentHeightPx;

        void Start()
        {
            int widthPx = gridWidthInBlocks * blockSize;
            int heightPx = gridHeightInBlocks * blockSize;
            CreateRenderer(widthPx, heightPx);

            _previewRenderer = new NextPiecePreviewRenderer(blockSize * 4) { BackgroundColor = previewBackgroundColor };
            if (nextPieceDisplayImage != null) nextPieceDisplayImage.texture = _previewRenderer.Texture;
        }

        void CreateRenderer(int widthPx, int heightPx)
        {
            _currentWidthPx = widthPx;
            _currentHeightPx = heightPx;
            _renderer = new BoardRenderer(widthPx, heightPx) { BackgroundColor = backgroundColor };
            if (displayImage != null) displayImage.texture = _renderer.Texture;
        }

        /// <summary>
        /// PlayerNetworkSync.Render から、デコード済みのスナップショットと、その実際のピクセルサイズを渡してもらう想定。
        /// Inspectorの設定(gridWidthInBlocks等)が送信側とズレていても、ここで実際のサイズに自動的に合わせ直すため、
        /// 「横幅が食い違って縞模様に見える」バグが起きなくなる。
        /// </summary>
        public void ApplySnapshot(byte[] snapshot, int widthPx, int heightPx)
        {
            if (widthPx != _currentWidthPx || heightPx != _currentHeightPx)
            {
                Debug.LogWarning($"[RemoteBoardView] 受信した盤面サイズ({widthPx}x{heightPx})が現在の設定({_currentWidthPx}x{_currentHeightPx})と異なるため、表示側を自動的に作り直しました。"
                    + "Inspectorの Grid Width/Height In Blocks・Block Size を送信側(BoardView)と揃えることをおすすめします。");
                CreateRenderer(widthPx, heightPx);
            }

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

        /// <summary>
        /// PlayerNetworkSync.Render から、相手のネクストミノを渡してもらう想定。
        /// null なら「まだ届いていない」という意味。
        /// </summary>
        public void ApplyNextPiece(FallingPiece piece)
        {
            _latestNextPiece = piece;
        }

        void Update()
        {
            if (_latestSnapshot == null || _renderer == null) return;

            _renderer.DrawFromSnapshot(_latestSnapshot, TetrominoShapes.Colors);
            _renderer.DrawPiece(_latestPiece);
            _renderer.Upload();

            _previewRenderer?.Render(_latestNextPiece);
        }
    }
}