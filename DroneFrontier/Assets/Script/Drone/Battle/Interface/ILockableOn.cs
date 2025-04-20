using System.Collections.Generic;
using UnityEngine;

namespace Drone.Battle
{
    /// <summary>
    /// 実装したオブジェクトをロックオン可能とするインターフェース
    /// </summary>
    public interface ILockableOn
    {
        /// <summary>
        /// ロックオン可能であるか
        /// </summary>
        public bool IsLockableOn { get; }

        /// <summary>
        /// 実装オブジェクトをロックオン不可とするオブジェクト
        /// </summary>
        public List<GameObject> NotLockableOnList { get; }
    }
}