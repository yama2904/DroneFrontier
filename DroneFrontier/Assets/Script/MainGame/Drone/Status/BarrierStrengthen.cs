using Cysharp.Threading.Tasks;
using Offline;
using System;
using UnityEngine;

public class BarrierStrengthen : MonoBehaviour, IDroneStatus
{
    public RectTransform IconImage => null;

    public event EventHandler StatusEndEvent;

    public bool Invoke(GameObject drone, params object[] parameters)
    {
        // �p�����[�^�擾
        float damageDownPercent = (float)parameters[0];
        int time = (int)parameters[1];

        // �o���A�������s
        bool success = drone.GetComponent<DroneBarrierComponent>().StrengthenBarrier(damageDownPercent, time);

        // �����Ɏ��s�����ꍇ�͎��s�ŕԂ�
        if (!success) return false;

        // �������ԏI���^�C�}�[�ݒ�
        UniTask.Void(async () =>
        {
            await UniTask.Delay(TimeSpan.FromSeconds(time));
            StatusEndEvent?.Invoke(this, EventArgs.Empty);
        });

        return true;
    }
}