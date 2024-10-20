using UnityEngine;

public class DroneRotateComponent : MonoBehaviour
{
    [SerializeField, Tooltip("�ړ����ɉ�]������I�u�W�F�N�g")]
    private Transform _rotateObject = null;

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
