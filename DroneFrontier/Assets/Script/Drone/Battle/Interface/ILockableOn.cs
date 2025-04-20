using System.Collections.Generic;
using UnityEngine;

namespace Drone.Battle
{
    /// <summary>
    /// ���������I�u�W�F�N�g�����b�N�I���\�Ƃ���C���^�[�t�F�[�X
    /// </summary>
    public interface ILockableOn
    {
        /// <summary>
        /// ���b�N�I���\�ł��邩
        /// </summary>
        public bool IsLockableOn { get; }

        /// <summary>
        /// �����I�u�W�F�N�g�����b�N�I���s�Ƃ���I�u�W�F�N�g
        /// </summary>
        public List<GameObject> NotLockableOnList { get; }
    }
}