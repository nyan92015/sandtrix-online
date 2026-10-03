using UnityEngine;

namespace SandTetris
{
    /// <summary>
    /// キーボード入力を読み取り、BoardPresenter へ伝える。
    /// </summary>
    public class BoardInputReader
    {
        readonly BoardPresenter _presenter;

        public BoardInputReader(BoardPresenter presenter)
        {
            _presenter = presenter;
        }

        public void ReadInput()
        {
            int dir = 0;
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) dir = -1;
            else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) dir = 1;

            bool moveKeyDown = Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow) ||
                                Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D);
            _presenter.SetMoveInput(dir, moveKeyDown);

            bool rotate = Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W);
            _presenter.SetRotateInput(rotate);

            bool softDrop = Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S);
            _presenter.SetSoftDrop(softDrop);
        }
    }
}