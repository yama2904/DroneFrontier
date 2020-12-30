using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BarrierWeakArea : MonoBehaviour
{
    [SerializeField] ParticleSystem lineParticle = null;
    [SerializeField] ParticleSystem thunderParticle = null;
    [SerializeField] float lineRadius = 0.01f;  //レーザーの半径
    Transform lineTransform = null;
    Transform thunderTransform = null;
    float initThunderScaleZ;    //初期のthunderの長さ(敵にレーザーが当たった際に使う)
    float initThunderPosZ;      //初期のthunderのz座標

    //ヒットしているプレイヤーを格納
    List<BasePlayer> hitPlayers = new List<BasePlayer>();

    void Start()
    {
        lineTransform = lineParticle.transform;
        thunderTransform = thunderParticle.transform;
        initThunderPosZ = thunderTransform.localPosition.z;
        initThunderScaleZ = thunderTransform.localScale.z;
        hitPlayers.Clear();

        ModifyLaserLength(1000);
    }

    void FixedUpdate()
    {
        
    }

    //レーザーの長さを変える
    void ModifyLaserLength(float length)
    {
        //Lineオブジェクト
        Vector3 lineScale = lineTransform.localScale;
        lineTransform.localScale = new Vector3(length, length, lineScale.z);

        //Thunderオブジェクト
        Vector3 thunderScale = thunderTransform.localScale;
        thunderTransform.localScale = new Vector3(thunderScale.x, thunderScale.y, initThunderScaleZ * length);
        Vector3 thunderPos = thunderTransform.localPosition;
        thunderTransform.localPosition = new Vector3(thunderPos.x, thunderPos.y, initThunderPosZ * length);
    }
}
