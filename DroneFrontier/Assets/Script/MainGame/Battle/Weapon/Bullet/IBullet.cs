using UnityEngine;

namespace Battle.Weapon.Bullet
{
    public interface IBullet
    {
        /// <summary>
        /// 弾丸発射元オブジェクト
        /// </summary>
        GameObject Shooter { get; }

        /// <summary>
        /// 弾丸発射
        /// </summary>
        /// <param name="shooter">弾丸発射元オブジェクト</param>
        /// <param name="damage">ダメージ量</param>
        /// <param name="speed">弾速</param>
        /// <param name="trackingPower">追従力</param>
        /// <param name="target">追従先オブジェクト</param>
        void Shot(GameObject shooter, float damage, float speed, float trackingPower = 0, GameObject target = null);
    }
}