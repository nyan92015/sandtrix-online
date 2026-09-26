namespace SandTetris
{
    /// <summary>
    /// 通常プレイ中のステート。入力を受けて移動・回転、落下タイマー、砂物理シミュレーションを進める。
    /// 着地したら FreezeState へ、ラインが見つかったら LineClearState へ遷移する。
    /// </summary>
    public class PlayingState : IBoardState
    {
        readonly BoardPresenter _presenter;

        public PlayingState(BoardPresenter presenter)
        {
            _presenter = presenter;
        }

        public void Enter() { }
        public void Exit() { }

        public void Update(float deltaTime)
        {
            var model = _presenter.Model;
            var cfg = _presenter.Config;

            // 左右移動(リピート付き)
            if (_presenter.MoveDirection != 0)
            {
                _presenter.MoveTimer -= deltaTime;
                if (_presenter.MoveKeyDownThisFrame || _presenter.MoveTimer <= 0f)
                {
                    _presenter.MoveTimer = cfg.MoveRepeatInterval;
                    model.TryMove(_presenter.MoveDirection);
                }
            }

            // 回転
            if (_presenter.RotatePressedThisFrame)
            {
                model.TryRotate(1);
            }

            // 落下
            float fallInterval = _presenter.SoftDropHeld ? cfg.SoftDropInterval : cfg.FallInterval;
            _presenter.FallTimer += deltaTime;
            if (_presenter.FallTimer >= fallInterval)
            {
                _presenter.FallTimer = 0f;
                if (!model.TryStepDown())
                {
                    model.LockCurrentPiece();
                    _presenter.TransitionTo(new FreezeState(_presenter, cfg.LandingFreezeDuration));
                    return;
                }
            }

            // 砂の物理シミュレーション(実時間の経過分だけまとめて進める)
            _presenter.GravityTimer += deltaTime;
            while (_presenter.GravityTimer >= cfg.GravityInterval)
            {
                _presenter.GravityTimer -= cfg.GravityInterval;
                model.SimulatePhysicsStep();
            }

            // ライン消去チェック
            if (model.CheckForLineClear())
            {
                _presenter.TransitionTo(new LineClearState(_presenter, cfg.LineClearFreezeDuration, cfg.ClearFlashDuration));
            }
        }
    }
}