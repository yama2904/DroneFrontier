using UnityEngine;

namespace Drone
{
    public class DroneRotateComponent : MonoBehaviour, IDroneComponent
    {
        [SerializeField, Tooltip("��]������I�u�W�F�N�g")]
        private Transform _rotateObject = null;

        public void Initialize() { }

        /// <summary>
        /// �w�肵���p�x�Ɖ�]�ʂŃh���[������]
        /// </summary>
        /// <param name="rotate">��]��p�x</param>
        /// <param name="value">��]�ʁi0�`1�j</param>
        public void Rotate(Quaternion rotate, float value)
        {
            _rotateObject.localRotation = Quaternion.Slerp(_rotateObject.localRotation, rotate, value);
        }
    }
}