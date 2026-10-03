using System.Collections.Generic;

namespace SandTetris
{
    /// <summary>
    /// 今まさに「白く見えている」「もう消えたように見えている」セルの一覧を、
    /// 複数の演出(着地・ライン消去・感染)からまとめて集める。
    /// PlayerNetworkSync が、相手へ送るスナップショットに反映するために使う。
    /// </summary>
    public class BoardNetworkOverlayProvider
    {
        readonly BoardModel _model;
        readonly BoardPresenter _presenter;
        readonly LandingFlashEffect _landingFlash;
        readonly LineClearSweepFlashEffect _lineClearSweep;
        readonly InfectionSweepEffect _infectionSweep;

        public BoardNetworkOverlayProvider(
            BoardModel model,
            BoardPresenter presenter,
            LandingFlashEffect landingFlash,
            LineClearSweepFlashEffect lineClearSweep,
            InfectionSweepEffect infectionSweep)
        {
            _model = model;
            _presenter = presenter;
            _landingFlash = landingFlash;
            _lineClearSweep = lineClearSweep;
            _infectionSweep = infectionSweep;
        }

        public HashSet<int> GetCurrentWhiteIndices()
        {
            var result = new HashSet<int>();
            if (_model == null) return result;

            foreach (var p in _landingFlash.GetCurrentlyWhitePositions())
            {
                if (_model.Grid.InBounds(p.x, p.y)) result.Add(_model.Grid.Index(p.x, p.y));
            }

            if (_presenter.CurrentState is LineClearState lineClearState)
            {
                foreach (var idx in _lineClearSweep.GetCurrentlyWhiteIndices(lineClearState.TotalElapsed, lineClearState.TotalDuration))
                {
                    result.Add(idx);
                }
            }

            foreach (var idx in _infectionSweep.GetCurrentlyWhiteIndices())
            {
                result.Add(idx);
            }

            return result;
        }

        /// <summary>
        /// 本物のデータはまだ消去されていなくても、相手側のスナップショットにはこれを「空」として
        /// 送ってもらう必要がある(ライン消去の「点滅しながら左から右へ消える」演出用)。
        /// </summary>
        public HashSet<int> GetCurrentGoneIndices()
        {
            var result = new HashSet<int>();
            if (_model == null) return result;

            if (_presenter.CurrentState is LineClearState lineClearState)
            {
                foreach (var idx in _lineClearSweep.GetCurrentlyGoneIndices(lineClearState.TotalElapsed, lineClearState.TotalDuration))
                {
                    result.Add(idx);
                }
            }

            return result;
        }
    }
}