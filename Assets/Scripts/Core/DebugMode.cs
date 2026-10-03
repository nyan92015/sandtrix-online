namespace SandTetris
{
    /// <summary>
    /// デバッグプレイ(ネットワーク接続なしの1人プレイ確認用)が有効かどうかを持つだけの、
    /// シーンをまたいで参照できる静的フラグ。
    ///
    /// static なので、シーンを移動しても値はそのまま残る(DontDestroyOnLoadのオブジェクトを
    /// 別途用意しなくて済む、簡易的な方法)。
    /// </summary>
    public static class DebugMode
    {
        public static bool Enabled;
    }
}