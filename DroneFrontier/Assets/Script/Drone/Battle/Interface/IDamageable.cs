using UnityEngine;

namespace Drone.Battle
{
    /// <summary>
    /// ���������I�u�W�F�N�g���_���[�W�\�Ƃ���C���^�[�t�F�[�X
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// �����I�u�W�F�N�g�̏��L��
        /// </summary>
        GameObject Owner { get; }

        /// <summary>
        /// �_���[�W�C�x���g
        /// </summary>
        event DamageHandler OnDamage;

        /// <summary>
        /// ���������I�u�W�F�N�g�Ƀ_���[�W��^����
        /// </summary>
        /// <param name="source">�_���[�W���I�u�W�F�N�g</param>
        /// <param name="value">�_���[�W��</param>
        /// <returns>�_���[�W�ɐ��������ꍇ��true</returns>
        bool Damage(GameObject source, float value);
    }

    /// <summary>
    /// �_���[�W�n���h���[
    /// </summary>
    /// <param name="sender">�C�x���g�I�u�W�F�N�g</param>
    /// <param name="source">�_���[�W��^�����I�u�W�F�N�g</param>
    /// <param name="damage">�_���[�W��</param>
    public delegate void DamageHandler(IDamageable sender, GameObject source, float damage);
}