using UnityEngine;

namespace SandTetris
{
    /// <summary>
    /// 毎フレーム、盤面・各種演出をまとめて描画する手順を担当する。
    /// </summary>
    public class BoardRenderOrchestrator
    {
        readonly BoardModel _model;
        readonly BoardPresenter _presenter;
        readonly BoardRenderer _renderer;
        readonly NextPiecePreviewRenderer _previewRenderer;
        readonly LandingFlashEffect _landingFlash;
        readonly LineClearSweepFlashEffect _lineClearSweep;
        readonly InfectionSweepEffect _infectionSweep;
        readonly int _blockSize;

        public BoardRenderOrchestrator(
            BoardModel model,
            BoardPresenter presenter,
            BoardRenderer renderer,
            NextPiecePreviewRenderer previewRenderer,
            LandingFlashEffect landingFlash,
            LineClearSweepFlashEffect lineClearSweep,
            InfectionSweepEffect infectionSweep,
            int blockSize)
        {
            _model = model;
            _presenter = presenter;
            _renderer = renderer;
            _previewRenderer = previewRenderer;
            _landingFlash = landingFlash;
            _lineClearSweep = lineClearSweep;
            _infectionSweep = infectionSweep;
            _blockSize = blockSize;
        }

        public void Render()
        {
            _renderer.DrawBoard(_model);

            _landingFlash.ApplyOverlay(_renderer, _model.Grid);
            // LineClearState自身が持つ「本物の」タイマーの値を、そのまま渡す(ズレを起こさないため)
            if (_presenter.CurrentState is LineClearState lineClearState)
            {
                _lineClearSweep.ApplyOverlay(_renderer, lineClearState.TotalElapsed, lineClearState.TotalDuration);
            }
            _infectionSweep.ApplyOverlay(_renderer);

            // せめぎ合いで、自分が守備側かつ基準点を超えているときだけ、警告表示を重ねる
            float warningLevel = _presenter.Ground.PendingWarningLevel;
            if (warningLevel > 0f)
            {
                int warningHeightPx = Mathf.RoundToInt(warningLevel * _blockSize);
                _renderer.DrawGroundWarning(warningHeightPx, Time.time);
            }

            _renderer.Upload();

            _previewRenderer.Render(_model.NextPiece);
        }
    }
}