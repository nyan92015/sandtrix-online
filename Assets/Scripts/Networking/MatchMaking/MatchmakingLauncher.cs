using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SandTetris
{
    /// <summary>
    /// タイトルシーンに置く、クイックマッチ開始用のスクリプト。
    ///
    /// 「クイックマッチ」ボタンを押すと、Fusionに SessionName を指定せずに接続する。
    /// Shared Modeでこれをやると、Fusionが「空いているセッションがあれば参加、無ければ新規作成」を
    /// 自動的にやってくれる(公式ドキュメントに明記されている、ランダムマッチングの標準的なやり方)。
    ///
    /// 接続と同時に、StartGameArgs.Scene で対戦シーンを指定しているので、
    /// 接続が成立した瞬間、Fusionが自動的に対戦シーンを読み込んでくれる
    /// (「メニューシーンから対戦シーンへ移る」という、Fusionが公式に想定している使い方)。
    ///
    /// このオブジェクト自身は DontDestroyOnLoad で残しておく。理由は、
    /// NetworkRunner はシーン読み込みをまたいで生き続ける必要があり、
    /// 対戦シーン側でプレイヤーオブジェクトを生成する処理(OnPlayerJoined)も、
    /// このスクリプトが引き続き担当するため。
    ///
    /// INetworkRunnerCallbacks は Fusion の仕様上、MonoBehaviour自身が実装する必要があるが、
    /// 実際の接続・プレイヤー生成の中身は FusionMatchConnector に委譲している。
    /// </summary>
    public class MatchmakingLauncher : MonoBehaviour, INetworkRunnerCallbacks
    {
        [Tooltip("対戦シーンのビルドインデックス(File > Build Settings の並び順)")]
        [SerializeField] int battleSceneBuildIndex = 1;

        [Tooltip("PlayerNetworkSync + NetworkObject をアタッチしたプレハブ")]
        [SerializeField] NetworkPrefabRef playerPrefab;

        [Header("UI")]
        [SerializeField] Button quickMatchButton;
        [SerializeField] Button debugPlayButton;
        [SerializeField] TMP_Text statusText;

        FusionMatchConnector _connector;

        void Start()
        {
            _connector = new FusionMatchConnector(this, playerPrefab, battleSceneBuildIndex);

            if (quickMatchButton != null)
            {
                quickMatchButton.onClick.AddListener(OnQuickMatchClicked);
            }
            if (debugPlayButton != null)
            {
                debugPlayButton.onClick.AddListener(OnDebugPlayClicked);
            }
        }

        async void OnQuickMatchClicked()
        {
            if (_connector.Runner != null) return; // 二重クリック防止

            // 前回デバッグプレイを使っていた場合に備えて、必ず通常のフラグへ戻しておく
            DebugMode.Enabled = false;

            if (quickMatchButton != null) quickMatchButton.interactable = false;
            SetStatus("マッチング中...");

            // シーンをまたいで生き続ける必要があるので、接続を始める前にここで保護する
            DontDestroyOnLoad(gameObject);

            var (ok, errorMessage) = await _connector.Connect();
            if (!ok)
            {
                SetStatus(errorMessage);
                if (quickMatchButton != null) quickMatchButton.interactable = true;
            }
            // 成功時は、このあとFusionが対戦シーンを自動的に読み込むので、ここでは何もしない
        }

        /// <summary>
        /// デバッグプレイ: ネットワークに一切繋がず、対戦シーンを直接読み込む。
        /// 相手がいないので、対戦シーン側(MatchStartController)が
        /// DebugMode.Enabled を見て、相手待ち・カウントダウンを飛ばして即座に始める。
        /// NetworkRunner を作らないので、このオブジェクト自体はシーンをまたぐ必要がなく、
        /// DontDestroyOnLoad も不要(対戦シーンへ移った時点で、普通に破棄されて構わない)。
        /// </summary>
        void OnDebugPlayClicked()
        {
            DebugMode.Enabled = true;
            UnityEngine.SceneManagement.SceneManager.LoadScene(battleSceneBuildIndex);
        }

        void SetStatus(string message)
        {
            if (statusText != null) statusText.text = message;
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) => _connector.HandlePlayerJoined(runner, player);
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) => _connector.HandlePlayerLeft(runner, player);

        /// <summary>
        /// タイトルへ戻るときに、MatchResultController から呼んでもらう。
        /// 接続を切って、このオブジェクト自体も破棄する(新しいタイトルシーンで、
        /// まっさらな MatchmakingLauncher が改めて生成されるようにするため)。
        /// </summary>
        public void ShutdownAndDestroy()
        {
            _connector.Shutdown();
            Destroy(gameObject);
        }

        // ↓ここから下は INetworkRunnerCallbacks が要求する残りのメソッド。中身は空でOK。
        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    }
}