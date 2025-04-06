using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Network
{
    public class NetworkSpawnItem : MyNetworkBehaviour, ISpawnItem, IRadarable
    {
        /// <summary>
        /// スポーンアイテム消滅イベント
        /// </summary>
        public event EventHandler SpawnItemDestroyEvent;

        /// <summary>
        /// 取得時に使用可能となるアイテム
        /// </summary>
        public IDroneItem DroneItem { get; private set; }

        public IRadarable.ObjectType Type => IRadarable.ObjectType.Item;

        public bool IsRadarable => true;

        public List<GameObject> NotRadarableList => new List<GameObject>();

        [SerializeField, Tooltip("Addressableのキー名")]
        private string _addressableKey = string.Empty;

        [SerializeField, Tooltip("アイテム所持中に表示するアイコン")]
        private Image _iconImage = null;

        [SerializeField, Tooltip("取得時に使用可能となるアイテム（※要IDroneItemインターフェース実装）")]
        private GameObject _droneItem = null;

        public override string GetAddressKey()
        {
            return _addressableKey;
        }

        public Image InstantiateIcon()
        {
            return Instantiate(_iconImage);
        }

        protected override void Awake()
        {
            DroneItem = Instantiate(_droneItem, Vector3.zero, Quaternion.identity).GetComponent<IDroneItem>();
        }

        protected override void OnDestroy()
        {
            SpawnItemDestroyEvent?.Invoke(this, EventArgs.Empty);
        }
    }
}
