using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagnetArea : MonoBehaviour
{
    [SerializeField] float downPercent = 0.7f;  //下がる倍率

    void Start()
    {
    }

    void Update()
    {
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(TagNameManager.PLAYER) || other.CompareTag(TagNameManager.CPU))
        {
            IPlayerStatus ps = other.GetComponent<Player>();
            ps.SetSpeedDown(downPercent);


            //デバッグ用
            Debug.Log(other.GetComponent<Player>().name + ": in磁場エリア");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(TagNameManager.PLAYER) || other.CompareTag(TagNameManager.CPU))
        {
            IPlayerStatus ps = other.GetComponent<Player>();
            ps.UnSetSpeedDown();


            //デバッグ用
            Debug.Log(other.GetComponent<Player>().name + ": out磁場エリア");
        }
    }
}
