using UnityEngine;
using UnityEngine.UI;

namespace Common
{
    public class BrightnessManager : MonoBehaviour
    {
        /// <summary>
        /// 明るさの最小値
        /// </summary>
        private const float MIN_BRIGHTNESS = 200.0f / 255.0f;

        private const float RED = 0;
        private const float GREEN = 0;
        private const float BLUE = 0;

        /// <summary>
        /// 明るさの初期値
        /// </summary>
        private const float INIT_BRIGHTNESS = 1f;

        /// <summary>
        /// 明るさ調整用画像
        /// </summary>
        private static Image _maskImage = null;

        /// <summary>
        /// フェードイン/フェードアウト時の毎フレーム明るさ増減量
        /// </summary>
        private static float _fadeValue = 0;

        /// <summary>
        /// フェードイン中であるか
        /// </summary>
        private static bool _isFadeIn = false;

        /// <summary>
        /// フェードアウト中であるか
        /// </summary>
        private static bool _isFadeOut = false;

        /// <summary>
        /// 明るさを0～1で調整<br/>
        /// 0→1へ近づくほど明るくなる
        /// </summary>
        public static float Brightness
        {
            get { return _brightness; }
            set
            {
                _brightness = value;
                if (value < 0)
                {
                    _brightness = 0;
                }
                if (value > 1f)
                {
                    _brightness = 1f;
                }
                ApplyImageColor();
            }
        }
        private static float _brightness = INIT_BRIGHTNESS;

        /// <summary>
        /// フェードインを開始して徐々に明るくする
        /// </summary>
        /// <param name="fadeSec">最大の明るさになるまでの時間（秒）</param>
        public static void FadeIn(float fadeSec)
        {
            if (_isFadeOut)
            {
                _isFadeOut = false;
            }
            _isFadeIn = true;
            _fadeValue = (Time.fixedDeltaTime / fadeSec) * (1 - _brightness);
        }

        /// <summary>
        /// フェードアウトを開始して徐々に暗くする
        /// </summary>
        /// <param name="fadeSec">最大の暗さになるまでの時間（秒）</param>
        public static void FadeOut(float fadeSec)
        {
            if (_isFadeIn)
            {
                _isFadeIn = false;
            }
            _isFadeOut = true;
            _fadeValue = (Time.fixedDeltaTime / fadeSec) * _brightness;
        }

        /// <summary>
        /// フェードイン/フェードアウトを停止
        /// </summary>
        public static void StopFadeInOut()
        {
            _isFadeIn = false;
            _isFadeOut = false;
            _fadeValue = 0;
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            _maskImage = transform.Find("Canvas/Panel").GetComponent<Image>();
            ApplyImageColor();
        }

        private void FixedUpdate()
        {
            if (!_isFadeIn && !_isFadeOut) return;

            // フェードイン
            if (_isFadeIn)
            {
                Brightness += _fadeValue;
                if (Brightness == 1)
                {
                    _isFadeIn = false;
                }
            }

            // フェードアウト
            if (_isFadeOut)
            {
                Brightness -= _fadeValue;
                if (Brightness == 0)
                {
                    _isFadeOut = false;
                }
            }

            ApplyImageColor();
        }

        /// <summary>
        /// 指定した明るさを画像に適用する
        /// </summary>
        private static void ApplyImageColor()
        {
            _maskImage.color = new Color(RED, GREEN, BLUE, (1 - Brightness) * MIN_BRIGHTNESS);
        }
    }
}