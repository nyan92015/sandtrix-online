using System.Collections.Generic;
using UnityEngine;

namespace SandTetris
{
    /// <summary>
    /// 着地時にRawImage(などのRectTransform)を一瞬揺らす演出。
    /// BoardModel.OnPieceLanded を購読するだけの独立したオブザーバー。
    /// </summary>
    public class ScreenShakeEffect
    {
        readonly RectTransform _target;
        readonly Vector2 _originalPos;
        readonly float _duration;
        readonly float _magnitude;
        float _timer;

        public ScreenShakeEffect(BoardModel model, RectTransform target, float duration, float magnitude)
        {
            _target = target;
            _originalPos = target != null ? target.anchoredPosition : Vector2.zero;
            _duration = duration;
            _magnitude = magnitude;

            model.OnPieceLanded += (_, __, ___) => Trigger();
        }

        void Trigger()
        {
            _timer = _duration;
        }

        /// <summary>毎フレーム呼ぶ。RectTransformの位置を直接更新する。</summary>
        public void Tick(float deltaTime)
        {
            if (_target == null) return;

            if (_timer > 0f)
            {
                _timer -= deltaTime;
                float t = Mathf.Clamp01(_timer / _duration);
                _target.anchoredPosition = _originalPos + Random.insideUnitCircle * _magnitude * t;
            }
            else
            {
                _target.anchoredPosition = _originalPos;
            }
        }
    }
}