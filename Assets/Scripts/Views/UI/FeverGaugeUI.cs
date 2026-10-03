using UnityEngine;
using UnityEngine.UI;

namespace SandTetris
{
    /// <summary>
    /// フィーバーゲージの見た目を管理する。
    /// 色をプログラムで塗るのではなく、用意されたスプライト素材(黒1枚+4色ぶん)を、
    /// 状況に応じてそのまま差し替える方式。
    ///
    /// 通常時: 4つのImageそれぞれに、今回のランダムな並び順(CurrentOrder)に対応する色のスプライトか、
    ///         まだ点灯していなければ黒(消灯)のスプライトを割り当てる。
    ///
    /// フィーバー中: 4つとも「フィーバーが始まった瞬間の色」のスプライトで統一し、
    ///              Image.fillAmount を1(満タン)から0(空)へ、残り時間に応じて減らしていく
    ///              (Image.type=Filled、上からの縦方向の減り方)。
    ///              フィーバーが終わると、自然に通常表示(=すでに進み始めている次の回の状態)に戻る。
    ///
    /// FeverSystem の中身(ロジック)には一切関与しない、完全に表示専用のオブザーバー。
    /// </summary>
    public class FeverGaugeUI
    {
        readonly Image[] _lampImages;
        readonly Sprite[] _litSprites; // 4色ぶん。インデックスはColorIndexと対応(0=赤、1=青、2=黄、3=緑)
        readonly Sprite _unlitSprite;  // 消灯時(黒)のスプライト

        FeverSystem _fever;
        byte[] _feverStartOrder; // フィーバーが始まった瞬間の色の並び(CurrentOrderは直後に上書きされるため、先に控えておく)

        public FeverGaugeUI(Image[] lampImages, Sprite[] litSprites, Sprite unlitSprite)
        {
            _lampImages = lampImages;
            _litSprites = litSprites;
            _unlitSprite = unlitSprite;

            // Image.fillAmount を使うので、見た目の設定をここで揃えておく。
            // スプライトをそのまま使うので、色(color)は白(= 何も掛け合わせない)にしておく。
            if (_lampImages != null)
            {
                foreach (var img in _lampImages)
                {
                    if (img == null) continue;
                    img.type = Image.Type.Filled;
                    img.fillMethod = Image.FillMethod.Vertical;
                    img.fillOrigin = (int)Image.OriginVertical.Top;
                    img.fillAmount = 1f;
                    img.color = Color.white;
                }
            }
        }

        /// <summary>対象のFeverSystemを購読し始める。盤面(BoardModel)が用意できたタイミングで呼ぶ。</summary>
        public void Bind(FeverSystem fever)
        {
            _fever = fever;
            _fever.OnOrderChanged += RedrawNormal;
            _fever.OnLampLit += _ => RedrawNormal();
            _fever.OnFeverStarted += HandleFeverStarted;
            RedrawNormal();
        }

        void HandleFeverStarted()
        {
            // この時点では、CurrentOrder はまだ「揃えたばかりの、今回の順番」のまま
            // (次の回用への上書きは、このイベントの発行より後に行われる)
            _feverStartOrder = (byte[])_fever.CurrentOrder.Clone();
        }

        /// <summary>毎フレーム呼ぶ。フィーバー中は、残り時間に応じてゲージを減らしていく。</summary>
        public void Tick()
        {
            if (_fever == null || _lampImages == null) return;
            if (!_fever.IsFeverActive) return; // フィーバー中でなければ、ここでは何もしない

            float fraction = _fever.FeverDuration > 0f
                ? Mathf.Clamp01(_fever.FeverTimeRemaining / _fever.FeverDuration)
                : 0f;

            for (int i = 0; i < _lampImages.Length; i++)
            {
                if (_lampImages[i] == null) continue;

                byte colorIndex = (_feverStartOrder != null && i < _feverStartOrder.Length) ? _feverStartOrder[i] : (byte)0;
                _lampImages[i].sprite = _litSprites[colorIndex % _litSprites.Length];
                _lampImages[i].fillAmount = fraction;
            }
        }

        void RedrawNormal()
        {
            if (_fever == null || _lampImages == null) return;
            if (_fever.IsFeverActive) return; // フィーバー中は、Tickによる表示を優先する(ここでは上書きしない)

            for (int i = 0; i < _lampImages.Length; i++)
            {
                if (_lampImages[i] == null) continue;
                _lampImages[i].fillAmount = 1f; // 通常表示では、満タン固定(点灯/消灯はスプライトの差し替えだけで表す)

                if (i >= _fever.CurrentOrder.Length)
                {
                    _lampImages[i].sprite = _unlitSprite;
                    continue;
                }

                byte colorIndex = _fever.CurrentOrder[i];
                bool lit = i < _fever.Progress;
                _lampImages[i].sprite = lit ? _litSprites[colorIndex % _litSprites.Length] : _unlitSprite;
            }
        }
    }
}