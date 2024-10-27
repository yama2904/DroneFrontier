using Offline.Player;
using UnityEngine;

namespace Offline
{
    public class BarrierStrengthenItem : MonoBehaviour, IDroneItem
    {
        [SerializeField, Tooltip("ダメージ軽減率")]
        private float _damageDownPercent = 0.5f;

        [SerializeField, Tooltip("強化時間（秒）")]
        private int _strengthenSec = 10;

        public bool UseItem(GameObject drone)
        {
            return drone.GetComponent<DroneStatusComponent>().AddStatus(new BarrierStrengthenStatus(), _strengthenSec, _damageDownPercent);
        }
    }
}