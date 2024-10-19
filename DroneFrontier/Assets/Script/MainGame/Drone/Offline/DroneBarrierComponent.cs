using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

namespace Offline
{
    public class DroneBarrierComponent : MonoBehaviour
    {
        /// <summary>
        /// バリアの残りHP
        /// </summary>
        public float HP { get; private set; }

        /// <summary>
        /// バリアの最大透過度
        /// </summary>
        private const float BARRIER_MAX_ALFA_COLOR = 0.5f;

        [SerializeField, Tooltip("バリアオブジェクト")]
        private GameObject _barrierObject = null;

        [SerializeField, Tooltip("バリアの最大HP")]
        private float _barrierMaxHP = 100f;

        [SerializeField, Tooltip("バリアが回復し始める時間（秒）")]
        private float _regeneStartTime = 8.0f;

        [SerializeField, Tooltip("回復間隔（秒）")]
        private float _regeneInterval = 1.0f;

        [SerializeField, Tooltip("回復量")]
        private float _regeneValue = 5.0f;

        [SerializeField, Tooltip("バリア破壊後の復活時間（秒）")]
        private float _resurrectBarrierTime = 15.0f;

        [SerializeField, Tooltip("バリア復活時のHP")]
        private float _resurrectBarrierHP = 10.0f;

        /// <summary>
        /// バリアを張っているドローン
        /// </summary>
        private IBattleDrone _drone = null;

        /// <summary>
        /// バリアの色を設定するマテリアル
        /// </summary>
        private Material _material = null;

        /// <summary>
        /// バリア回復用タイマー
        /// </summary>
        private float _regeneTimer = 0;

        /// <summary>
        /// バリア回復中であるか
        /// </summary>
        private bool _isRegening = false;

        /// <summary>
        /// バリア強化中であるか
        /// </summary>
        private bool _isStrengthen = false;

        /// <summary>
        /// バリア強化中のダメージ軽減率（0～1）
        /// </summary>
        private float _strengthenValue = 0f;

        /// <summary>
        /// バリア弱体化中であるか
        /// </summary>
        private bool _isWeak = false;

        /// <summary>
        /// キャンセルトークン発行クラス
        /// </summary>
        private CancellationTokenSource _cancel = new CancellationTokenSource();

        //サウンド
        DroneSoundAction soundAction = null;

        /// <summary>
        /// バリアにダメージを与える
        /// </summary>
        /// <param name="power">与えるダメージ量</param>
        public void Damage(float power)
        {
            // バリアが破壊されている場合は何もしない
            if (HP <= 0) return;

            // バリア強化中の場合はダメージ軽減
            float damage = power;
            if (_isStrengthen)
            {
                damage = power * _strengthenValue;
            }

            // 小数点第2以下切り捨てで計算
            HP -= Useful.Floor(damage, 1);

            // HPが残っている場合はダメージSE再生
            if (HP > 0)
            {
                soundAction.PlayOneShot(SoundManager.SE.BARRIER_DAMAGE, SoundManager.SEVolume * 0.7f);
            }
            else
            {
                // 破壊された場合はバリア破壊SE
                HP = 0;
                soundAction.PlayOneShot(SoundManager.SE.DESTROY_BARRIER, SoundManager.SEVolume);
                Debug.Log($"{_drone.Name}:バリア破壊");
            }

            // バリア回復停止
            _regeneTimer = 0;
            _isRegening = false;

            // バリアの色更新
            ChangeBarrierColor();

            Debug.Log($"{_drone.Name}:バリアに{damage}のダメージ\n残りHP:{HP}");
        }

        /// <summary>
        /// バリア強化実行
        /// </summary>
        /// <param name="damageDown">ダメージ軽減率（0～1）</param>
        /// <param name="time">強化時間（秒）</param>
        /// <returns>強化に成功した場合はtrue</returns>
        public bool StrengthenBarrier(float damageDown, int time)
        {
            // バリアが破壊されている場合は失敗
            if (HP <= 0) return false;

            // 既に強化中の場合は失敗
            if (_isStrengthen) return false;

            // バリア弱体化中の場合は失敗
            if (_isWeak) return false;

            // 強化フラグを立てる
            _isStrengthen = true;

            // ダメージ軽減率保存
            _strengthenValue = damageDown;

            // バリアの色変更
            ChangeBarrierColor();

            // 強化終了タイマー開始
            UniTask.Void(async () =>
            {
                await UniTask.Delay(TimeSpan.FromSeconds(time), cancellationToken: _cancel.Token);
                _isStrengthen = false;
                ChangeBarrierColor();
            });

            return true;
        }

