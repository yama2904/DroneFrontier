using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// �h���[�����g�p�\�ȃA�C�e������������C���^�[�t�F�[�X
/// </summary>
public interface IDroneItem
{
    /// <summary>
    /// �A�C�e���̏����A�C�R������
    /// </summary>
    /// <returns>���������A�C�R��</returns>
    Image InstantiateIcon();

    /// <summary>
    /// �A�C�e���g�p
    /// </summary>
    /// <param name="drone">�A�C�e�����g�p����h���[��</param>
    /// <returns>true:����, false:���s</returns>
    bool UseItem(GameObject drone);
}
