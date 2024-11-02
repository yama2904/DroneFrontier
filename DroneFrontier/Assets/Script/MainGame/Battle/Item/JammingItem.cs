using Cysharp.Threading.Tasks;
using System;
using System.Threading;
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

        [SerializeField, Tooltip("ジャミングボットの生存時間")] 
        private float _jammingBotTime = 60.0f;

        /// <summary>
        /// 生成したジャミングボット
        /// </summary>
        private JammingBot _createdBot = null;

        /// <summary>
        /// ジャミングボットのRigidBody
        /// </summary>
        private Rigidbody _botRigidBody = null;

        /// <summary>
        /// キャンセルトークン発行クラス
        /// </summary>
        private CancellationTokenSource _cancel = new CancellationTokenSource();

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
            UniTask.Void(async () =>
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_jammingBotTime), cancellationToken: _cancel.Token);
                Destroy(_createdBot.gameObject);
                Destroy(gameObject);
            });

            return true;
        }

        private void FixedUpdate()
        {
            // ジャミングボットを生成していない場合は処理しない
            if (_createdBot == null) return;

            // ジャミングボットの移動が終わった場合は処理しない
            if (_botMoveTimer > BOT_MOVE_TIME) return;

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
            // 時間経過によるジャミングボット破壊を停止
            _cancel.Cancel();

            // イベント削除
            _createdBot.DestroyEvent -= JammingBotDestroy;

            Destroy(gameObject);
        }
    }
}