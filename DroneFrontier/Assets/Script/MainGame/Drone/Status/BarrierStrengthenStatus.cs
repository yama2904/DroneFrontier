using Offline;
using System;
using UnityEngine;
using UnityEngine.UI;

public class BarrierStrengthenStatus : IDroneStatus
{
    public Image IconImage => null;

    public event EventHandler StatusEndEvent;

    private DroneBarrierComponent _barrier = null;

    public bool Invoke(GameObject drone, float statusSec, params object[] addParams)
    {
        // �p�����[�^�擾
        float damageDownPercent = (float)addParams[0];

        // �o���A�������s
        _barrier = drone.GetComponent<DroneBarrierComponent>();
        bool success = _barrier.StrengthenBarrier(damageDownPercent, statusSec);

        // �����Ɏ��s�����ꍇ�͎��s�ŕԂ�
        if (!success) return false;

        // �������ԏI�����ɃX�e�[�^�X�ω��I���C�x���g����
        _barrier.StrengthenEndEvent += StrengthenEndEvent;

        return true;
    }

    /// <summary>
    /// �o���A�����I���C�x���g
    /// </summary>
    /// <param name="o">�C�x���g�I�u�W�F�N�g</param>
    /// <param name="e">�C�x���g����</param>
    private void StrengthenEndEvent(object o, EventArgs e)
    {
        StatusEndEvent?.Invoke(this, EventArgs.Empty);
        _barrier.StrengthenEndEvent -= StrengthenEndEvent;
    }
}