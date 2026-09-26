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

        void Update()
        {
            if (_latestSnapshot == null || _renderer == null) return;

            _renderer.DrawShadedFromSnapshot(_latestSnapshot, TetrominoShapes.Colors);
            _renderer.Upload();
        }
    }
}