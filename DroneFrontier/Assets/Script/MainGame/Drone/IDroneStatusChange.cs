using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ドローンステータス変更インターフェース
/// </summary>
public interface IDroneStatusChange
{
    /// <summary>
    /// 状態異常アイコン生成
    /// </summary>
    /// <returns></returns>
    Image InstantiateIcon();

    /// <summary>
    /// ステータス変化実行
    /// </summary>
    /// <param name="drone">ステータスを変化させるドローン</param>
    /// <param name="statusSec">ステータス変化時間（秒）</param>
    /// <param name="addParams">追加パラメータ</param>
    /// <returns>true:成功, false:失敗</returns>
    bool Invoke(GameObject drone, float statusSec, params object[] addParams);

    /// <summary>
    /// ステータス変化終了イベント
    /// </summary>
    event EventHandler StatusEndEvent;
}
