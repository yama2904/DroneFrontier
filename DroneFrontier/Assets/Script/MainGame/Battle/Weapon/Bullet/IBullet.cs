using UnityEngine;

namespace Battle.Weapon.Bullet
{
    public interface IBullet
    {
        /// <summary>
        /// �e�۔��ˌ��I�u�W�F�N�g
        /// </summary>
        GameObject Shooter { get; }

        /// <summary>
        /// �e�۔���
        /// </summary>
        /// <param name="shooter">�e�۔��ˌ��I�u�W�F�N�g</param>
        /// <param name="damage">�_���[�W��</param>
        /// <param name="speed">�e��</param>
        /// <param name="trackingPower">�Ǐ]��</param>
        /// <param name="target">�Ǐ]��I�u�W�F�N�g</param>
        void Shot(GameObject shooter, float damage, float speed, float trackingPower = 0, GameObject target = null);
    }
}