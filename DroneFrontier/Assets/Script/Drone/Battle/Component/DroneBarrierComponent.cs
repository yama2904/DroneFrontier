﻿using Common;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

namespace Drone.Battle
{
    public class DroneBarrierComponent : MonoBehaviour, IDroneComponent
    {
        /// <summary>
        /// バリアの最大透過度
        /// </summary>
        private const float BARRIER_MAX_ALFA_COLOR = 0.5f;

        /// <summary>
        /// バリアの残りHP
        /// </summary>
        public float HP
        {
            get => _hp;
            internal set
            {
                // 破壊検知
                if (_hp > 0 && value <= 0)
                {
                    // バリア回復停止
                    _regeneTimer = 0;
                    _isRegening = false;

                    // バリア破壊SE
                    _soundComponent.Play(SoundManager.SE.DestroyBarrier);

                    // バリア破壊イベント発火
                    _hp = 0;
                    OnBarrierBreak?.Invoke(this, EventArgs.Empty);
                    Debug.Log($"{_drone.Name}:バリア破壊");
                }

                // 最小/最大HP内に補正
                float hp = value;
                if (hp < 0)
                {
                    hp = 0;
                }
                if (hp > MaxHP)
                {
                    hp = MaxHP;
                }
                _hp = hp;

                // バリアの色更新
                ApplyBarrierColor();
            }
        }
        private float _hp = 0;

        /// <summary>
        /// バリアの最大HP
        /// </summary>
        public float MaxHP => _barrierMaxHP;

        /// <summary>
        /// バリアダメージイベント
        /// </summary>
        public event EventHandler OnBarrierDamage;

        /// <summary>
        /// バリア破壊イベント
        /// </summary>
        public event EventHandler OnBarrierBreak;

        /// <summary>
        /// バリア復活イベント
        /// </summary>
        public event EventHandler OnBarrierResurrect;

        /// <summary>
        /// バリア強化開始イベント
        /// </summary>
        public event EventHandler OnStrengthenStart;

        /// <summary>
        /// バリア強化終了イベント
        /// </summary>
        public event EventHandler OnStrengthenEnd;

        /// <summary>
        /// バリア弱体化開始イベント
        /// </summary>
        public event EventHandler OnWeakStart;

        /// <summary>
        /// バリア弱体化終了イベント
        /// </summary>
        public event EventHandler OnWeakEnd;

        [SerializeField, Tooltip("バリアオブジェクト")]
        private GameObject _barrierObject = null;

        [SerializeField, Tooltip("バリアの最大HP")]
        private float _barrierMaxHP = 100f;

        [SerializeField, Tooltip("バリアが回復し始める時間（秒）")]
        private float _regeneStartSec = 8.0f;

        [SerializeField, Tooltip("回復間隔（秒）")]
        private float _regeneIntervalSec = 1.0f;

        [SerializeField, Tooltip("回復量")]
        private float _regeneValue = 5.0f;

        [SerializeField, Tooltip("バリア破壊後の復活時間（秒）")]
        private float _resurrectBarrierSec = 15.0f;

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

        /// <summary>
        /// ドローンのSE再生用コンポーネント
        /// </summary>
        DroneSoundComponent _soundComponent = null;

        public void Initialize()
        {
            // HP初期化
            HP = MaxHP;

            // ドローンが破壊された場合は本コンポーネントを停止
            _drone.OnDroneDestroy += OnDroneDestroy;
        }

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
                _soundComponent.Play(SoundManager.SE.BarrierDamage, 0.7f);
            }

            // バリア回復停止
            _regeneTimer = 0;
            _isRegening = false;

            // ダメージイベント発火
            OnBarrierDamage?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// バリアを復活させる
        /// </summary>
        public void ResurrectBarrier()
        {
            if (HP > 0) return;

            // 修復したら回復処理に移る
            HP = _resurrectBarrierHP;
            _isRegening = true;

            // イベント発火
            OnBarrierResurrect?.Invoke(this, EventArgs.Empty);
            Debug.Log("バリア復活");
        }

        /// <summary>
        /// バリア強化実行
        /// </summary>
        /// <param name="damageDown">ダメージ軽減率（0～1）</param>
        /// <param name="time">強化時間（秒）</param>
        /// <returns>強化に成功した場合はtrue</returns>
        public bool StrengthenBarrier(float damageDown, float time)
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

