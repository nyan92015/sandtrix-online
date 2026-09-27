using UnityEngine;
using UnityEngine.UI;

namespace SandTetris
{
    /// <summary>
    /// MVPのView。やることは3つだけ:
    /// 1. Unityの入力を拾って BoardPresenter に伝える
    /// 2. 毎フレーム BoardPresenter.Tick を呼ぶ
    /// 3. BoardModel の状態を BoardRenderer で描画する
    ///
    /// ゲームのルールやタイミングの判断はここには一切書かない(それはPresenter/Modelの仕事)。
    /// SEや画面演出も、各Observerクラス(LandingFlashEffect等)に委譲するだけにする。
    /// </summary>
    public class BoardView : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] int gridWidthInBlocks = 10;
        [SerializeField] int gridHeightInBlocks = 20;
        [SerializeField] int blockSize = 6;

        [Header("Timing (秒)")]
        [SerializeField] float fallInterval = 0.6f;
        [SerializeField] float softDropInterval = 0.04f;
        [SerializeField] float moveRepeatInterval = 0.08f;
        [SerializeField] float gravityInterval = 0.03f;
        [Range(0f, 1f)]
        [SerializeField] float diagonalMoveChance = 0.35f;
        [Range(0f, 1f)]
        [SerializeField] float fallMoveChance = 0.85f;
        [SerializeField] float clearFlashDuration = 0.2f;
        [SerializeField] float landingFreezeDuration = 0.06f;
        [SerializeField] float lineClearFreezeDuration = 0.08f;

        [Header("Rendering")]
        [SerializeField] RawImage displayImage;
        [SerializeField] Color32 backgroundColor = new Color32(18, 18, 24, 255);
        [SerializeField] Color32 lineClearFlashColor = new Color32(255, 255, 255, 255);

        [Header("Landing Impact (着地演出)")]
        [SerializeField] Color32 landingFlashColor = new Color32(255, 255, 255, 255);
        [SerializeField] float landingFlashDuration = 0.12f;
        [SerializeField] float landingShakeDuration = 0.15f;
        [SerializeField] float landingShakeMagnitude = 8f;

        [Header("Sound Effects")]
        [SerializeField] AudioClip landSound;
        [SerializeField] AudioClip rotateSound;
        [SerializeField] AudioClip lineClearSound;
        [Range(0f, 1f)]
        [SerializeField] float sfxVolume = 1f;

        BoardModel _model;
        BoardPresenter _presenter;
        BoardRenderer _renderer;
        LandingFlashEffect _landingFlash;
        ScreenShakeEffect _screenShake;
        AudioObserver _audio;

        /// <summary>自分のBoardModel。PlayerNetworkSyncが送信元データとして参照する。</summary>
        public BoardModel Model => _model;
        public int BlockSize => blockSize;

        void Start()
        {
            int widthPx = gridWidthInBlocks * blockSize;
            int heightPx = gridHeightInBlocks * blockSize;
            var spawnAnchor = new Vector2Int(widthPx / 2, blockSize * 2);

            _model = new BoardModel(widthPx, heightPx, blockSize, spawnAnchor);

            var config = new BoardPresenterConfig
            {
                FallInterval = fallInterval,
                SoftDropInterval = softDropInterval,
                MoveRepeatInterval = moveRepeatInterval,
                GravityInterval = gravityInterval,
                DiagonalMoveChance = diagonalMoveChance,
                FallMoveChance = fallMoveChance,
                ClearFlashDuration = clearFlashDuration,
                LandingFreezeDuration = landingFreezeDuration,
                LineClearFreezeDuration = lineClearFreezeDuration,
            };
            _presenter = new BoardPresenter(_model, config);

            _renderer = new BoardRenderer(widthPx, heightPx) { BackgroundColor = backgroundColor };
            if (displayImage != null) displayImage.texture = _renderer.Texture;

            _landingFlash = new LandingFlashEffect(_model, landingFlashDuration) { FlashColor = landingFlashColor };

            RectTransform shakeTarget = displayImage != null ? displayImage.rectTransform : null;
            _screenShake = new ScreenShakeEffect(_model, shakeTarget, landingShakeDuration, landingShakeMagnitude);

            var audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            _audio = new AudioObserver(_model, audioSource)
            {
                LandSound = landSound,
                RotateSound = rotateSound,
                LineClearSound = lineClearSound,
                Volume = sfxVolume,
            };
        }

        void Update()
        {
            ReadInput();

            float dt = Time.deltaTime;
            _presenter.Tick(dt);
            _landingFlash.Tick(dt);
            _screenShake.Tick(dt);

            Render();
        }

        void ReadInput()
        {
            int dir = 0;
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) dir = -1;
            else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) dir = 1;

            bool moveKeyDown = Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow) ||
                                Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D);
            _presenter.SetMoveInput(dir, moveKeyDown);

            bool rotate = Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W);
            _presenter.SetRotateInput(rotate);

            bool softDrop = Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S);
            _presenter.SetSoftDrop(softDrop);
        }

        void Render()
        {
            _renderer.DrawBoard(_model);

            // ライン消去中(光っている間)はハイライトを重ねる。
            // 操作中ミノの描画は DrawBoard 側が常に行うので、ここで分岐させる必要はない。
            if (_presenter.CurrentState is LineClearState)
            {
                _renderer.DrawHighlight(_model.Grid.LastClearedIndices, lineClearFlashColor);
            }

            _landingFlash.ApplyOverlay(_renderer.Buffer, _model.Grid);

            _renderer.Upload();
        }
    }
}