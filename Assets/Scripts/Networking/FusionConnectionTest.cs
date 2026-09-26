using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SandTetris
{
    /// <summary>
    /// 【接続テスト専用】Fusionが正しくセットアップされているかを確認するための、
    /// 最小限のスクリプト。ゲームロジックとは無関係。
    ///
    /// 使い方:
    /// 1. 空のGameObjectを作り、このスクリプトをアタッチする
    /// 2. 現在のシーンを File > Build Settings > Scenes In Build に追加しておく
    ///    (SceneRef.FromIndex がビルド設定のシーン一覧を参照するため)
    /// 3. 再生すると画面左上にボタンが出る。「Host として起動」を押す
    /// 4. もう1つ別のインスタンス(ビルドしたexe、または ParrelSync などで複製した
    ///    もう1つのEditor)で同じシーンを再生し、「Client として参加」を押す
    /// 5. 両方のコンソールに「プレイヤーが参加しました」ログが出れば接続成功
    /// </summary>
    public class FusionConnectionTest : MonoBehaviour, INetworkRunnerCallbacks
    {
        const string RoomName = "sandtris-shared-room";

        [Tooltip("PlayerNetworkSync + NetworkObject をアタッチしたプレハブ")]
        [SerializeField] NetworkPrefabRef playerPrefab;

        NetworkRunner _runner;

        void OnGUI()
        {
            if (_runner != null)
            {
                GUI.Label(new Rect(20, 20, 400, 30), $"接続中... (Session: {RoomName})");
                return;
            }

            GUI.Label(new Rect(20, 20, 400, 30), "Sand Tetris - Fusion 接続テスト (Shared Mode)");
            if (GUI.Button(new Rect(20, 60, 200, 40), "参加する"))
            {
                _ = StartConnection(GameMode.Shared);
            }
        }

        async Task StartConnection(GameMode mode)
        {
            _runner = gameObject.AddComponent<NetworkRunner>();
            _runner.ProvideInput = true;

            var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
            var sceneInfo = new NetworkSceneInfo();
            if (scene.IsValid)
            {
                sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive);
            }

            var result = await _runner.StartGame(new StartGameArgs
            {
                GameMode = mode,
                SessionName = RoomName,
                Scene = scene,
                SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>(),
            });

            if (result.Ok)
            {
                Debug.Log($"[Fusion] 接続成功: モード={mode}, セッション={_runner.SessionInfo.Name}");
            }
            else
            {
                Debug.LogError($"[Fusion] 接続失敗: {result.ShutdownReason}");
                _runner = null;
            }
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            Debug.Log($"[Fusion] プレイヤーが参加しました: {player}");

            // Shared Modeでは、各クライアントが「自分自身の分身」を自分で生成する。
            // 生成した本人に自動的に StateAuthority が与えられる。
            if (player == runner.LocalPlayer)
            {
                runner.Spawn(playerPrefab, Vector3.zero, Quaternion.identity, player);
            }
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            Debug.Log($"[Fusion] プレイヤーが退出しました: {player}");
        }

        // ↓ここから下は INetworkRunnerCallbacks が要求する残りのメソッド。
        // 今回の接続テストでは中身は空でOK。
        // もしインストールした Fusion のバージョンでメンバー不足のコンパイルエラーが出たら、
        // Unity/IDEが指摘する不足分をこの下に追加してください
        // (Visual StudioやRiderの「インターフェースの実装」クイックフィックスで自動生成できます)。
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