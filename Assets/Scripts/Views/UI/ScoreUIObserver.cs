using TMPro;
using UnityEngine.UI;

namespace SandTetris
{
    /// <summary>
    /// ScoreTrackerのイベントを購読して、TextMeshProのテキストやゲージを更新するだけのオブザーバー。
    /// ゲームロジックには一切関与しない(AudioObserverと同じ立ち位置)。
    /// バケツのゲージだけは時間経過で連続的に変わるため、毎フレーム Tick を呼んでもらう必要がある。
    /// </summary>
    public class ScoreUIObserver
    {
        readonly ScoreTracker _score;
        readonly TMP_Text _scoreText;
        readonly TMP_Text _multiplierText;
        readonly TMP_Text _linesClearedText;
        readonly Slider _bucketGaugeSlider;

        public ScoreUIObserver(ScoreTracker score, TMP_Text scoreText, TMP_Text multiplierText, TMP_Text linesClearedText, Slider bucketGaugeSlider)
        {
            _score = score;
            _scoreText = scoreText;
            _multiplierText = multiplierText;
            _linesClearedText = linesClearedText;
            _bucketGaugeSlider = bucketGaugeSlider;

            if (_bucketGaugeSlider != null)
            {
                _bucketGaugeSlider.minValue = 0f;
                _bucketGaugeSlider.maxValue = 1f;
                _bucketGaugeSlider.interactable = false; // プレイヤーが触って動かせないようにする
            }

            score.OnScoreChanged += HandleScoreChanged;
            score.OnMultiplierChanged += HandleMultiplierChanged;
            score.OnLinesClearedChanged += HandleLinesClearedChanged;

            // 生成された時点の初期値も反映しておく
            HandleScoreChanged(score.TotalScore);
            HandleMultiplierChanged(score.CurrentMultiplier);
            HandleLinesClearedChanged(score.LinesCleared);
        }

        void HandleScoreChanged(int total)
        {
            if (_scoreText != null) _scoreText.text = total.ToString();
        }

        void HandleMultiplierChanged(int multiplier)
        {
            if (_multiplierText != null) _multiplierText.text = $"x{multiplier}";
        }

        void HandleLinesClearedChanged(int lines)
        {
            if (_linesClearedText != null) _linesClearedText.text = lines.ToString();
        }

        /// <summary>
        /// 毎フレーム呼ぶ。バケツの残量は時間経過で連続的に減っていくため、
        /// イベント駆動ではなくポーリングでゲージを更新する。
        /// </summary>
        public void Tick()
        {
            if (_bucketGaugeSlider != null)
            {
                _bucketGaugeSlider.value = _score.CurrentBucketFillRatio;
            }
        }
    }
}