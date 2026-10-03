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

            // 左右移動(リピート付き)。本家と同じく、ライン数による加速の対象には含めない(常に一定の速さ)。
            // フレームレートが低くても間隔通りの速さで動くよう、
            // 「1フレームにつき1回まで」ではなく、溜まった時間の分だけまとめて動かす。
            if (_presenter.MoveDirection != 0)
            {
                float moveInterval = cfg.MoveRepeatInterval;
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

            // 落下速度は「間隔(秒/ピクセル)」ではなく、いったん「速さ(ピクセル/秒)」で考える。
            // 本家がそうしているのと同じく、ソフトドロップは掛け算ではなく、
            // 「決まった速さを、そのまま足す」方式にする(加速の影響を受けない固定量)。
            float normalSpeedPxPerSec = 1f / _presenter.Score.ScaleInterval(cfg.FallInterval);
            float effectiveSpeedPxPerSec = normalSpeedPxPerSec;

            if (_presenter.SoftDropHeld)
            {
                // 左右に移動していない間はより多く、移動中はやや少なめに足す(本家の仕様)
                bool movingHorizontally = _presenter.MoveDirection != 0;
                effectiveSpeedPxPerSec += movingHorizontally
                    ? cfg.SoftDropBonusSpeedMovingPxPerSec
                    : cfg.SoftDropBonusSpeedIdlePxPerSec;
            }

            if (effectiveSpeedPxPerSec < 0.01f) effectiveSpeedPxPerSec = 0.01f; // 0以下だと割り算が壊れるので下限を設ける
            float fallInterval = 1f / effectiveSpeedPxPerSec;
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