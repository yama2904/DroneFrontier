using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// �h���[���X�e�[�^�X�ύX�C���^�[�t�F�[�X
/// </summary>
public interface IDroneStatusChange
{
    /// <summary>
    /// ��Ԉُ�A�C�R������
    /// </summary>
    /// <returns></returns>
    Image InstantiateIcon();

    /// <summary>
    /// �X�e�[�^�X�ω����s
    /// </summary>
    /// <param name="drone">�X�e�[�^�X��ω�������h���[��</param>
    /// <param name="statusSec">�X�e�[�^�X�ω����ԁi�b�j</param>
    /// <param name="addParams">�ǉ��p�����[�^</param>
    /// <returns>true:����, false:���s</returns>
    bool Invoke(GameObject drone, float statusSec, params object[] addParams);

    /// <summary>
    /// �X�e�[�^�X�ω��I���C�x���g
    /// </summary>
    event EventHandler StatusEndEvent;
}
