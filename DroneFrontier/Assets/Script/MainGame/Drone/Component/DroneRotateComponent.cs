using UnityEngine;

public class DroneRotateComponent : MonoBehaviour
{
    [SerializeField, Tooltip("移動時に回転させるオブジェクト")]
    private Transform _rotateObject = null;

    /// <summary>
    /// 指定した角度と回転量でドローンを回転
    /// </summary>
    /// <param name="rotate">回転先角度</param>
    /// <param name="value">回転量（0～1）</param>
    public void Rotate(Quaternion rotate, float value)
    {
        _rotateObject.localRotation = Quaternion.Slerp(_rotateObject.localRotation, rotate, value);
    }
}
