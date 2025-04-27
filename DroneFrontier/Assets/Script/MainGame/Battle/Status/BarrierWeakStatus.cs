using Drone.Battle;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace Battle.Status
{
    public class BarrierWeakStatus : IDroneStatusChange
    {
        public event EventHandler StatusEndEvent;

        private DroneBarrierComponent _barrier = null;

        public Image InstantiateIcon()
        {
            return Addressables.InstantiateAsync("BarrierWeakIcon").WaitForCompletion().GetComponent<Image>();
        }

        public bool Invoke(GameObject drone, float statusSec, params object[] addParams)
        {
            // �o���A��̉����s
            _barrier = drone.GetComponent<DroneBarrierComponent>();
            bool success = _barrier.WeakBarrier(statusSec);

            // ��̉��Ɏ��s�����ꍇ�͎��s�ŕԂ�
            if (!success) return false;

            // ��̉����ԏI�����ɃX�e�[�^�X�ω��I���C�x���g����
            _barrier.WeakEndEvent += WeakEndEvent;

            return true;
        }

        /// <summary>
        /// �o���A��̉��I���C�x���g
        /// </summary>
        /// <param name="o">�C�x���g�I�u�W�F�N�g</param>
        /// <param name="e">�C�x���g����</param>
        private void WeakEndEvent(object o, EventArgs e)
        {
            StatusEndEvent?.Invoke(this, EventArgs.Empty);
            _barrier.WeakEndEvent -= WeakEndEvent;
        }
    }
}