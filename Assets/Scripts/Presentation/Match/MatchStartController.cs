using System.Linq;
using Fusion;
using UnityEngine;
using TMPro;

namespace SandTetris
{
    /// <summary>
    /// 対戦シーンに置く、試合開始までの流れを管理するコントローラー。
    ///
    /// 流れ:
    /// 1. シーンに入った直後は、自分の盤面(BoardView)を一時停止しておく(見えるが動かない)
    /// 2. 相手(2人目のプレイヤー)が揃うまで「対戦相手を待っています...」を表示
    /// 3. 揃ったら、3・2・1のカウントダウンを表示する(ローカルで数えるだけ。相手と厳密に同期はしない)
    /// 4. カウントダウンが終わったら、表示を消して盤面の一時停止を解除する
    ///
    /// カウントダウンをネットワークで厳密に同期していないのは、意図的な割り切り。
    /// 「2人揃った」タイミングはほぼ同時に両者へ届くので、体感のズレはごくわずかで済む。
    /// </summary>
    public class MatchStartController : MonoBehaviour
    {
        [SerializeField] BoardView boardView;

        [Header("UI")]
        [SerializeField] GameObject waitingPanel;
        [SerializeField] GameObject countdownPanel;
        [SerializeField] TMP_Text countdownText;

        [Header("Timing")]
        [SerializeField] float countdownSeconds = 3f;

        enum Phase { WaitingForOpponent, CountingDown, Playing }
        Phase _phase = Phase.WaitingForOpponent;

        float _countdownRemaining;
        int _lastShownNumber = -1;

        void Start()
        {
            if (DebugMode.Enabled)
            {
                // デバッグプレイ: 相手を待たず、カウントダウンもせず、すぐに操作できるようにする
                _phase = Phase.Playing;
                SetPanels(waiting: false, countdown: false);
                if (boardView != null) boardView.SetPaused(false);
                return;
            }

            if (boardView != null) boardView.SetPaused(true);
            SetPanels(waiting: true, countdown: false);
        }

        void Update()
        {
            switch (_phase)
            {
                case Phase.WaitingForOpponent:
                    if (GetActivePlayerCount() >= 2)
                    {
                        StartCountdown();
                    }
                    break;

                case Phase.CountingDown:
                    _countdownRemaining -= Time.deltaTime;
                    UpdateCountdownDisplay();
                    if (_countdownRemaining <= 0f)
                    {
                        FinishCountdown();
                    }
                    break;

                case Phase.Playing:
                    break;
            }
        }

        int GetActivePlayerCount()
        {
            var runner = FindFirstObjectByType<NetworkRunner>();
            if (runner == null || !runner.IsRunning) return 0;
            return runner.ActivePlayers.Count();
        }

        void StartCountdown()
        {
            _phase = Phase.CountingDown;
            _countdownRemaining = countdownSeconds;
            _lastShownNumber = -1;
            SetPanels(waiting: false, countdown: true);
            UpdateCountdownDisplay();
        }

        void UpdateCountdownDisplay()
        {
            if (countdownText == null) return;

            int number = Mathf.CeilToInt(Mathf.Max(_countdownRemaining, 0f));
            if (number == _lastShownNumber) return;

            _lastShownNumber = number;
            countdownText.text = number > 0 ? number.ToString() : "START";
        }

        void FinishCountdown()
        {
            _phase = Phase.Playing;
            SetPanels(waiting: false, countdown: false);
            if (boardView != null) boardView.SetPaused(false);
        }

        void SetPanels(bool waiting, bool countdown)
        {
            if (waitingPanel != null) waitingPanel.SetActive(waiting);
            if (countdownPanel != null) countdownPanel.SetActive(countdown);
        }
    }
}