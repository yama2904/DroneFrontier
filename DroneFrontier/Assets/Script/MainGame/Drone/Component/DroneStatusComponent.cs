using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;
using UnityEngine.UI;

namespace Offline
{
    namespace Player
    {
        public class DroneStatusComponent : MonoBehaviour
        {
            /// <summary>
            /// 変化中のステータスリスト
            /// </summary>
            public List<StatusChangeType> Statuses { get; private set; } = new List<StatusChangeType>();

            /// <summary>
            /// ステータス変化イベントハンドラー
            /// </summary>
            /// <param name="component">イベントコンポーネント</param>
            /// <param name="status">ステータス変化オブジェクト</param>
            public delegate void StatusEventHandler(DroneStatusComponent component, IDroneStatusChange status);

            /// <summary>
            /// ステータス変化追加イベント
            /// </summary>
            public event StatusEventHandler OnStatusAdd;

            /// <summary>
            /// ステータス変化終了イベント
            /// </summary>
            public event StatusEventHandler OnStatusEnd;

            /// <summary>
            /// 状態異常アイコン幅
            /// </summary>
            private int STATUS_ICON_WIDTH = 100;

            [SerializeField, Tooltip("状態異常アイコンを表示するCanvas")]
            private RectTransform _statusIconCanvas = null;

            /// <summary>
            /// 状態異常と対応するアイコン<br/>
            /// key:状態異常を付与したIDroneStatusChange, value:状態異常アイコンのRectTransform
            /// </summary>
            private OrderedDictionary _statusesIconMap = new OrderedDictionary();

            //アイコン
            [SerializeField] Image barrierWeakIcon = null;
            [SerializeField] Image speedDownIcon = null;

            /// <summary>
            /// ドローンにステータス変化を追加する
            /// </summary>
            /// <param name="status">追加するステータス変化</param>
            /// <param name="statusSec">ステータス変化時間（秒）</param>
            /// <param name="addParams">追加パラメータ</param>
            /// <returns>true:成功, false:失敗</returns>
            public bool AddStatus(IDroneStatusChange status, float statusSec, params object[] addParams)
            {
                // ステータス変化実行
                bool success = status.Invoke(gameObject, statusSec, addParams);
                if (!success) return false;

                // ステータス終了イベントを設定してリストに追加
                status.StatusEndEvent += StatusEndEvent;
                Statuses.Add(status.StatusType);

                // ステータス変化アイコンを表示
                Debug.Log(status.IconPrefab);
                if (status.IconPrefab != null)
                {
                    Image icon = Instantiate(status.IconPrefab);
                    RectTransform t = icon.rectTransform;
                    t.SetParent(_statusIconCanvas, false);

                    // アイコン表示位置調整
                    t.localPosition = new Vector3(STATUS_ICON_WIDTH * _statusesIconMap.Count, t.localPosition.y, t.localPosition.z);

                    // マップに追加
                    _statusesIconMap.Add(status, t);
                }

                // ステータス変化追加イベント発火
                OnStatusAdd?.Invoke(this, status);

                return true;
            }

            /*
            //スピードダウン
            public void SetSpeedDown(float downPercent)
            {
                //isStatus[(int)Status.SPEED_DOWN] = true;
                baseAction.MoveSpeed *= (1 - downPercent);
                speedDownCount++;

                //アイコン表示
                speedDownIcon.enabled = true;

                //SE再生
                speedDownSoundId = soundAction.PlayLoopSE(SoundManager.SE.MAGNETIC_AREA, SoundManager.SEVolume);
            }

            //スピードダウン解除
            public void UnSetSpeedDown(float downPercent)
            {
                baseAction.MoveSpeed *= 1 / (1 - downPercent);

                //スピードダウンがすべて解除されたらフラグも解除
                if (--speedDownCount <= 0)
                {
                    //isStatus[(int)Status.SPEED_DOWN] = false;
                }

                //アイコン非表示
                speedDownIcon.enabled = false;

                //SE停止
                soundAction.StopLoopSE(speedDownSoundId);
            }
            */

            /// <summary>
            /// ステータス変化終了イベント
            /// </summary>
            /// <param name="sender">イベントオブジェクト</param>
            /// <param name="e">イベント引数</param>
            private void StatusEndEvent(object sender, EventArgs e)
            {
                IDroneStatusChange status = sender as IDroneStatusChange;

                // ステータスリストから除去
                Statuses.Remove(status.StatusType);

                // 状態異常アイコンを削除
                if (_statusesIconMap.Contains(status))
                {
                    Destroy((_statusesIconMap[status] as RectTransform).gameObject);
                    _statusesIconMap.Remove(status);

                    // 削除した分アイコンの表示を詰める
                    for (int i = 0; i < _statusesIconMap.Count; i++)
                    {
                        RectTransform t = _statusesIconMap[i] as RectTransform;
                        t.localPosition = new Vector3(STATUS_ICON_WIDTH * i, t.localPosition.y, t.localPosition.z);
                    }
                }

                // イベント削除
                status.StatusEndEvent -= StatusEndEvent;

                // ステータス変化終了イベント発火
                OnStatusEnd?.Invoke(this, status);

                Debug.Log("StatusEndEvent:" + sender.GetType().ToString());
            }
        }
    }
}