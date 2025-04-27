using Cysharp.Threading.Tasks;
using Drone.Battle;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace Battle.Status
{
    public class StunStatus : IDroneStatusChange
    {
        public event EventHandler OnStatusEnd;

        private FadeoutImage _createdMask;

        public Image InstantiateIcon()
        {
            return null;
        }

        public bool Invoke(GameObject drone, float statusSec, params object[] addParams)
        {
            // プレイヤーの場合はマスク生成
            if ((bool)addParams[0])
            {
                Addressables.InstantiateAsync("StunMask").Completed += handle =>
                {
                    _createdMask = handle.Result.GetComponent<FadeoutImage>();
                    _createdMask.FadeoutSec = statusSec;
                    _createdMask.OnFadeoutEnd += OnFadeoutEnd;
                };
            }
            else
            {
                // プレイヤー以外の場合はマスクしない

                // スタンの間ロックオン機能停止
                DroneLockOnComponent lockon = drone.GetComponent<DroneLockOnComponent>();
                lockon.SetEnableLockOn(false);

                // スタン終了タイマー設定
                UniTask.Void(async () =>
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(statusSec));
                    lockon.SetEnableLockOn(true);
                    OnStatusEnd?.Invoke(this, EventArgs.Empty);
                });
            }
            return true;
        }

        /// <summary>
        /// フェードアウト終了イベント
        /// </summary>
        /// <param name="o">イベントオブジェクト</param>
        /// <param name="e">イベント引数</param>
        private void OnFadeoutEnd(object o, EventArgs e)
        {
            // ステータス終了イベント発火
            OnStatusEnd?.Invoke(this, EventArgs.Empty);

            // イベント削除してオブジェクト破棄
            _createdMask.OnFadeoutEnd -= OnFadeoutEnd;
            UnityEngine.Object.Destroy(_createdMask.gameObject);
        }
    }
}