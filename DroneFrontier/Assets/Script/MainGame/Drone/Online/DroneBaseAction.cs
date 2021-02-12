using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace Online
{
    public class DroneBaseAction : NetworkBehaviour
    {
        //コンポーネント用
        Rigidbody _rigidbody = null;
        Transform cacheTransform = null;  //キャッシュ用

        //移動用
        [SerializeField, Tooltip("移動速度")] float moveSpeed = 800;
        public float MoveSpeed { get { return moveSpeed; } }
        float initSpeed = 0;

        //回転用
        [SerializeField] Transform droneObject = null;
        [SerializeField, Tooltip("回転速度")] public float rotateSpeed = 5.0f;
        public float RotateSpeed { get { return rotateSpeed; } }
        float initRotateSpeed = 0;
        [SerializeField, Tooltip("上下の回転制限角度")] float limitCameraTiltX = 40f;

        //カメラ
        [SerializeField] Camera _camera = null;
        public Camera _Camera { get { return _camera; } }


        #region Init

        public override void OnStartClient()
        {
            base.OnStartClient();

            _rigidbody = GetComponent<Rigidbody>();
            cacheTransform = _rigidbody.transform; //キャッシュ用

            //初期値の保存
            initSpeed = moveSpeed;
            initRotateSpeed = rotateSpeed;
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            _camera.depth++;
        }

        void Start() { }

        #endregion


        //移動処理
        public void Move(Vector3 vec)
        {
            _rigidbody.AddForce(vec * moveSpeed + (vec * moveSpeed - _rigidbody.velocity), ForceMode.Force);
        }

        //ドローンを徐々に回転させる
        public void RotateDroneObject(Quaternion rotate, float speed)
        {
            droneObject.localRotation = Quaternion.Slerp(droneObject.localRotation, rotate, speed);
        }

        //回転処理
        public void Rotate(Vector3 angle)
        {
            if (MainGameManager.IsCursorLock)
            {
                angle.x *= rotateSpeed * CameraManager.ReverseX;
                angle.y *= rotateSpeed * CameraManager.ReverseY;

                //カメラの左右回転
                cacheTransform.RotateAround(cacheTransform.position, Vector3.up, angle.x);

                //カメラの上下の回転に制限をかける
                Vector3 localAngle = cacheTransform.localEulerAngles;
                localAngle.x += angle.y * -1;
                if (localAngle.x > limitCameraTiltX && localAngle.x < 180)
                {
                    localAngle.x = limitCameraTiltX;
                }
                if (localAngle.x < 360 - limitCameraTiltX && localAngle.x > 180)
                {
                    localAngle.x = 360 - limitCameraTiltX;
                }
                cacheTransform.localEulerAngles = localAngle;
            }
        }

        //スピードを変更する
        public void ModifySpeed(float speedMgnf)
        {
            moveSpeed *= speedMgnf;
        }
    }
}