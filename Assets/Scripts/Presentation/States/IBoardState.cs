namespace SandTetris
{
    /// <summary>
    /// 盤面の「今どういう状況か」を表すステート。
    /// Enter/Update/Exitの3つだけを持つ、教科書通りのシンプルな形。
    /// 各ステートが自分の振る舞いに全責任を持つことで、
    /// 「消去中は描画をスキップしてしまう」のような漏れが構造的に起きにくくなる。
    /// </summary>
    public interface IBoardState
    {
        void Enter();
        void Update(float deltaTime);
        void Exit();
    }
}