        /// <summary>
        /// バリア弱体化実行
        /// </summary>
        /// <param name="time">弱体化時間（秒）</param>
        /// <returns>弱体化に成功した場合はtrue</returns>
        public bool WeakBarrier(int time)
        {
            // 既に弱体化中の場合は失敗
            if (_isWeak) return false;

            // バリア強化中の場合は強化解除
            if (_isStrengthen)
            {
                _cancel.Cancel();
                _isStrengthen = false;
            }
            else
            {
                // 強化中でない場合はHPを半分にする
                HP = Useful.Floor(HP * 0.5f, 1);
            }

            // バリアの色更新
            ChangeBarrierColor();

            // バリア回復停止
            _regeneTimer = 0;
            _isRegening = false;

            // 弱体化フラグを立てる
            _isWeak = true;

            // 弱体化終了タイマー開始
            UniTask.Void(async () =>
            {
                await UniTask.Delay(TimeSpan.FromSeconds(time), cancellationToken: _cancel.Token);
                _isWeak = false;

                // バリアが破壊されている場合は修復
                if (HP <= 0)
                {
                    ResurrectBarrier(_resurrectBarrierHP);
                }
            });

            return true;
        }

        private void Awake()
        {
            // 各コンポーネント取得
            _drone = GetComponent<IBattleDrone>();
            soundAction = GetComponent<DroneSoundAction>();
            _material = _barrierObject.GetComponent<Renderer>().material;

            // HP初期化
            HP = _barrierMaxHP;

            // ドローンが破壊された場合は本コンポーネントを停止
            _drone.DroneDestroyEvent += (o, e) =>
            {
                HP = 0;
                ChangeBarrierColor();
                enabled = false;
            };
        }

        private void Start()
        {
            ChangeBarrierColor();
        }

        private void Update()
        {
            // バリア弱体化中は回復処理を行わない
            if (_isWeak) return;

            // HPが減っている場合は回復処理
            if (HP > 0 && HP < _barrierMaxHP)
            {
                // 回復中の場合は一定間隔ごとに回復
                if (_isRegening)
                {
                    if (_regeneTimer >= _regeneInterval)
                    {
                        // HP回復
                        float hp = HP + _regeneValue;
                        HP = hp >= _barrierMaxHP ? _barrierMaxHP : hp;  // 最大超過分を調整

                        // バリアの色更新
                        ChangeBarrierColor();

                        // 回復タイマーリセット
                        _regeneTimer = 0;

                        Debug.Log($"{_drone.Name}:バリア回復後HP->{HP}");
                    }
                }
                else
                {
                    // 回復中でない場合は回復が開始するまで待つ
                    if (_regeneTimer >= _regeneStartTime)
                    {
                        // 回復開始
                        _isRegening = true;
                        Debug.Log($"{_drone.Name}:バリア回復開始");
                    }
                }
            }

            // バリアが破壊されている場合はバリア復活まで待つ
            if (HP <= 0)
            {
                if (_regeneTimer >= _resurrectBarrierTime)
                {
                    // バリア復活
                    ResurrectBarrier(_resurrectBarrierHP);
                    _regeneTimer = 0;

                    Debug.Log($"{_drone.Name}:バリア復活");
                }
            }

            _regeneTimer += Time.deltaTime;
        }

        /// <summary>
        /// バリアを復活させる
        /// </summary>
        /// <param name="resurrectHP">復活時のHP</param>
        private void ResurrectBarrier(float resurrectHP)
        {
            // 修復したら回復処理に移る
            HP = resurrectHP;
            _isRegening = true;

            // バリアの色更新
            ChangeBarrierColor();

            Debug.Log("バリア復活");
        }

        /// <summary>
        /// 残りHPに応じてバリアの色を変更する
        /// </summary>
        private void ChangeBarrierColor()
        {
            // 残りHPの割合で色合いを変化
            float value = HP / _barrierMaxHP;

            if (!_isStrengthen)
            {
                _material.color = new Color(1 - value, value, 0, value * BARRIER_MAX_ALFA_COLOR);
            }
            else
            {
                _material.color = new Color(1 - value, 0, value, value * BARRIER_MAX_ALFA_COLOR);
            }
        }
    }
}