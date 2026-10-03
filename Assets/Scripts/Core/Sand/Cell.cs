using UnityEngine;

namespace SandTetris
{
    /// <summary>
    /// グリッド上の1マス分のデータ。
    /// </summary>
    public struct Cell
    {
        public bool Occupied;
        public Color32 Color;
        public byte ColorIndex; // 色グループ(ライン消去の同色判定に使う)
        public bool IsConcrete; // シーソー式の地面。壊れない・動かない・ライン消去に参加しない
        public bool IsAsh;      // 灰。砂と同じ物理で落ちるが、盤面の底に触れると消える。ライン消去に参加しない
        public float AshAge;    // 灰になってからの経過時間(秒)。IsAshがtrueのときだけ意味を持つ
    }
}