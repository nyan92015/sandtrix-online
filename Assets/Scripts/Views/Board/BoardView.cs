using System.Collections.Generic;
using UnityEngine;

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
    ///
    /// Inspector項目は、関連するものごとに[System.Serializable]な設定クラスへまとめてある
    /// (Views/Board/Settings/ 以下)。
    /// </summary>
    public class BoardView : MonoBehaviour
    {
        [SerializeField] GridSettings grid;
        [SerializeField] TimingSettings timing;
        [SerializeField] RenderingSettings rendering;
        [SerializeField] NextPiecePreviewSettings nextPiecePreview;
        [SerializeField] LandingImpactSettings landingImpact;
        [SerializeField] SoundSettings sound;
        [SerializeField] SeesawGroundSettings seesawGround;
        [SerializeField] SpecialPieceSettings specialPiece;
        [SerializeField] InfectionSweepSettings infectionSweep;
        [SerializeField] ScoreUISettings scoreUI;
        [SerializeField] FeverGaugeSettings feverGauge;

        BoardModel _model;
        BoardPresenter _presenter;
        BoardRenderer _renderer;
        NextPiecePreviewRenderer _previewRenderer;
        LandingFlashEffect _landingFlash;
        LineClearSweepFlashEffect _lineClearSweep;
        InfectionSweepEffect _infectionSweepEffect;
        ScreenShakeEffect _screenShake;
        AudioObserver _audio;
        ScoreUIObserver _scoreUIObserver;
        FeverGaugeUI _feverGaugeUI;
        BoardInputReader _inputReader;
        BoardNetworkOverlayProvider _overlayProvider;
        BoardRenderOrchestrator _renderOrchestrator;

        /// <summary>自分のBoardModel。PlayerNetworkSyncが送信元データとして参照する。</summary>
        public BoardModel Model => _model;
        public int BlockSize => grid.blockSize;
        public ScoreTracker Score => _presenter?.Score;
        public GroundLevelController Ground => _presenter?.Ground;

        /// <summary>
        /// 今まさに白く見えているセルのインデックス一覧。PlayerNetworkSync が、
        /// 相手へ送るスナップショットに反映するために使う。
        /// </summary>
        public HashSet<int> GetCurrentWhiteIndices() => _overlayProvider.GetCurrentWhiteIndices();

        /// <summary>
        /// 今まさに「もう消えた(背景色)」ように見えているセルのインデックス一覧。
        /// </summary>
        public HashSet<int> GetCurrentGoneIndices() => _overlayProvider.GetCurrentGoneIndices();

        /// <summary>
        /// 相手の合計スコアを伝える。PlayerNetworkSync が、ネットワークで受け取った値を毎フレーム渡す。
        /// これをもとにシーソー式の地面の高さが決まる。
        /// </summary>
        public void SetOpponentScore(int opponentTotalScore)
        {
            _presenter?.Ground.SetOpponentScore(opponentTotalScore);
        }

        void Start()
        {
            int widthPx = grid.widthInBlocks * grid.blockSize;
            int heightPx = grid.heightInBlocks * grid.blockSize;
            // 盤面の上端(y=0)よりさらに上、完全に見えない位置から出現させる。
            // -blockSize*2 は、今の形状(最大でも縦3ブロック)なら確実に画面外に収まる余裕を持たせた値。
            var spawnAnchor = new Vector2Int(widthPx / 2, -grid.blockSize * 2);

            FallingPiece.NormalBrightenAmount = specialPiece.normalBrightenAmount;

            _model = new BoardModel(widthPx, heightPx, grid.blockSize, spawnAnchor);

            var config = new BoardPresenterConfig
            {
                NormalBrightenAmount = specialPiece.normalBrightenAmount,
                FeverDuration = feverGauge.duration,
                FallInterval = timing.fallInterval,
                SoftDropBonusSpeedIdlePxPerSec = timing.softDropBonusSpeedIdlePxPerSec,
                SoftDropBonusSpeedMovingPxPerSec = timing.softDropBonusSpeedMovingPxPerSec,
                MoveRepeatInterval = timing.moveRepeatInterval,
                GravityInterval = timing.gravityInterval,
                AshGravityInterval = timing.ashGravityInterval,
                AshLifetimeSeconds = timing.ashLifetimeSeconds,
                MinGravityInterval = timing.minGravityInterval,
                DiagonalMoveChance = timing.diagonalMoveChance,
                FallMoveChance = timing.fallMoveChance,
                ClearFlashDuration = timing.clearFlashDuration,
                LandingFreezeDuration = timing.landingFreezeDuration,
                LineClearFreezeDuration = timing.lineClearFreezeDuration,
                BlockSize = grid.blockSize,
                SoftDropPointsPerSecond = timing.softDropPointsPerSecond,
                ContestWindowSeconds = seesawGround.contestWindowSeconds,
                ContestThreshold = seesawGround.contestThreshold,
                GroundRiseSeconds = seesawGround.groundRiseSeconds,
                GroundPushBaseCost = seesawGround.groundPushBaseCost,
                GroundColor = seesawGround.groundColor,
            };
            _presenter = new BoardPresenter(_model, config);

            _renderer = new BoardRenderer(widthPx, heightPx, seesawGround.frameThicknessPx)
            {
                BackgroundColor = rendering.backgroundColor,
                ConcreteColor = seesawGround.groundColor,
                AshColor = seesawGround.groundColor,
                FrameColor = seesawGround.frameColor,
                WarningColor = seesawGround.warningColor,
                SpecialPieceColors = new[] { specialPiece.colorRed, specialPiece.colorBlue, specialPiece.colorYellow, specialPiece.colorGreen },
                SpecialPieceSaturationBoost = specialPiece.saturationBoost,
                SpecialPieceEdgeColor = specialPiece.edgeColor,
                SpecialPieceEdgeMinBrightness = specialPiece.edgeMinBrightness,
                SpecialPieceEdgeMaxBrightness = specialPiece.edgeMaxBrightness,
                SpecialPiecePulseSpeed = specialPiece.pulseSpeed,
                SpecialPieceBrightBias = specialPiece.brightBias,
            };
            if (rendering.displayImage != null) rendering.displayImage.texture = _renderer.Texture;

            _previewRenderer = new NextPiecePreviewRenderer(grid.blockSize * 4) { BackgroundColor = nextPiecePreview.backgroundColor };
            if (nextPiecePreview.displayImage != null) nextPiecePreview.displayImage.texture = _previewRenderer.Texture;

            _landingFlash = new LandingFlashEffect(_model, landingImpact.flashDuration) { FlashColor = landingImpact.flashColor };
            _lineClearSweep = new LineClearSweepFlashEffect(_model);
            _infectionSweepEffect = new InfectionSweepEffect(_model)
            {
                FlashSpeed = infectionSweep.flashSpeed,
                FlashMaxBlend = infectionSweep.flashMaxBlend,
                RingWidth = infectionSweep.ringWidth,
            };

            // 感染が起きた瞬間にも、着地のときと同じ画面の揺れを流用してトリガーする(「ぐわん」とした衝撃)
            _model.OnInfection += (_, __, ___) => _screenShake.Trigger();

            RectTransform shakeTarget = rendering.displayImage != null ? rendering.displayImage.rectTransform : null;
            _screenShake = new ScreenShakeEffect(_model, shakeTarget, landingImpact.shakeDuration, landingImpact.shakeMagnitude);

            var audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            _audio = new AudioObserver(_model, audioSource)
            {
                LandSound = sound.landSound,
                RotateSound = sound.rotateSound,
                LineClearSound = sound.lineClearSound,
                Volume = sound.volume,
            };

            _scoreUIObserver = new ScoreUIObserver(_presenter.Score, scoreUI.scoreText, scoreUI.multiplierText, scoreUI.linesClearedText, scoreUI.bucketGaugeSlider);

            _feverGaugeUI = new FeverGaugeUI(
                feverGauge.lampImages,
                new[] { feverGauge.spriteRed, feverGauge.spriteBlue, feverGauge.spriteYellow, feverGauge.spriteGreen },
                feverGauge.spriteUnlit);
            _feverGaugeUI.Bind(_model.Fever);

            _inputReader = new BoardInputReader(_presenter);
            _overlayProvider = new BoardNetworkOverlayProvider(_model, _presenter, _landingFlash, _lineClearSweep, _infectionSweepEffect);
            _renderOrchestrator = new BoardRenderOrchestrator(_model, _presenter, _renderer, _previewRenderer, _landingFlash, _lineClearSweep, _infectionSweepEffect, grid.blockSize);
        }

        /// <summary>
        /// true の間、入力・落下・物理・スコア・演出のすべてを止める(盤面の見た目はそのまま残す)。
        /// マッチング直後のカウントダウン中など、「見えてはいるが、まだ始まっていない」状態に使う。
        /// </summary>
        public bool IsPaused { get; private set; }

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
        }

        void Update()
        {
            float dt = Time.deltaTime;

            if (!IsPaused)
            {
                _inputReader.ReadInput();

                _presenter.Tick(dt);
                _landingFlash.Tick(dt);
                _infectionSweepEffect.Tick(dt);
                _screenShake.Tick(dt);
                _scoreUIObserver.Tick();
                _feverGaugeUI.Tick();
            }

            // デバッグプレイ中だけ、Gキーで今操作中のミノを特別なミノに差し替える。
            // 特別なミノの演出確認用。デバッグモードに限らず、オンライン対戦中でも押せるようにしてある。
            if (Input.GetKeyDown(KeyCode.G))
            {
                _model.DebugReplaceCurrentPieceWithSpecial();
            }

            _renderOrchestrator.Render();
        }
    }
}