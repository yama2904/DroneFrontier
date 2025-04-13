using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ���������I�u�W�F�N�g�����[�_�[�Ǝˉ\�Ƃ���C���^�[�t�F�[�X
/// </summary>
public interface IRadarable
{
    /// <summary>
    /// �I�u�W�F�N�g�^�C�v
    /// </summary>
    public enum ObjectType
    {
        /// <summary>
        /// �G
        /// </summary>
        Enemy,

        /// <summary>
        /// �A�C�e��
        /// </summary>
        Item
    }

    /// <summary>
    /// ���[�_�[�Ǝ˂��ꂽ�ۂɕԂ��I�u�W�F�N�g�^�C�v
    /// </summary>
    public ObjectType Type { get; }

    /// <summary>
    /// ���[�_�[�Ǝˉ\�ł��邩
    /// </summary>
    public bool IsRadarable { get; }

    /// <summary>
    /// ���[�_�[�Ǝ˕s�I�u�W�F�N�g
    /// </summary>
    public List<GameObject> NotRadarableList { get; }
}