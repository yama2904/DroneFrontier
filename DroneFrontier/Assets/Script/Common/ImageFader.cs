using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace Common
{
    public class ImageFader
    {
        /// <summary>
        /// 指定した画像のフェードインを行う
        /// </summary>
        /// <param name="image">フェードインを行う画像</param>
        /// <param name="fadeInSec">フェードインが完了するまでの時間（秒）</param>
        /// <param name="token">キャンセルトークン</param>
        public static async UniTask FadeIn(Image image, float fadeInSec, CancellationToken token = default)
        {
            float timer = 0;
            while (true)
            {
                // フェードインが完了したら終了
                if (timer >= fadeInSec)
                {
                    ChangeImageAlfa(image, 1f);
                    break;
                }

                timer += Time.deltaTime;
                if (timer < fadeInSec)
                {
                    float alpha = timer / fadeInSec;
                    ChangeImageAlfa(image, alpha);
                }

                await UniTask.Delay(1, ignoreTimeScale: true, cancellationToken: token);
            }
        }

        /// <summary>
        /// 指定した画像のフェードアウトを行う
        /// </summary>
        /// <param name="image">フェードアウトを行う画像</param>
        /// <param name="fadeOutSec">フェードアウトが完了するまでの時間（秒）</param>
        /// <param name="token">キャンセルトークン</param>
        public static async UniTask FadeOut(Image image, float fadeOutSec, CancellationToken token = default)
        {
            float timer = 0;
            while (true)
            {
                // フェードアウトが完了したら終了
                if (timer >= fadeOutSec)
                {
                    ChangeImageAlfa(image, 0);
                    break;
                }

                timer += Time.deltaTime;
                if (timer < fadeOutSec)
                {
                    float alpha = 1.0f - timer / fadeOutSec;
                    ChangeImageAlfa(image, alpha);
                }

                await UniTask.Delay(1, ignoreTimeScale: true, cancellationToken: token);
            }
        }

        /// <summary>
        /// 画像のアルファ値を変更する
        /// </summary>
        /// <param name="image">アルファ値を変更する画像</param>
        /// <param name="alpha">変更後のアルファ値</param>
        private static void ChangeImageAlfa(Image image, float alpha)
        {
            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }
    }
}