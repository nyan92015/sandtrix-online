namespace SandTetris
{
    /// <summary>
    /// MVPのPresenter。BoardModelを保持し、ステートマシンを介して
    /// 「今何をすべきか」を管理する。Unity非依存(MonoBehaviourを継承しない)。
    ///
    /// View側(MonoBehaviour)は、毎フレーム Tick を呼び、その前に入力の状態を
    /// SetMoveInput / SetRotateInput / SetSoftDrop で伝える、という使い方を想定する。
    /// 描画やSEはBoardModelのイベントを直接購読する別のObserverが行うため、
    /// Presenter自身は「いつ・何をすべきか」の管理に専念する。
    /// </summary>
    public class BoardPresenter
    {
        public BoardModel Model { get; }
        public BoardPresenterConfig Config { get; }
        public IBoardState CurrentState { get; private set; }

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

            Model.Grid.DiagonalMoveChance = config.DiagonalMoveChance;
            Model.Grid.FallMoveChance = config.FallMoveChance;

            Model.OnGameOver += HandleGameOver;

            TransitionTo(new PlayingState(this));
        }

        public void TransitionTo(IBoardState next)
        {
            CurrentState?.Exit();
            CurrentState = next;
            CurrentState.Enter();
        }

        /// <summary>毎フレーム呼ぶ。現在のステートの更新処理を1回進める。</summary>
        public void Tick(float deltaTime)
        {
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