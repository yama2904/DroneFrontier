using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Drone.Battle
{
    public class DroneStatusComponent : MonoBehaviour, IDroneComponent
    {
        /// <summary>
        /// 変化中のステータスリスト
        /// </summary>
        public List<IDroneStatusChange> Statuses => _statuses.Select(x => x.status).ToList();

        /// <summary>
        /// プレイヤーであるか
        /// </summary>
        public bool IsPlayer { get; set; } = false;

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
        /// 付与中のステータス一覧
        /// </summary>
        private List<(IDroneStatusChange status, RectTransform icon)> _statuses = new List<(IDroneStatusChange status, RectTransform icon)>();

        public void Initialize() { }

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

            // ステータス終了イベントを設定
            status.OnStatusEnd += OnDroneStatusEnd;

            lock (_statuses)
            {
                // ステータス変化アイコンを表示
                RectTransform iconTransform = null;
                if (IsPlayer)
                {
                    Image icon = status.InstantiateIcon();
                    if (icon != null)
                    {
                        iconTransform = icon.rectTransform;
                        iconTransform.SetParent(_statusIconCanvas, false);

                        // アイコン表示位置調整
                        int iconCount = _statuses.Where(x => x.icon != null).Count();
                        iconTransform.localPosition = new Vector3(STATUS_ICON_WIDTH * iconCount,
                                                                  iconTransform.localPosition.y,
                                                                  iconTransform.localPosition.z);
                    }
                }

                // ステータス一覧に追加
                _statuses.Add((status, iconTransform));
            }

            // ステータス変化追加イベント発火
            OnStatusAdd?.Invoke(this, status);

            return true;
        }

        /// <summary>
        /// ステータス変化終了イベント
        /// </summary>
        /// <param name="sender">イベントオブジェクト</param>
        /// <param name="e">イベント引数</param>
        private void OnDroneStatusEnd(object sender, EventArgs e)
        {
            IDroneStatusChange status = sender as IDroneStatusChange;

            lock (_statuses)
            {
                // 終了したステータス情報取得
                int index = _statuses.FindIndex(x => x.status == status);
                RectTransform icon = _statuses[index].icon;

                // ステータスリストから除去
                _statuses.RemoveAt(index);

                // 状態異常アイコンを削除
                if (icon != null)
                {
                    Destroy(icon.gameObject);

                    // 削除した分アイコンの表示を詰める
                    int iconCount = 0;
                    for (int i = 0; i < _statuses.Count; i++)
                    {
                        if (_statuses[i].icon == null) continue;

                        RectTransform t = _statuses[i].icon;
                        t.localPosition = new Vector3(STATUS_ICON_WIDTH * iconCount, t.localPosition.y, t.localPosition.z);
                        iconCount++;
                    }
                }
            }

            // イベント削除
            status.OnStatusEnd -= OnDroneStatusEnd;

            // ステータス変化終了イベント発火
            OnStatusEnd?.Invoke(this, status);

            Debug.Log("StatusEndEvent:" + sender.GetType().ToString());
        }
    }
}