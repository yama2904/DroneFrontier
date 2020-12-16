using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AtackBase : MonoBehaviour
{
    //武器の所持者のオブジェクト名
    public string OwnerName { get; set; } = "";
        
    protected float recast;         //リキャスト時間
    protected float shotPerSecond;  //1秒間に発射する数
    protected abstract void Start();
    protected abstract void Update();

    public abstract void Shot(Transform transform, GameObject target = null);
}
