using UnityEngine;

/// <summary>
/// ���������I�u�W�F�N�g���_���[�W�\�Ƃ���C���^�[�t�F�[�X
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// ���������I�u�W�F�N�g�Ƀ_���[�W��^����
    /// </summary>
    /// <param name="source">�_���[�W���I�u�W�F�N�g</param>
    /// <param name="value">�_���[�W��</param>
    void Damage(GameObject source, float value);
}
