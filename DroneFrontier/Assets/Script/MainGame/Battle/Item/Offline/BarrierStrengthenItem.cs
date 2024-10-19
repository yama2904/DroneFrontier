using Offline.Player;
using UnityEngine;

namespace Offline
{
    public class BarrierStrengthenItem : MonoBehaviour, IGameItem
    {
        [SerializeField, Tooltip("バリア強化オブジェクト")]
        private GameObject _barrierStrengthen;

        [SerializeField, Tooltip("ダメージ軽減率")]
        private float _damageDownPercent = 0.5f;

        [SerializeField, Tooltip("強化時間（秒）")]
        private int _time = 10;

        public bool UseItem(GameObject drone)
        {
            return drone.GetComponent<DroneStatus>().AddStatus(_barrierStrengthen.GetComponent<IDroneStatus>(), _damageDownPercent, _time);
        }
    }
}