using Common;
using Cysharp.Threading.Tasks;
using Drone;
using Drone.Battle;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace Battle.Status
{
    public class SpeedDownStatus : IDroneStatusChange
    {
        public event EventHandler OnStatusEnd;

        /// <summary>
        /// スピードダウン付与したオブジェクトのMoveコンポーネント
        /// </summary>
        private DroneMoveComponent _move = null;

        /// <summary>
        /// スピードダウン付与したオブジェクトのSoundコンポーネント
        /// </summary>
        private DroneSoundComponent _sound = null;

        /// <summary>
        /// 再生したSE番号
        /// </summary>
        private int _seId = 0;

        /// <summary>
        /// 発行した移動速度変更ID
        /// </summary>
        private int _changeSpeedId = -1;

        /// <summary>
        /// キャンセルトークン発行クラス
        /// </summary>
        private CancellationTokenSource _cancel = new CancellationTokenSource();

        public Image InstantiateIcon()
        {
            return Addressables.InstantiateAsync("SpeedDownIcon").WaitForCompletion().GetComponent<Image>();
        }

        public bool Invoke(GameObject drone, float statusSec, params object[] addParams)
        {
            // コンポーネント取得
            _move = drone.GetComponent<DroneMoveComponent>();
            _sound = drone.GetComponent<DroneSoundComponent>();

            // スピードダウン効果付与
            _changeSpeedId = _move.ChangeMoveSpeedPercent(1 - (float)addParams[0]);

            // スピードダウンSE再生
            if (_sound != null)
            {
                _seId = _sound.Play(SoundManager.SE.MagneticArea, 1, true);
            }

            // スピードダウン終了タイマー設定
            UniTask.Void(async () =>
            {
                await UniTask.Delay(TimeSpan.FromSeconds(statusSec), cancellationToken: _cancel.Token);
                EndSpeedDown();
            });

            return true;
        }

        /// <summary>
        /// スピードダウン終了
        /// </summary>
        public void EndSpeedDown()
        {
            // スピードダウン終了
            _move.ResetMoveSpeed(_changeSpeedId);
            _sound?.StopSE(_seId);

            // スピードダウン終了タイマー停止
            _cancel.Cancel();

            // 終了イベント発火
            OnStatusEnd?.Invoke(this, EventArgs.Empty);
        }
    }
}