using System;
using UnityEngine;

namespace Drone.Battle
{
    public interface IWeapon
    {
        /// <summary>
        /// ���폊�L��
        /// </summary>
        public GameObject Owner { get; }

        /// <summary>
        /// �S�e��[�C�x���g
        /// </summary>
        public event EventHandler OnBulletFull;

        /// <summary>
        /// �c�e�����C�x���g
        /// </summary>
        public event EventHandler OnBulletEmpty;

        /// <summary>
        /// Addressable�̃L�[��
        /// </summary>
        /// <returns></returns>
        public string GetAddressKey();

        /// <summary>
        /// ������
        /// </summary>
        /// <param name="owner">���폊�L��</param>
        public void Initialize(GameObject owner);

        /// <summary>
        /// �e�۔���
        /// </summary>
        /// <param name="target">�Ǐ]��I�u�W�F�N�g</param>
        public void Shot(GameObject target = null);
    }
}