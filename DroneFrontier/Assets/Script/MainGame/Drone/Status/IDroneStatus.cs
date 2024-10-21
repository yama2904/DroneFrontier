using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// �h���[���X�e�[�^�X�ύX�C���^�[�t�F�[�X
/// </summary>
public interface IDroneStatus
{
    /// <summary>
    /// ��ԕω��̃A�C�R��
    /// </summary>
    Image IconImage { get; }

    /// <summary>
    /// �X�e�[�^�X�ω����s
    /// </summary>
    /// <param name="drone">�X�e�[�^�X��ω�������h���[��</param>
    /// <param name="parameters">�p�����[�^</param>
    /// <returns>true:����, false:���s</returns>
    bool Invoke(GameObject drone, params object[] parameters);

    /// <summary>
    /// �X�e�[�^�X�ω��I���C�x���g
    /// </summary>
    public event EventHandler StatusEndEvent;
}
