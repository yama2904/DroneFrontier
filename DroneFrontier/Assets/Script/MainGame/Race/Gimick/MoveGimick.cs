using UnityEngine;

public class MoveGimick : MonoBehaviour
{
    private enum Dir
    {
        DirX,
        DirY,
        DirZ,

        None
    }
    [SerializeField] 
    private Dir _moveDir = Dir.None;

    [SerializeField] 
    float _speed = 1f;

    [SerializeField, Tooltip("移動距離")] 
    private float _range = 7.5f;

    /// <summary>
    /// 初期座標
    /// </summary>
    Vector3 _initPos;

    // コンポーネントキャッシュ
    Transform _transform = null;

    private void Awake()
    {
        _transform = transform;
        _initPos = _transform.position;
    }

    private void FixedUpdate()
    {
        if (_moveDir == Dir.DirX)
        {
            _transform.position = new Vector3(_initPos.x + Mathf.PingPong(Time.time * _speed, _range), _initPos.y, _initPos.z);
        }
        if (_moveDir == Dir.DirY)
        {
            _transform.position = new Vector3(_initPos.x, _initPos.y + Mathf.PingPong(Time.time * _speed, _range), _initPos.z);
        }
        if (_moveDir == Dir.DirZ)
        {
            _transform.position = new Vector3(_initPos.x, _initPos.y, _initPos.z + Mathf.PingPong(Time.time * _speed, _range));
        }
    }
}
