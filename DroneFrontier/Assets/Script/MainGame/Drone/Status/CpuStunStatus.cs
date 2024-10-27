using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.UI;

public class CpuStunStatus : IDroneStatus
{
    public Image IconImage => null;

    public event EventHandler StatusEndEvent;

    public bool Invoke(GameObject drone, float statusSec, params object[] addParams)
    {
        // �X�^���̊ԃ��b�N�I���@�\��~
        DroneLockOnComponent lockon = drone.GetComponent<DroneLockOnComponent>();
        lockon.enabled = false;

        // �X�^���I���^�C�}�[�ݒ�
        UniTask.Void(async () =>
        {
            await UniTask.Delay(TimeSpan.FromSeconds(statusSec));
            lockon.enabled = true;
        });

        return true;
    }
}
