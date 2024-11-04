using UnityEngine;

public interface IWeapon
{
    /// <summary>
    /// 武器所有者
    /// </summary>
    GameObject Owner { get; set; }

    /// <summary>
    /// 弾丸発射座標
    /// </summary>
    Transform ShotPosition { get; set; }

    /// <summary>
    /// 残弾UI表示Canvas
    /// </summary>
    Canvas BulletUICanvas { get; set; }

    /// <summary>
    /// 弾丸発射
    /// </summary>
    /// <param name="target">追従先オブジェクト</param>
    void Shot(GameObject target = null);
}
