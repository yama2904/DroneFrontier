using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WatchingGame : MonoBehaviour
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

    private void Start() { }

    private void Update()
    {
        if (_watchDrones.Count <= 0) return;

        // スペースキーで次のCPUへカメラ切り替え
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // カメラ深度初期化
            _watchDrones[_watchingDrone].SetCameraDepth(0);

            // 次のCPU
            _watchingDrone++;
            if (_watchingDrone >= _watchDrones.Count)
            {
                _watchingDrone = 0;
            }

            // カメラ参照設定
            _watchDrones[_watchingDrone].SetCameraDepth(5);
        }
    }

    private void OnEnable()
    {
        // 試合中のCPU取得
        _watchDrones = FindObjectsByType<CpuBattleDrone>(FindObjectsSortMode.None).ToList();

        // 全てのドローンのカメラ深度初期化
        foreach (CpuBattleDrone drone in _watchDrones)
        {
            drone.SetCameraDepth(0);
        }

        // 参照先カメラ設定
        _watchingDrone = 0;
        _watchDrones[_watchingDrone].SetCameraDepth(5);

        // ドローン破壊イベント設定
        _droneSpawnManager.DroneDestroyEvent += DroneDestroy;

        // AudioListener有効化
        GetComponent<AudioListener>().enabled = true;
    }


    private void OnDisable()
    {
        // 全てのドローンのカメラ深度初期化
        foreach (CpuBattleDrone drone in _watchDrones)
        {
            drone.SetCameraDepth(0);
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
        if (destroyDrone is CpuBattleDrone drone)
        {
            // 破壊されたドローンをリスポーンしたドローンに入れ替える
            int index = _watchDrones.IndexOf(drone);
            _watchDrones.RemoveAt(index);
            _watchDrones.Insert(index, drone);

            // 破壊されたドローンが現在観戦中のCPUの場合はカメラ深度調整
            if (index == _watchingDrone)
            {
                drone.SetCameraDepth(5);
            }
             else
            {
                // 観戦中CPUでない場合はカメラ深度初期化
                drone.SetCameraDepth(0);
            }
        }
    }
}
