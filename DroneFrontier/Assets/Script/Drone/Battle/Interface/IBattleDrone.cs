using System;
using UnityEngine;

namespace Drone.Battle
{
    /// <summary>
    /// �o�g�����[�h�̃h���[������������C���^�[�t�F�[�X
    /// </summary>
    public interface IBattleDrone
    {
        // <summary>
        /// �h���[���̖��O
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// �h���[����HP
        /// </summary>
        public float HP { get; }

        /// <summary>
        /// ���C������
        /// </summary>
        public IWeapon MainWeapon { get; }

        /// <summary>
        /// �T�u����
        /// </summary>
        public IWeapon SubWeapon { get; }

        /// <summary>
        /// ���݂̃X�g�b�N��
        /// </summary>
        public int StockNum { get; }

        /// <summary>
        /// UI�\��Canvas
        /// </summary>
        public Canvas Canvas { get; }

        /// <summary>
        /// �e��UI�\��Canvas
        /// </summary>
        public Canvas BulletCanvas { get; }

        /// <summary>
        /// ������
        /// </summary>
        /// <param name="name">�h���[����</param>
        /// <param name="mainWeapon">���C���E�F�|��</param>
        /// <param name="subWeapon">�T�u�E�F�|��</param>
        /// <param name="stock">�X�g�b�N��</param>
        public void Initialize(string name, IWeapon mainWeapon, IWeapon subWeapon, int stock);

        /// <summary>
        /// �h���[���Ƀ_���[�W��^����
        /// </summary>
        /// <param name="value">�_���[�W��</param>
        public void Damage(float value);

        /// <summary>
        /// �h���[���j��C�x���g
        /// </summary>
        public event EventHandler DroneDestroyEvent;
    }
}