using Common;
using Cysharp.Threading.Tasks;
using Drone;
using Drone.Battle;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace Battle.Status
{
    public class SpeedDownStatus : IDroneStatusChange
    {
        public event EventHandler OnStatusEnd;

        /// <summary>
        /// �X�s�[�h�_�E���t�^�����I�u�W�F�N�g��Move�R���|�[�l���g
        /// </summary>
        private DroneMoveComponent _move = null;

        /// <summary>
        /// �X�s�[�h�_�E���t�^�����I�u�W�F�N�g��Sound�R���|�[�l���g
        /// </summary>
        private DroneSoundComponent _sound = null;

        /// <summary>
        /// �Đ�����SE�ԍ�
        /// </summary>
        private int _seId = 0;

        /// <summary>
        /// ���s�����ړ����x�ύXID
        /// </summary>
        private int _changeSpeedId = -1;

        /// <summary>
        /// �L�����Z���g�[�N�����s�N���X
        /// </summary>
        private CancellationTokenSource _cancel = new CancellationTokenSource();

        public Image InstantiateIcon()
        {
            return Addressables.InstantiateAsync("SpeedDownIcon").WaitForCompletion().GetComponent<Image>();
        }

        public bool Invoke(GameObject drone, float statusSec, params object[] addParams)
        {
            // �R���|�[�l���g�擾
            _move = drone.GetComponent<DroneMoveComponent>();
            _sound = drone.GetComponent<DroneSoundComponent>();

            // �X�s�[�h�_�E�����ʕt�^
            _changeSpeedId = _move.ChangeMoveSpeedPercent(1 - (float)addParams[0]);

            // �X�s�[�h�_�E��SE�Đ�
            if (_sound != null)
            {
                _seId = _sound.Play(SoundManager.SE.MagneticArea, 1, true);
            }

            // �X�s�[�h�_�E���I���^�C�}�[�ݒ�
            UniTask.Void(async () =>
            {
                await UniTask.Delay(TimeSpan.FromSeconds(statusSec), cancellationToken: _cancel.Token);
                EndSpeedDown();
            });

            return true;
        }

        /// <summary>
        /// �X�s�[�h�_�E���I��
        /// </summary>
        public void EndSpeedDown()
        {
            // �X�s�[�h�_�E���I��
            _move.ResetMoveSpeed(_changeSpeedId);
            _sound?.StopSE(_seId);

            // �X�s�[�h�_�E���I���^�C�}�[��~
            _cancel.Cancel();

            // �I���C�x���g����
            OnStatusEnd?.Invoke(this, EventArgs.Empty);
        }
    }
}