using Drone.Battle;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DroneWatcher : MonoBehaviour
{
    [SerializeField, Tooltip("ドローンスポーン管理オブジェクト")]
    private DroneSpawnManager _droneSpawnManager = null;

    /// <summary>
    /// 観戦中のCPUドローン
    /// </summary>
    private List<CpuBattleDrone> _watchDrones = new List<CpuBattleDrone>();

    /// <summary>
    /// 現在カメラ参照中のドローンのインデックス
    /// </summary>
    private int _watchingDrone = 0;

    private void Update()
    {
        if (_watchDrones.Count <= 0) return;

        // スペースキーで次のCPUへカメラ切り替え
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _watchDrones[_watchingDrone].IsWatch = false;

            // 次のCPU
            _watchingDrone++;
            if (_watchingDrone >= _watchDrones.Count)
            {
                _watchingDrone = 0;
            }

            // カメラ参照設定
            _watchDrones[_watchingDrone].IsWatch = true;
        }
    }

    private void OnEnable()
    {
        // 試合中のCPU取得
        _watchDrones = FindObjectsByType<CpuBattleDrone>(FindObjectsSortMode.None).ToList();

        // 全てのドローンのカメラ参照初期化
        foreach (CpuBattleDrone drone in _watchDrones)
        {
            drone.IsWatch = false;
        }

        // 参照先カメラ設定
        _watchingDrone = 0;
        _watchDrones[_watchingDrone].IsWatch = true;

        // ドローン破壊イベント設定
        _droneSpawnManager.DroneDestroyEvent += DroneDestroy;

        // AudioListener有効化
        GetComponent<AudioListener>().enabled = true;
    }


    private void OnDisable()
    {
        // 全てのドローンのカメラ参照初期化
        foreach (CpuBattleDrone drone in _watchDrones)
        {
            drone.IsWatch = false;
        }

        // ドローン破壊イベント削除
        _droneSpawnManager.DroneDestroyEvent -= DroneDestroy;

        // AudioListener無効化
        GetComponent<AudioListener>().enabled = false;
    }

    /// <summary>
    /// ドローン破壊イベント
    /// </summary>
    /// <param name="destroyDrone">破壊されたドローン</param>
    /// <param name="respawnDrone">リスポーンしたドローン</param>
    private void DroneDestroy(IBattleDrone destroyDrone, IBattleDrone respawnDrone)
    {
        if (respawnDrone is CpuBattleDrone drone)
        {
            // 破壊されたドローンをリスポーンしたドローンに入れ替える
            int index = _watchDrones.IndexOf(destroyDrone as CpuBattleDrone);
            _watchDrones.RemoveAt(index);
            _watchDrones.Insert(index, drone);

            // 破壊されたドローンが現在観戦中のCPUの場合はリスポーンしたドローンを見る
            if (index == _watchingDrone)
            {
                drone.IsWatch = true;
            }
        }
    }
}
