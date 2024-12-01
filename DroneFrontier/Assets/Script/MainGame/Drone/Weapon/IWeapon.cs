using System;
using UnityEngine;

public interface IWeapon
{
    /// <summary>
    /// 武器所有者
    /// </summary>
    public GameObject Owner { get; set; }

    /// <summary>
    /// 弾丸発射座標
    /// </summary>
    public Transform ShotPosition { get; set; }

    /// <summary>
    /// 残弾UI表示Canvas
    /// </summary>
    public Canvas BulletUICanvas { get; set; }

    /// <summary>
    /// 全弾補充イベント
    /// </summary>
    public event EventHandler OnBulletFull;

    /// <summary>
    /// 残弾無しイベント
    /// </summary>
    public event EventHandler OnBulletEmpty;

    /// <summary>
    /// 弾丸発射
    /// </summary>
    /// <param name="target">追従先オブジェクト</param>
    public void Shot(GameObject target = null);
}
