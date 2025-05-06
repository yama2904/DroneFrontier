using Common;
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

        public Image InstantiateIcon()
        {
            return null;
        }

        public bool Invoke(GameObject drone, float statusSec, params object[] addParams)
        {
            // �v���C���[�̏ꍇ�̓X�^���ɂ��}�X�N����
            if (drone.CompareTag(TagNameConst.PLAYER))
            {
                StunMask mask = Addressables.InstantiateAsync("StunMask").WaitForCompletion().GetComponent<StunMask>();
                mask.OnStunEnd += OnStunEnd;
                mask.Run(drone.GetComponent<IBattleDrone>().Canvas, statusSec);
            }

            // CPU�̏ꍇ�̓��b�N�I����~
            if (drone.CompareTag(TagNameConst.CPU))
            {
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
        /// �X�^���I���C�x���g
        /// </summary>
        /// <param name="sender">�C�x���g�I�u�W�F�N�g</param>
        /// <param name="e">�C�x���g����</param>
        private void OnStunEnd(object sender, EventArgs e)
        {
            // �X�e�[�^�X�I���C�x���g����
            OnStatusEnd?.Invoke(this, EventArgs.Empty);

            // �C�x���g�폜���ăI�u�W�F�N�g�j��
            StunMask mask = sender as StunMask;
            mask.OnStunEnd -= OnStunEnd;
            UnityEngine.Object.Destroy(mask.gameObject);
        }
    }
}