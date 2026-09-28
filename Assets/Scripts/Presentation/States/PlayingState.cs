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

            // 落下(SpeedMultiplierが上がるほど間隔が短くなり、速く落ちるようになる。上限なし)
            // whileにしているのは、フレームレートが低い環境で「1フレームにつき1マスまで」という
            // 隠れた制限がかかってしまうのを防ぐため(低フレームレートだと1フレームの経過時間が
            // fallIntervalの何倍にもなりうるので、その分をまとめて処理する必要がある)。
            float baseFallInterval = _presenter.SoftDropHeld ? cfg.SoftDropInterval : cfg.FallInterval;
            float fallInterval = _presenter.Score.ScaleFallInterval(baseFallInterval);
            _presenter.FallTimer += deltaTime;
            while (_presenter.FallTimer >= fallInterval)
            {
                _presenter.FallTimer -= fallInterval;
                if (!model.TryStepDown())
                {
                    model.LockCurrentPiece();
                    _presenter.TransitionTo(new FreezeState(_presenter, cfg.LandingFreezeDuration));
                    return;
                }
            }

            // 砂の物理シミュレーション(こちらは MinGravityInterval で下限を設け、無限に重くならないようにする)
            float gravityInterval = _presenter.Score.ScaleGravityInterval(cfg.GravityInterval, cfg.MinGravityInterval);
            _presenter.GravityTimer += deltaTime;
            while (_presenter.GravityTimer >= gravityInterval)
            {
                _presenter.GravityTimer -= gravityInterval;
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