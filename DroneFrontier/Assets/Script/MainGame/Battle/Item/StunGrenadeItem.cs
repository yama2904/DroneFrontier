using UnityEngine;

namespace Offline
{
    public class StunGrenadeItem : MonoBehaviour, IDroneItem
    {
        [SerializeField, Tooltip("投擲オブジェクト")]
        private StunGrenade _throwObject = null;

        [SerializeField, Tooltip("投擲角度")]
        private Transform _throwRotate = null;

        [SerializeField, Tooltip("投擲速度")]
        private float _throwSpeed = 550f;

        [SerializeField, Tooltip("着弾時間（秒）")]
        private float _impactSec = 1.0f;

        [SerializeField, Tooltip("投擲時の重さ")]
        private float _weight = 450f;

        [SerializeField, Tooltip("スタン状態の時間（秒）")]
        private float _stunSec = 9.0f;

        public bool UseItem(GameObject drone)
        {
            // ドローンの座標と向きでスタングレネードを生成
            Transform _throwerPos = drone.transform;
            StunGrenade grenade = Instantiate(_throwObject, _throwerPos.position, _throwerPos.rotation * _throwRotate.rotation);

            // 投てき処理
            grenade.ThrowGrenade(drone, _throwSpeed, _impactSec, _weight, _stunSec);

            Destroy(gameObject);

            return true;
        }
    }
}