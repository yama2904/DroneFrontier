using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissieShot : AtackBase
{
    [SerializeField] GameObject missile = null;

    List<MissileBullet> missiles;
    float shotInterval; //1発ごとの間隔
    float deltaTime;

    protected override void Start()
    {
        missiles = new List<MissileBullet>();
        shotPerSecond = 2.0f;
        shotInterval = 1 / shotPerSecond;
        deltaTime = 0;
    }

    protected override void Update()
    {

        //消滅した弾丸がないか走査
        for (int i = 0; i < missiles.Count; i++)
        {
            if (missiles[i] == null)
            {
                missiles.RemoveAt(i);
            }
        }

        deltaTime += Time.deltaTime;
    }

    public override void Shot(Transform t)
    {
        if (deltaTime > shotInterval)
        {
            GameObject o = Instantiate(missile, t.position, t.rotation) as GameObject;    //ミサイルの複製
            missiles.Add(o.GetComponent<MissileBullet>());

            deltaTime = 0;
        }
    }
}
