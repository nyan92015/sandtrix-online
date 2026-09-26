namespace SandTetris
{
    /// <summary>
    /// ゲームオーバー後のステート。何も更新しない(入力も物理も完全停止)。
    /// リスタート処理は将来的にここか、外側のGameSession側で扱う想定。
    /// </summary>
    public class GameOverState : IBoardState
    {
        public void Enter() { }
        public void Update(float deltaTime) { }
        public void Exit() { }
    }
}