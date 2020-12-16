using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AtackBase : MonoBehaviour
{
    //武器の所持者のオブジェクト名
    public string OwnerName { get; set; } = "";

    protected float recast;       //リキャスト時間
    protected float recastCount;
    protected float shotInterval; //1発ごとの間隔
    protected float shotCount;

    protected abstract void Start();
    protected virtual void Update()
    {
        recastCount += Time.deltaTime;
        if (recastCount > recast)
        {
            recastCount = recast;
        }

        shotCount += Time.deltaTime;
        if (shotCount > shotInterval)
        {
            shotCount = shotInterval;
        }
    }

    /*
     * 変数の初期化
     * 引数1: リキャスト時間
     * 引数2: 1秒間に何発発射するか
     */
    protected void InitValue(float initRecast, float shotParSecond)
    {
        recast = initRecast;
        recastCount = initRecast;
        shotInterval = 1 / shotParSecond;
        shotCount = shotInterval;
    }

    public abstract void Shot(Transform transform, GameObject target = null);
}
