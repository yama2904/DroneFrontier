using System;

/// <summary>
/// �o�g�����[�h�̃h���[������������C���^�[�t�F�[�X
/// </summary>
public interface IBattleDrone
{
    // <summary>
    /// �h���[���̖��O
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// �h���[����HP
    /// </summary>
    public float HP { get; }

    /// <summary>
    /// ���݂̃X�g�b�N��
    /// </summary>
    public int StockNum { get; set; }

    /// <summary>
    /// �T�u����
    /// </summary>
    public WeaponType SubWeapon { get; set; }

    /// <summary>
    /// ������
    /// </summary>
    public void Initialize();

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