            // 強化時のバリアの色へ変更
            ApplyBarrierColor();

            // 強化終了タイマー開始
            UniTask.Void(async () =>
            {
                try
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(time), cancellationToken: _cancel.Token);
                    _isStrengthen = false;  // 強化フラグ初期化
                    ApplyBarrierColor();   // 通常時のバリアの色へ戻す
                }
                finally
                {
                    // バリア強化終了イベント発火
                    OnStrengthenEnd?.Invoke(this, EventArgs.Empty);
                }
            });

            // バリア強化開始イベント発火
            OnStrengthenStart?.Invoke(this, EventArgs.Empty);

            return true;
        }

        /// <summary>
        /// バリア弱体化実行
        /// </summary>
        /// <param name="time">弱体化時間（秒）</param>
        /// <returns>弱体化に成功した場合はtrue</returns>
        public bool WeakBarrier(float time)
        {
            // 既に弱体化中の場合は失敗
            if (_isWeak) return false;

            // バリア強化中の場合は強化解除
            if (_isStrengthen)
            {
                _cancel.Cancel();
                _cancel = new CancellationTokenSource();
                _isStrengthen = false;
            }
            else
            {
                // 強化中でない場合はHPを半分にする
                HP = Useful.Floor(HP * 0.5f, 1);
            }

            // バリアの色更新
            ApplyBarrierColor();

            // バリア回復停止
            _regeneTimer = 0;
            _isRegening = false;

            // 弱体化フラグを立てる
            _isWeak = true;

            // 弱体化終了タイマー開始
            UniTask.Void(async () =>
            {
                try
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(time), cancellationToken: _cancel.Token);
                    _isWeak = false;

                    // バリアが破壊されている場合は修復
                    if (HP <= 0)
                    {
                        ResurrectBarrier();
                    }
                }
                finally
                {
                    // バリア弱体化終了イベント発火
                    OnWeakEnd?.Invoke(this, EventArgs.Empty);
                }
            });

            // バリア弱体化開始イベント発火
            OnWeakStart?.Invoke(this, EventArgs.Empty);

            return true;
        }

        private void Awake()
        {
            // 各コンポーネント取得
            _drone = GetComponent<IBattleDrone>();
            _soundComponent = GetComponent<DroneSoundComponent>();
            _material = _barrierObject.GetComponent<Renderer>().material;
        }

        private void Update()
        {
            // ドローンが破壊されている場合は処理しない
            if (_drone.HP <= 0) return;

            // バリア弱体化中は回復処理を行わない
            if (_isWeak) return;

            // HPが減っている場合は回復処理
            if (HP > 0 && HP < MaxHP)
            {
                // 回復中の場合は一定間隔ごとに回復
                if (_isRegening)
                {
                    if (_regeneTimer >= _regeneIntervalSec)
                    {
                        // HP回復
                        HP += _regeneValue;

                        // 回復タイマーリセット
                        _regeneTimer = 0;
                    }
                }
                else
                {
                    // 回復中でない場合は回復が開始するまで待つ
                    if (_regeneTimer >= _regeneStartSec)
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
                if (_regeneTimer >= _resurrectBarrierSec)
                {
                    // バリア復活
                    ResurrectBarrier();
                    _regeneTimer = 0;

                    Debug.Log($"{_drone.Name}:バリア復活");
                }
            }

            _regeneTimer += Time.deltaTime;
        }

        /// <summary>
        /// 残りHPに応じたバリアの色変更を適用する
        /// </summary>
        private void ApplyBarrierColor()
        {
            // 残りHPの割合で色合いを変化
            float value = HP / MaxHP;

            if (!_isStrengthen)
            {
                _material.color = new Color(1 - value, value, 0, value * BARRIER_MAX_ALFA_COLOR);
            }
            else
            {
                _material.color = new Color(1 - value, 0, value, value * BARRIER_MAX_ALFA_COLOR);
            }
        }

        /// <summary>
        /// ドローン破壊イベント
        /// </summary>
        /// <param name="o">イベントオブジェクト</param>
        /// <param name="e">イベント引数</param>
        private void OnDroneDestroy(object o, EventArgs e)
        {
            // イベント削除
            _drone.OnDroneDestroy -= OnDroneDestroy;
        }
    }
}