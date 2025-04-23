using Common;
using Drone.Battle;
using System;
using System.Collections.Generic;
using UnityEngine;

public class DroneSpawnManager : MonoBehaviour
{
    /// <summary>
    /// ドローン破壊イベント
    /// </summary>
    /// <param name="destroyDrone">破壊されたドローン</param>
    /// <param name="respawnDrone">リスポーンしたドローン（残機が無くなった場合はnull）</param>
    public delegate void DroneDestroyHandler(IBattleDrone destroyDrone, IBattleDrone respawnDrone);

    /// <summary>
    /// ドローン破壊イベント
    /// </summary>
    public event DroneDestroyHandler DroneDestroyEvent;

    [SerializeField, Tooltip("プレイヤードローン")]
    private GameObject _playerDrone = null;

    [SerializeField, Tooltip("CPUドローン")]
    private GameObject _cpuDrone = null;

    [SerializeField, Tooltip("ドローンスポーン位置")]
    private Transform[] _droneSpawnPositions = null;

    /// <summary>
    /// 各ドローンの初期情報
    /// </summary>
    private Dictionary<string, (WeaponType weapon, Transform pos)> _initDatas = new Dictionary<string, (WeaponType weapon, Transform pos)>();

    /// <summary>
    /// 次のスポーン時に使用する配列インデックス
    /// </summary>
    private int _nextSpawnIndex = 0;

    /// <summary>
    /// ドローンをスポーンさせる
    /// </summary>
    /// <param name="name">スポーンさせるドローンの名前</param>
    /// <param name="weapon">スポーンさせるドローンのサブ武器</param>
    /// <param name="isPlayer">プレイヤーであるか</param>
    /// <returns>スポーンさせたドローン</returns>
    public IBattleDrone SpawnDrone(string name, WeaponType weapon, bool isPlayer)
    {
        // スポーン位置取得
        Transform spawnPos = _droneSpawnPositions[_nextSpawnIndex];

        // ドローン生成
        IBattleDrone drone = CreateDrone(spawnPos, isPlayer);
        IWeapon main = WeaponCreater.CreateWeapon(WeaponType.GATLING);
        IWeapon sub = WeaponCreater.CreateWeapon(weapon);
        drone.Initialize(name, main, sub, drone.StockNum);

        // スポーン時点情報を保存
        _initDatas.Add(drone.Name, (weapon, spawnPos));

        // 次のスポーン位置
        _nextSpawnIndex++;
        if (_nextSpawnIndex >= _droneSpawnPositions.Length)
        {
            _nextSpawnIndex = 0;
        }

        return drone;
    }

    private void Awake()
    {
        // 初期スポーン位置をランダムに選択
        _nextSpawnIndex = UnityEngine.Random.Range(0, _droneSpawnPositions.Length);
    }

    /// <summary>
    /// ドローン生成
    /// </summary>
    /// <param name="spawnPosition">スポーン位置</param>
    /// <param name="isPlayer">プレイヤーであるか</param>
    /// <returns>生成したドローン</returns>
    private IBattleDrone CreateDrone(Transform spawnPosition, bool isPlayer)
    {
        // 生成元オブジェクト選択
        GameObject drone = isPlayer ? _playerDrone : _cpuDrone;

        IBattleDrone createdDrone = Instantiate(drone, spawnPosition.position, spawnPosition.rotation).GetComponent<IBattleDrone>();
        createdDrone.DroneDestroyEvent += DroneDestroy;

        return createdDrone;
    }

    /// <summary>
    /// ドローン破壊イベント
    /// </summary>
    /// <param name="sender">イベントオブジェクト</param>
    /// <param name="e">イベント引数</param>
    private void DroneDestroy(object sender, EventArgs e)
    {
        IBattleDrone drone = sender as IBattleDrone;

        // 破壊されたドローンの初期情報取得
        var initData = _initDatas[drone.Name];

        // リスポーンさせたドローン
        IBattleDrone respawnDrone = null;

        if (drone.StockNum > 0)
        {
            if (drone is BattleDrone)
            {
                // リスポーン
                respawnDrone = CreateDrone(initData.pos, true);

                // 復活SE再生
                SoundManager.Play(SoundManager.SE.Respawn);
            }
            else
            {
                // リスポーン
                respawnDrone = CreateDrone(initData.pos, false);
            }

            // ドローン初期化
            IWeapon main = WeaponCreater.CreateWeapon(WeaponType.GATLING);
            IWeapon sub = WeaponCreater.CreateWeapon(initData.weapon);
            respawnDrone.Initialize(drone.Name, main, sub, drone.StockNum - 1);
        }

        // イベント発火
        DroneDestroyEvent?.Invoke(drone, respawnDrone);

        // 破壊されたドローンからイベントの削除
        drone.DroneDestroyEvent -= DroneDestroy;
    }
}
