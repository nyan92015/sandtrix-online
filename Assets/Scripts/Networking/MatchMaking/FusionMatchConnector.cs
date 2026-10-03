using System.Threading.Tasks;
using Fusion;
using UnityEngine;

namespace SandTetris
{
    /// <summary>
    /// Fusionへの接続・切断、プレイヤーの生成を担当する。
    /// MonoBehaviourではないが、AddComponentなどUnity固有の操作が必要な箇所は、
    /// 呼び出し元(MatchmakingLauncher)のGameObjectを間借りして行う。
    /// </summary>
    public class FusionMatchConnector
    {
        readonly MonoBehaviour _owner;
        readonly NetworkPrefabRef _playerPrefab;
        readonly int _battleSceneBuildIndex;

        public NetworkRunner Runner { get; private set; }

        public FusionMatchConnector(MonoBehaviour owner, NetworkPrefabRef playerPrefab, int battleSceneBuildIndex)
        {
            _owner = owner;
            _playerPrefab = playerPrefab;
            _battleSceneBuildIndex = battleSceneBuildIndex;
        }

        /// <summary>
        /// クイックマッチで接続する(SessionNameを指定しないことで、Fusionに
        /// 「空いているセッションがあれば参加、無ければ新規作成」を任せる)。
        /// 成功時は ok=true。失敗時は ok=false と、画面表示用のエラーメッセージを返す。
        /// </summary>
        public async Task<(bool ok, string errorMessage)> Connect()
        {
            Runner = _owner.gameObject.AddComponent<NetworkRunner>();
            Runner.ProvideInput = true;

            var scene = SceneRef.FromIndex(_battleSceneBuildIndex);

            var result = await Runner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Shared,
                PlayerCount = 2, // 1対1専用なので、3人目は別のセッションに回してもらう
                Scene = scene,
                SceneManager = _owner.gameObject.AddComponent<NetworkSceneManagerDefault>(),
            });

            if (result.Ok)
            {
                Debug.Log($"[Matchmaking] 接続成功: セッション={Runner.SessionInfo.Name}");
                return (true, null);
            }

            Debug.LogError($"[Matchmaking] 接続失敗: {result.ShutdownReason}");
            Object.Destroy(Runner);
            Runner = null;
            return (false, $"接続に失敗しました: {result.ShutdownReason}");
        }

        /// <summary>
        /// Shared Modeでは、各クライアントが「自分自身の分身」を自分で生成する。
        /// 生成した本人に自動的に StateAuthority が与えられる。
        /// </summary>
        public void HandlePlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            Debug.Log($"[Matchmaking] プレイヤーが参加しました: {player}");
            if (player == runner.LocalPlayer)
            {
                runner.Spawn(_playerPrefab, Vector3.zero, Quaternion.identity, player);
            }
        }

        public void HandlePlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            Debug.Log($"[Matchmaking] プレイヤーが退出しました: {player}");
            if (player != runner.LocalPlayer)
            {
                // 相手が抜けた: 自分の不戦勝として扱う
                var matchResult = Object.FindFirstObjectByType<MatchResultController>();
                matchResult?.NotifyOpponentLeft();
            }
        }

        public void Shutdown()
        {
            if (Runner != null)
            {
                _ = Runner.Shutdown();
            }
        }
    }
}