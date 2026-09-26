namespace SandTetris
{
    /// <summary>
    /// ラインが見つかった瞬間から実際に消去されるまでを管理するステート。
    /// 「一瞬静止 → 光る → 消去 → 連鎖チェック」を1つのステートの中で
    /// 小さなフェーズ(Freeze/Flash)として管理する。
    /// 連鎖していればこのステートに留まり、なければ PlayingState に戻る。
    /// </summary>
    public class LineClearState : IBoardState
    {
        enum Phase { Freeze, Flash }

        readonly BoardPresenter _presenter;
        readonly float _freezeDuration;
        readonly float _flashDuration;

        Phase _phase;
        float _timer;

        public LineClearState(BoardPresenter presenter, float freezeDuration, float flashDuration)
        {
            _presenter = presenter;
            _freezeDuration = freezeDuration;
            _flashDuration = flashDuration;
        }

        public void Enter()
        {
            _phase = Phase.Freeze;
            _timer = _freezeDuration;
        }

        public void Update(float deltaTime)
        {
            _timer -= deltaTime;
            if (_timer > 0f) return;

            if (_phase == Phase.Freeze)
            {
                _phase = Phase.Flash;
                _timer = _flashDuration;
                return;
            }

            // 光る時間が終わったので、実際に消去する
            var model = _presenter.Model;
            model.ApplyLineClear();

            if (model.CheckForLineClear())
            {
                // 連鎖: もう一度Freeze→Flashをやり直す
                _phase = Phase.Freeze;
                _timer = _freezeDuration;
            }
            else
            {
                _presenter.TransitionTo(new PlayingState(_presenter));
            }
        }

        public void Exit() { }
    }
}