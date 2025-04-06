using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Network
{
    public class NetworkSpawnItem : MyNetworkBehaviour, ISpawnItem, IRadarable
    {
        /// <summary>
        /// �X�|�[���A�C�e�����ŃC�x���g
        /// </summary>
        public event EventHandler SpawnItemDestroyEvent;

        /// <summary>
        /// �擾���Ɏg�p�\�ƂȂ�A�C�e��
        /// </summary>
        public IDroneItem DroneItem { get; private set; }

        public IRadarable.ObjectType Type => IRadarable.ObjectType.Item;

        public bool IsRadarable => true;

        public List<GameObject> NotRadarableList => new List<GameObject>();

        [SerializeField, Tooltip("Addressable�̃L�[��")]
        private string _addressableKey = string.Empty;

        [SerializeField, Tooltip("�A�C�e���������ɕ\������A�C�R��")]
        private Image _iconImage = null;

        [SerializeField, Tooltip("�擾���Ɏg�p�\�ƂȂ�A�C�e���i���vIDroneItem�C���^�[�t�F�[�X�����j")]
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
