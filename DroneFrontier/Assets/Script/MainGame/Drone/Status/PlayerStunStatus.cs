using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class PlayerStunStatus : IDroneStatusChange

{
    public StatusChangeType StatusType => StatusChangeType.Stun;

    public Image IconImage => null;

    public event EventHandler StatusEndEvent;

    private FadeoutImage _createdMask;

    public bool Invoke(GameObject drone, float statusSec, params object[] addParams)
    {
        Addressables.LoadAssetAsync<GameObject>("StunMask").Completed += handle =>
        {
            _createdMask = UnityEngine.Object.Instantiate(handle.Result).GetComponent<FadeoutImage>();
            _createdMask.FadeoutSec = statusSec;
            _createdMask.FadeoutEndEvent += FadeoutEndEvent;
            Addressables.Release(handle);
        };
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
        UnityEngine.Object.Destroy(_createdMask);
    }
}
