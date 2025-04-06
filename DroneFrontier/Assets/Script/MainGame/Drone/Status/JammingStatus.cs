using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class JammingStatus : IDroneStatusChange
{
    public StatusChangeType StatusType => StatusChangeType.Jamming;

    public event EventHandler StatusEndEvent;

    /// <summary>
    /// �W���~���O�t�^�����I�u�W�F�N�g��LockOn�R���|�[�l���g
    /// </summary>
    private DroneLockOnComponent _lockon = null;

    /// <summary>
    /// �W���~���O�t�^�����I�u�W�F�N�g��Radar�R���|�[�l���g
    /// </summary>
    private DroneRadarComponent _radar = null;

    /// <summary>
    /// �W���~���O�t�^�����I�u�W�F�N�g��Sound�R���|�[�l���g
    /// </summary>
    private DroneSoundComponent _sound = null;

    /// <summary>
    /// �Đ�����SE�ԍ�
    /// </summary>
    private int _seId = 0;

    /// <summary>
    /// �L�����Z���g�[�N�����s�N���X
    /// </summary>
    private CancellationTokenSource _cancel = new CancellationTokenSource();

    public Image InstantiateIcon()
    {
        return Addressables.InstantiateAsync("JammigUI").WaitForCompletion().GetComponent<Image>();
    }

    public bool Invoke(GameObject drone, float statusSec, params object[] addParams)
    {
        // �R���|�[�l���g�擾
        _lockon = drone.GetComponent<DroneLockOnComponent>();
        _radar = drone.GetComponent<DroneRadarComponent>();
        _sound = drone.GetComponent<DroneSoundComponent>();

        // �W���~���O���ʕt�^
        _lockon?.QueueDisabled();
        _radar?.QueueDisabled();

        // �W���~���OSE�Đ�
        if (_sound != null)
        {
            _seId = _sound.PlayLoopSE(SoundManager.SE.JAMMING_NOISE);
        }

        // �W���~���O�I���^�C�}�[�ݒ�
        UniTask.Void(async () =>
        {
            await UniTask.Delay(TimeSpan.FromSeconds(statusSec), cancellationToken: _cancel.Token);
            EndJamming();
        });

        return true;
    }
    
    /// <summary>
    /// �W���~���O�I��
    /// </summary>
    public void EndJamming()
    {
        // �W���~���O�I��
        _lockon?.DequeueDisabled();
        _radar?.DequeueDisabled();
        _sound?.StopLoopSE(_seId);
        
        // �W���~���O�I���^�C�}�[��~
        _cancel.Cancel();

        // �I���C�x���g����
        StatusEndEvent?.Invoke(this, EventArgs.Empty);
    }
}
