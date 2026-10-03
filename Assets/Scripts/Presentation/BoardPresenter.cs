namespace SandTetris
{

    public class BoardPresenter
    {
        public BoardModel Model { get; }
        public BoardPresenterConfig Config { get; }
        public IBoardState CurrentState { get; private set; }
        public ScoreTracker Score { get; }
        public GroundLevelController Ground { get; }

        // PlayingStateなど各ステートが読み書きするタイマー類。
        // Presenterが「箱」として保持し、実際の使い方は各ステートに委ねる。
        internal float FallTimer;
        internal float MoveTimer;
        internal float GravityTimer;

        // 直近1フレーム分の入力状態。Viewが毎フレームセットする。
        internal int MoveDirection;
        internal bool MoveKeyDownThisFrame;
        internal bool RotatePressedThisFrame;
        internal bool SoftDropHeld;

        public BoardPresenter(BoardModel model, BoardPresenterConfig config)
        {
            Model = model;
            Config = config;

            // 全てのミノの色がここを経由するので、ここで一度設定しておくだけでいい
            FallingPiece.NormalBrightenAmount = config.NormalBrightenAmount;

            Score = new ScoreTracker(model)
            {
                SoftDropPointsPerSecond = config.SoftDropPointsPerSecond,
            };

            Ground = new GroundLevelController(model.Grid, config.BlockSize)
            {
                RiseSeconds = config.GroundRiseSeconds,
                PushBaseCost = config.GroundPushBaseCost,
                GroundColor = config.GroundColor,
            };
            Ground.Contest.WindowSeconds = config.ContestWindowSeconds;
            Ground.Contest.Threshold = config.ContestThreshold;
            // 地面が盤面全体に達したら、はみ出たときと同じようにゲームオーバーにする
            Ground.OnGroundFull += Model.ForceGameOver;

            Model.Grid.DiagonalMoveChance = config.DiagonalMoveChance;
            Model.Grid.FallMoveChance = config.FallMoveChance;
            Model.Grid.AshGravityInterval = config.AshGravityInterval;
            Model.Grid.AshLifetimeSeconds = config.AshLifetimeSeconds;

            Model.OnGameOver += HandleGameOver;

            TransitionTo(new PlayingState(this));
        }

        public void TransitionTo(IBoardState next)
        {
            CurrentState?.Exit();
            CurrentState = next;
            CurrentState.Enter();
        }

        /// <summary>
        /// 毎フレーム呼ぶ。現在のステートの更新処理を1回進める。
        /// スコアの倍率減衰や地面の高さの更新は、ライン消去演出などで一時停止している間も
        /// 止めずに進める(演出は数百ミリ秒程度なので、無視できる差として扱う)。
        /// </summary>
        public void Tick(float deltaTime)
        {
            Score.Tick(deltaTime);
            Ground.Tick(deltaTime, Score.TotalScore);
            Model.Fever.Tick(deltaTime);
            CurrentState?.Update(deltaTime);
        }

        // --- 入力の受け口。Viewが毎フレーム、Tickを呼ぶ前にこれらを呼んで状態を伝える ---

        public void SetMoveInput(int direction, bool keyDownThisFrame)
        {
            MoveDirection = direction;
            MoveKeyDownThisFrame = keyDownThisFrame;
        }

        public void SetRotateInput(bool pressedThisFrame)
        {
            RotatePressedThisFrame = pressedThisFrame;
        }

        public void SetSoftDrop(bool held)
        {
            SoftDropHeld = held;
        }

        void HandleGameOver()
        {
            TransitionTo(new GameOverState());
        }
    }
}