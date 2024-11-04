using System;
using UnityEngine;

namespace Offline
{
    public class JammingItem : MonoBehaviour, IDroneItem
    {
        /// <summary>
        /// ジャミングボット生成直後の移動時間
        /// </summary>
        private const int BOT_MOVE_TIME = 1;

        /// <summary>
        /// ジャミングボット生成直後の移動量
        /// </summary>
        private const int BOT_MOVE_VALUE = 60;

        [SerializeField, Tooltip("生成するジャミングボット")] 
        private JammingBot _jammingBot = null;

        [SerializeField, Tooltip("ジャミングボットの生存時間（秒）")] 
        private float _jammingBotSec = 60.0f;

        /// <summary>
        /// 生成したジャミングボット
        /// </summary>
        private JammingBot _createdBot = null;

        /// <summary>
        /// ジャミングボットのRigidBody
        /// </summary>
        private Rigidbody _botRigidBody = null;

        /// <summary>
        /// ジャミングボット生成直後の移動時間計測
        /// </summary>
        private float _botMoveTimer = 0;

        public bool UseItem(GameObject drone)
        {
            // ジャミングボット生成
            Transform createrPos = drone.transform;
            _createdBot = Instantiate(_jammingBot, createrPos.position, Quaternion.identity);
            _botRigidBody = _createdBot.GetComponent<Rigidbody>();

            // ジャミングボット生成オブジェクト設定
            _createdBot.Creater = drone;

            // ジャミングボット破壊イベント設定
            _createdBot.DestroyEvent += JammingBotDestroy;

            // 時間経過でジャミングボット破壊
            Destroy(_createdBot.gameObject, _jammingBotSec);

            return true;
        }

        private void FixedUpdate()
        {
            // ジャミングボットの移動が終わった場合は処理しない
            if (_botMoveTimer > BOT_MOVE_TIME) return;

            // ジャミングボットを生成していない場合は処理しない
            if (Useful.IsNullOrDestroyed(_createdBot?.gameObject)) return;

            _botRigidBody.AddForce(Vector3.up * BOT_MOVE_VALUE, ForceMode.Acceleration);

            // 移動時間計測
            _botMoveTimer += Time.deltaTime;
            if (_botMoveTimer > BOT_MOVE_TIME)
            {
                _botRigidBody.isKinematic = true;
            }
        }

        /// <summary>
        /// ジャミングボット破壊イベント
        /// </summary>
        /// <param name="o">イベントオブジェクト</param>
        /// <param name="e">イベント引数</param>
        private void JammingBotDestroy(object o, EventArgs e)
        {
            // イベント削除
            _createdBot.DestroyEvent -= JammingBotDestroy;

            Destroy(gameObject);
        }
    }
}