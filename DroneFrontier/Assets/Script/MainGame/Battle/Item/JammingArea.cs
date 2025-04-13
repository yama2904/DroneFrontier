using System.Collections.Generic;
using UnityEngine;

public class JammingArea : MonoBehaviour
{
    public GameObject Creater { get; set; } = null;

    /// <summary>
    /// 各オブジェクトに付与したジャミングステータス
    /// </summary>
    private Dictionary<GameObject, JammingStatus> _addedJammingStatusMap = new Dictionary<GameObject, JammingStatus>();

    private void OnDestroy()
    {
        foreach (JammingStatus status in _addedJammingStatusMap.Values)
        {
            status.EndJamming();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // ジャミングボットを生成したオブジェクト自身なら処理しない
        if (other.gameObject == Creater) return;

        // 既にジャミング付与済みの場合は処理しない
        if (_addedJammingStatusMap.ContainsKey(other.gameObject)) return;

        // プレイヤーかCPUのみ処理
        string tag = other.tag;
        if (tag != TagNameConst.PLAYER && tag != TagNameConst.CPU) return;

        // ジャミングステータス付与
        JammingStatus status = new JammingStatus();
        other.GetComponent<DroneStatusComponent>().AddStatus(status, 9999);
        _addedJammingStatusMap.Add(other.gameObject, status);
    }

    private void OnTriggerExit(Collider other)
    {
        // ジャミング解除
        if (_addedJammingStatusMap.ContainsKey(other.gameObject))
        {
            _addedJammingStatusMap[other.gameObject].EndJamming();
            _addedJammingStatusMap.Remove(other.gameObject);
        }
    }
}
