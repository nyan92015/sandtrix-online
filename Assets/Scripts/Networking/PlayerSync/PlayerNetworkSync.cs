using Fusion;
using UnityEngine;

namespace SandTetris
{
    /// <summary>
    /// プレイヤーごとにアタッチされるネットワークオブジェクト(Shared Mode前提)。
    /// Shared Modeでは、自分が生成したオブジェクトには自動的に StateAuthority が与えられるため、
    /// 自分の盤面データを Networked配列に直接書き込める。
    /// 配列はビットパッキング済み(1マス3ビット)のデータを保持
    /// </summary>
    public class PlayerNetworkSync : NetworkBehaviour
    {
        // 想定される最大の盤面サイズに余裕を持たせた固定長(Networked配列は容量を実行時に変えられないため)。
        // 注意: Fusionの NetworkArray<byte> は1要素につき1バイトではなく1ワード(4バイト)を消費し、
        // かつ1オブジェクトあたりの状態サイズには 8192ワード(32KB)という上限がある。
        // 標準盤面(10x20マス、blockSize=8→80x160ピクセル)なら3ビットパッキングで4800バイト必要なので、
        // 余裕を見て5120としている。これより盤面を大きくする場合はここも調整すること。
        public const int MaxPackedBytes = 5120;

        [Tooltip("Networked配列に書き込む間隔(秒)。小さいほど滑らかだが、Fusionの差分検出コストが増える。")]
        [SerializeField] float writeInterval = 0.15f;

        [Networked, Capacity(MaxPackedBytes)]
        public NetworkArray<byte> PackedBoard => default;

        [Networked] public int GridWidth { get; set; }
        [Networked] public int GridHeight { get; set; }
        [Networked] public int PackedLength { get; set; }

        // --- ここからミノ専用の軽量チャンネル。毎ティック更新するので滑らかに動く ---
        [Networked] public int PieceShapeIndex { get; set; } = -1; // -1 = ミノなし(着地直後の空白フレームなど)
        [Networked] public int PieceColorIndex { get; set; }
        [Networked] public int PieceRotationSteps { get; set; }
        [Networked] public int PieceAnchorX { get; set; }
        [Networked] public int PieceAnchorY { get; set; }
        [Networked] public bool PieceIsSpecial { get; set; } // 特別なミノかどうか。相手側でも見た目(縁取りの明滅など)を再現するために送る

        // ネクストミノ(プレビュー表示用)。位置・回転は不要(常にプレビュー中央に表示するだけなので)
        [Networked] public int NextPieceShapeIndex { get; set; } = -1;
        [Networked] public int NextPieceColorIndex { get; set; }

        // --- スコア関連。数値だけなので軽く、毎ティック更新しても問題ない ---
        [Networked] public int NetScore { get; set; }
        [Networked] public int NetMultiplier { get; set; } = 1;
        [Networked] public int NetLinesCleared { get; set; }
        [Networked] public float NetBucketFillRatio { get; set; }

        // 自分が守備側かつ基準点を超えているときの、「今の圧力のまま確定したら、どこまで上がるか」。
        // 相手側の警告表示(赤枠+流れる縞)に使う。0なら警告なし。
        [Networked] public float NetGroundWarningLevel { get; set; }

        // 自分がゲームオーバーになったかどうか。相手側から見ると「相手のゲームオーバーを検知する」ために使う。
        [Networked] public bool NetIsGameOver { get; set; }

        /// <summary>ローカル(自分)のBoardModel。Spawned時に自分のBoardViewから取得する。</summary>
        public BoardModel LocalModel;

        BoardView _myOwnBoardView;
        RemoteBoardView _remoteView;
        PlayerNetworkSender _sender;
        PlayerNetworkReceiver _receiver;

        /// <summary>
        /// 生成された瞬間に呼ばれる。StateAuthorityを持っている(=自分自身の分身)かどうかで
        /// 送信側になるか受信側になるかを自動的に決める。
        /// </summary>
        public override void Spawned()
        {
            // blockSizeは送信側・受信側どちらでもミノの再構築に必要なので、
            // StateAuthorityの有無に関わらずシーン上のBoardViewから取得しておく。
            var localView = FindFirstObjectByType<BoardView>();
            int blockSize = localView != null ? localView.BlockSize : 0;
            _myOwnBoardView = localView;

            if (Object.HasStateAuthority)
            {
                if (localView != null)
                {
                    LocalModel = localView.Model;
                    GridWidth = LocalModel.Grid.Width;
                    GridHeight = LocalModel.Grid.Height;

                    // 着地した瞬間、次のネットワーク更新ですぐに盤面(固定砂)を送れるようにする。
                    // これをしないと、「ミノが消えた」情報の方が「新しく固まった砂」より先に届いてしまい、
                    // 相手の画面で着地の瞬間に一瞬何も表示されない空白ができてしまう。
                    LocalModel.OnPieceLanded += HandlePieceLanded;

                    // 感染が起きた瞬間にも、相手側へ揺れの合図を送る
                    LocalModel.OnInfection += HandleInfectionForShake;
                }

                _sender = new PlayerNetworkSender(this, writeInterval);
            }
            else
            {
                _remoteView = FindFirstObjectByType<RemoteBoardView>();
                var matchResultController = FindFirstObjectByType<MatchResultController>();
                _receiver = new PlayerNetworkReceiver(this, _remoteView, blockSize, matchResultController, _myOwnBoardView);
            }
        }

        void HandlePieceLanded(System.Collections.Generic.IReadOnlyList<Vector2Int> positions, System.Collections.Generic.IReadOnlyList<Color32> colors, byte colorIndex)
        {
            _sender?.ForceSendSoon();
            RPC_Shake();
        }

        void HandleInfectionForShake(System.Collections.Generic.IReadOnlyList<int> changedIndices, System.Collections.Generic.IReadOnlyList<Color32> oldColors, Vector2Int center)
        {
            RPC_Shake();
        }

        /// <summary>
        /// 揺れの合図。全員に送られるが、送信元(自分自身)は自分の画面ですでに揺れ済みなので無視する。
        /// 位置などのデータは一切持たない、ごく軽いRPC。
        /// </summary>
        [Rpc(RpcSources.All, RpcTargets.All)]
        void RPC_Shake()
        {
            if (Object.HasStateAuthority) return; // 自分が送った合図は、自分には反映しない
            _remoteView?.TriggerShake();
        }

        /// <summary>
        /// シミュレーションステップごとに呼ばれる(ネットワークの正式な状態を更新する場所)。
        /// StateAuthorityを持つ側だけが、自分の盤面をNetworked配列に書き込む。
        /// </summary>
        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority || LocalModel == null) return;
            _sender.Write(LocalModel, _myOwnBoardView?.Score, _myOwnBoardView?.Ground, _myOwnBoardView, Runner.DeltaTime);
        }

        /// <summary>
        /// 描画フレームごとに呼ばれる(見た目の更新はここで行うのがFusion推奨)。
        /// StateAuthorityを持たない側(=相手の分身)だけが、届いたデータをデコードして
        /// RemoteBoardViewに渡す。
        /// </summary>
        public override void Render()
        {
            if (Object.HasStateAuthority) return;
            _receiver?.Apply();
        }
    }
}