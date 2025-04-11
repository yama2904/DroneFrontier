using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace Offline
{
    public class JammingItem : IDroneItem
    {
        /// <summary>
        /// ジャミングボットの生存時間（秒）
        /// </summary>
        public float DestroySec { get; set; } = 60.0f;

        /// <summary>
        /// ボット生成時の移動時間（秒）
        /// </summary>
        public float InitMoveSec { get; set; } = 1;

        public Image InstantiateIcon()
        {
            return Addressables.InstantiateAsync("JammingIconImage").WaitForCompletion().GetComponent<Image>();
        }

        public bool UseItem(GameObject drone)
        {
            // ジャミングボット生成
            JammingBot bot = Addressables.InstantiateAsync("JammingBot", drone.transform.position, Quaternion.identity)
                                         .WaitForCompletion()
                                         .GetComponent<JammingBot>();

            // パラメータ設定
            bot.Creater = drone;
            bot.DestroySec = DestroySec;
            bot.InitMoveSec = InitMoveSec;

            return true;
        }
    }
}