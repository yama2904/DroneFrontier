using System;
using UnityEngine;

/// <summary>
/// ドローンステータス変更インターフェース
/// </summary>
public interface IDroneStatus
{
    /// <summary>
    /// 状態変化のアイコン
    /// </summary>
    RectTransform IconImage { get; }

    /// <summary>
    /// ステータス変化実行
    /// </summary>
    /// <param name="drone">ステータスを変化させるドローン</param>
    /// <param name="parameters">パラメータ</param>
    /// <returns>true:成功, false:失敗</returns>
    bool Invoke(GameObject drone, params object[] parameters);

    /// <summary>
    /// ステータス変化終了イベント
    /// </summary>
    public event EventHandler StatusEndEvent;
}
