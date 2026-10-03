using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace SandTetris
{
    /// <summary>
    /// 対戦シーンに置く、勝敗の確定と、その後タイトルへ戻る流れを管理するコントローラー。
    ///
    /// 勝敗が決まる条件:
    /// - 自分の負け: 自分の BoardModel.IsGameOver が true になった
    ///   (盤面の上端からのはみ出し、または自分の地面が盤面全体に達した。どちらも既存のロジックが
    ///   すでに IsGameOver に集約しているので、ここでは1つを見るだけでいい)
    /// - 自分の勝ち: 相手の IsGameOver を検知(PlayerNetworkSync経由)、
    ///   または相手が切断・退出した(MatchmakingLauncher経由)
    ///
    /// 決着したら、対戦画面の上に WIN/LOSE を重ねて表示し、盤面を一時停止したまま、
    /// 一定時間後に自動でタイトルシーンへ戻る。
    /// </summary>
    public class MatchResultController : MonoBehaviour
    {
        [SerializeField] BoardView boardView;

        [Header("UI")]
        [SerializeField] GameObject resultPanel;
        [SerializeField] TMP_Text resultText;
        [SerializeField] string winMessage = "WIN";
        [SerializeField] string loseMessage = "LOSE";

        [Header("Timing")]
        [SerializeField] float returnToTitleDelaySeconds = 3f;
        [SerializeField] int titleSceneBuildIndex = 0;

        bool _decided;
        float _returnTimer;

        void Start()
        {
            if (resultPanel != null) resultPanel.SetActive(false);
        }

        void Update()
        {
            if (!_decided)
            {
                // BoardView.Start() が自分より後に実行される可能性があるため、
                // イベント購読ではなく、ここで毎フレーム安全に確認する(ポーリング)。
                if (boardView != null && boardView.Model != null && boardView.Model.IsGameOver)
                {
                    Decide(won: false);
                }
                return;
            }

            _returnTimer -= Time.deltaTime;
            if (_returnTimer <= 0f)
            {
                ReturnToTitle();
            }
        }

        /// <summary>PlayerNetworkSync から、相手のゲームオーバーを検知したときに呼ばれる。</summary>
        public void NotifyOpponentGameOver()
        {
            Decide(won: true);
        }

        /// <summary>MatchmakingLauncher から、相手が退出したときに呼ばれる。</summary>
        public void NotifyOpponentLeft()
        {
            Decide(won: true);
        }

        void Decide(bool won)
        {
            if (_decided) return;
            _decided = true;

            if (boardView != null) boardView.SetPaused(true);

            if (resultPanel != null) resultPanel.SetActive(true);
            if (resultText != null) resultText.text = won ? winMessage : loseMessage;

            _returnTimer = returnToTitleDelaySeconds;
        }

        void ReturnToTitle()
        {
            // MatchmakingLauncher はタイトルシーンから DontDestroyOnLoad で生き残ってきたオブジェクトなので、
            // 対戦シーンの編集時点では存在せず、Inspectorでのドラッグ紐付けができない。
            // そのため実行時に検索する。
            var matchmakingLauncher = FindFirstObjectByType<MatchmakingLauncher>();
            if (matchmakingLauncher != null) matchmakingLauncher.ShutdownAndDestroy();

            SceneManager.LoadScene(titleSceneBuildIndex);
        }
    }
}