using Cysharp.Threading.Tasks;
using Drone.Battle;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace Battle.Status
{
    public class StunStatus : IDroneStatusChange
    {
        public event EventHandler OnStatusEnd;

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
                    _createdMask.OnFadeoutEnd += OnFadeoutEnd;
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
                    OnStatusEnd?.Invoke(this, EventArgs.Empty);
                });
            }
            return true;
        }

        /// <summary>
        /// �t�F�[�h�A�E�g�I���C�x���g
        /// </summary>
        /// <param name="o">�C�x���g�I�u�W�F�N�g</param>
        /// <param name="e">�C�x���g����</param>
        private void OnFadeoutEnd(object o, EventArgs e)
        {
            // �X�e�[�^�X�I���C�x���g����
            OnStatusEnd?.Invoke(this, EventArgs.Empty);

            // �C�x���g�폜���ăI�u�W�F�N�g�j��
            _createdMask.OnFadeoutEnd -= OnFadeoutEnd;
            UnityEngine.Object.Destroy(_createdMask.gameObject);
        }
    }
}