using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class StunStatus : IDroneStatusChange
{
    public StatusChangeType StatusType => StatusChangeType.Stun;

    public event EventHandler StatusEndEvent;

    private FadeoutImage _createdMask;

    public Image InstantiateIcon()
    {
        return null;
    }

    public bool Invoke(GameObject drone, float statusSec, params object[] addParams)
    {
        // �v���C���[�̏ꍇ�̓}�X�N����
        if ((bool)addParams[0])
        {
            Addressables.InstantiateAsync("StunMask").Completed += handle =>
            {
                _createdMask = handle.Result.GetComponent<FadeoutImage>();
                _createdMask.FadeoutSec = statusSec;
                _createdMask.FadeoutEndEvent += FadeoutEndEvent;
            };
        }
        else
        {
            // �v���C���[�ȊO�̏ꍇ�̓}�X�N���Ȃ�

            // �X�^���̊ԃ��b�N�I���@�\��~
            DroneLockOnComponent lockon = drone.GetComponent<DroneLockOnComponent>();
            lockon.SetEnableLockOn(false);

            // �X�^���I���^�C�}�[�ݒ�
            UniTask.Void(async () =>
            {
                await UniTask.Delay(TimeSpan.FromSeconds(statusSec));
                lockon.SetEnableLockOn(true);
                StatusEndEvent?.Invoke(this, EventArgs.Empty);
            });
        }
        return true;
    }

    /// <summary>
    /// �t�F�[�h�A�E�g�I���C�x���g
    /// </summary>
    /// <param name="o">�C�x���g�I�u�W�F�N�g</param>
    /// <param name="e">�C�x���g����</param>
    private void FadeoutEndEvent(object o, EventArgs e)
    {
        // �X�e�[�^�X�I���C�x���g����
        StatusEndEvent?.Invoke(this, EventArgs.Empty);

        // �C�x���g�폜���ăI�u�W�F�N�g�j��
        _createdMask.FadeoutEndEvent -= FadeoutEndEvent;
        UnityEngine.Object.Destroy(_createdMask.gameObject);
    }
}
