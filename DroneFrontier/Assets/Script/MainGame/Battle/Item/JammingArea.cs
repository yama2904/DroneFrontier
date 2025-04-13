using System.Collections.Generic;
using UnityEngine;

public class JammingArea : MonoBehaviour
{
    public GameObject Creater { get; set; } = null;

    /// <summary>
    /// �e�I�u�W�F�N�g�ɕt�^�����W���~���O�X�e�[�^�X
    /// </summary>
    private Dictionary<GameObject, JammingStatus> _addedJammingStatusMap = new Dictionary<GameObject, JammingStatus>();

    private void OnDestroy()
    {
        foreach (JammingStatus status in _addedJammingStatusMap.Values)
        {
            status.EndJamming();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // �W���~���O�{�b�g�𐶐������I�u�W�F�N�g���g�Ȃ珈�����Ȃ�
        if (other.gameObject == Creater) return;

        // ���ɃW���~���O�t�^�ς݂̏ꍇ�͏������Ȃ�
        if (_addedJammingStatusMap.ContainsKey(other.gameObject)) return;

        // �v���C���[��CPU�̂ݏ���
        string tag = other.tag;
        if (tag != TagNameConst.PLAYER && tag != TagNameConst.CPU) return;

        // �W���~���O�X�e�[�^�X�t�^
        JammingStatus status = new JammingStatus();
        other.GetComponent<DroneStatusComponent>().AddStatus(status, 9999);
        _addedJammingStatusMap.Add(other.gameObject, status);
    }

    private void OnTriggerExit(Collider other)
    {
        // �W���~���O����
        if (_addedJammingStatusMap.ContainsKey(other.gameObject))
        {
            _addedJammingStatusMap[other.gameObject].EndJamming();
            _addedJammingStatusMap.Remove(other.gameObject);
        }
    }
}
