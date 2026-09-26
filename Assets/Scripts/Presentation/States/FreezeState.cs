namespace SandTetris
{
    /// <summary>
    /// 着地の瞬間、指定時間だけ入力も物理も完全に停止させるステート。
    /// 演出(フラッシュ・画面シェイク)はこのステートとは無関係に、
    /// Observer側が自分自身のタイマーで動かし続ける(ゲーム進行だけを止める)。
    /// 時間が経過したら PlayingState に戻る。
    /// </summary>
    public class FreezeState : IBoardState
    {
        readonly BoardPresenter _presenter;
        readonly float _duration;
        float _timer;

        public FreezeState(BoardPresenter presenter, float duration)
        {
            _presenter = presenter;
            _duration = duration;
        }

        public void Enter()
        {
            _timer = _duration;
        }

        public void Update(float deltaTime)
        {
            _timer -= deltaTime;
            if (_timer <= 0f)
            {
                _presenter.TransitionTo(new PlayingState(_presenter));
            }
        }

        public void Exit() { }
    }
}