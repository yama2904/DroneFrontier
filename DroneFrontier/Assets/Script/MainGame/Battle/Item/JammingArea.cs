using Common;
using Drone.Battle;
using System.Collections.Generic;
using UnityEngine;

public class JammingArea : MonoBehaviour
{
    public GameObject Creater { get; set; } = null;

    /// <summary>
    /// �e�I�u�W�F�N�g�ɕt�^�����W���~���O�X�e�[�^�X
    /// </summary>
    private Dictionary<GameObject, JammingStatus> _jammingStatuses = new Dictionary<GameObject, JammingStatus>();

    private void OnDestroy()
    {
        foreach (JammingStatus status in _jammingStatuses.Values)
        {
            status.EndJamming();
        }
        _jammingStatuses.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        // �W���~���O�{�b�g�𐶐������I�u�W�F�N�g���g�Ȃ珈�����Ȃ�
        if (other.gameObject == Creater) return;

        // ���ɃW���~���O�t�^�ς݂̏ꍇ�͏������Ȃ�
        if (_jammingStatuses.ContainsKey(other.gameObject)) return;

        // �v���C���[��CPU�̂ݏ���
        string tag = other.tag;
        if (tag != TagNameConst.PLAYER && tag != TagNameConst.CPU) return;

        // �W���~���O�X�e�[�^�X�t�^
        JammingStatus status = new JammingStatus();
        other.GetComponent<DroneStatusComponent>().AddStatus(status, 9999);
        _jammingStatuses.Add(other.gameObject, status);
    }

    private void OnTriggerExit(Collider other)
    {
        // �W���~���O����
        if (_jammingStatuses.ContainsKey(other.gameObject))
        {
            _jammingStatuses[other.gameObject].EndJamming();
            _jammingStatuses.Remove(other.gameObject);
        }
    }
}
