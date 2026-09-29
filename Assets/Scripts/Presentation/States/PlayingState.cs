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

            // 左右移動(リピート付き)。リピート間隔もライン数に応じて短くなる(落下と同じ倍率)。
            // 落下と同じく、フレームレートが低くても間隔通りの速さで動くよう、
            // 「1フレームにつき1回まで」ではなく、溜まった時間の分だけまとめて動かす。
            if (_presenter.MoveDirection != 0)
            {
                float moveInterval = _presenter.Score.ScaleInterval(cfg.MoveRepeatInterval);
                if (moveInterval < 0.001f) moveInterval = 0.001f; // 0以下だと下のwhileが終わらなくなるので下限を設ける

                if (_presenter.MoveKeyDownThisFrame)
                {
                    // 押した瞬間は必ず1回動き、次のリピートまでの待ち時間を始める
                    model.TryMove(_presenter.MoveDirection);
                    _presenter.MoveTimer = moveInterval;
                }
                else
                {
                    _presenter.MoveTimer -= deltaTime;
                    while (_presenter.MoveTimer <= 0f)
                    {
                        model.TryMove(_presenter.MoveDirection);
                        _presenter.MoveTimer += moveInterval; // 0に戻さず加算するので、はみ出し分が次に持ち越される
                    }
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
            if (_presenter.SoftDropHeld)
            {
                // ソフトドロップを押している間は、その時間に応じて少しずつ得点が入る
                _presenter.Score.AddSoftDropTime(deltaTime);
            }

            // 通常の落下間隔(ライン数に応じて加速済み)。ソフトドロップ中は、これを「通常の落下のN倍の速さ」に縮める。
            // ソフトドロップ専用の間隔は持たず、通常の落下に対する倍率だけを固定しているので、
            // 通常の落下が加速すれば、ソフトドロップも同じ割合で一緒に速くなる。
            float fallInterval = _presenter.Score.ScaleInterval(cfg.FallInterval);
            if (_presenter.SoftDropHeld)
            {
                float softDropMultiplier = cfg.SoftDropSpeedMultiplier;
                if (softDropMultiplier < 0.01f) softDropMultiplier = 0.01f; // 0以下だと割り算が壊れるので下限を設ける
                fallInterval /= softDropMultiplier;
            }
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
                model.SimulatePhysicsStep(gravityInterval);
            }

            // ライン消去チェック
            if (model.CheckForLineClear())
            {
                _presenter.TransitionTo(new LineClearState(_presenter, cfg.LineClearFreezeDuration, cfg.ClearFlashDuration));
            }
        }
    }
}