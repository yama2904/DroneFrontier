using UnityEngine;

public interface IGameItem
{
    /// <summary>
    /// �A�C�e���g�p
    /// </summary>
    /// <param name="drone">�A�C�e�����g�p����h���[��</param>
    /// <returns>true:����, false:���s</returns>
    bool UseItem(GameObject drone);
}
