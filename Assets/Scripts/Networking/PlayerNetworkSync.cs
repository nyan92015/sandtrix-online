using Fusion;
using UnityEngine;

namespace SandTetris
{
    /// <summary>
    /// プレイヤーごとにアタッチされるネットワークオブジェクト(Shared Mode前提)。
    ///
    /// Shared Modeでは、自分が生成したオブジェクトには自動的に StateAuthority が与えられるため、
    /// 自分の盤面データを Networked配列に直接書き込める。RPCは使わない。
    /// 配列はビットパッキング済み(1マス3ビット)のデータを保持し、Fusionが変化した部分だけを
    /// 自動的に検出して同期してくれる。
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

        /// <summary>ローカル(自分)のBoardModel。Spawned時に自分のBoardViewから取得する。</summary>
        public BoardModel LocalModel;

        RemoteBoardView _remoteView;
        float _timer;

        /// <summary>
        /// 生成された瞬間に呼ばれる。StateAuthorityを持っている(=自分自身の分身)かどうかで
        /// 送信側になるか受信側になるかを自動的に決める。
        /// </summary>
        public override void Spawned()
        {
            if (Object.HasStateAuthority)
            {
                var localView = FindFirstObjectByType<BoardView>();
                if (localView != null)
                {
                    LocalModel = localView.Model;
                    GridWidth = LocalModel.Grid.Width;
                    GridHeight = LocalModel.Grid.Height;
                }
            }
            else
            {
                _remoteView = FindFirstObjectByType<RemoteBoardView>();
            }
        }

        /// <summary>
        /// シミュレーションステップごとに呼ばれる(ネットワークの正式な状態を更新する場所)。
        /// StateAuthorityを持つ側だけが、自分の盤面をNetworked配列に書き込む。
        /// </summary>
        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority || LocalModel == null) return;

            _timer += Runner.DeltaTime;
            if (_timer < writeInterval) return;
            _timer = 0f;

            byte[] packed = BoardSnapshotCodec.EncodePacked(LocalModel);
            if (packed.Length > MaxPackedBytes)
            {
                Debug.LogError($"[PlayerNetworkSync] 盤面データが MaxPackedBytes({MaxPackedBytes})を超えています: {packed.Length}バイト。MaxPackedBytesを増やしてください。");
                return;
            }

            PackedLength = packed.Length;
            for (int i = 0; i < packed.Length; i++)
            {
                PackedBoard.Set(i, packed[i]);
            }
        }

        /// <summary>
        /// 描画フレームごとに呼ばれる(見た目の更新はここで行うのがFusion推奨)。
        /// StateAuthorityを持たない側(=相手の分身)だけが、届いたデータをデコードして
        /// RemoteBoardViewに渡す。
        /// </summary>
        public override void Render()
        {
            if (Object.HasStateAuthority) return;
            if (_remoteView == null || PackedLength <= 0) return;
            if (GridWidth <= 0 || GridHeight <= 0) return;

            var packed = new byte[PackedLength];
            for (int i = 0; i < PackedLength; i++) packed[i] = PackedBoard[i];

            int cellCount = GridWidth * GridHeight;
            byte[] unpacked = BoardSnapshotCodec.DecodePacked(packed, cellCount);
            _remoteView.ApplySnapshot(unpacked);
        }
    }
}