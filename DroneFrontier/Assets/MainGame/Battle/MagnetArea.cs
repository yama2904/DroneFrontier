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
        if (other.CompareTag(Player.PLAYER_TAG) || other.CompareTag(CPUController.CPU_TAG))
        {
            IPlayerStatus ps = other.GetComponent<BasePlayer>();
            ps.SetSpeedDown(downPercent);


            //デバッグ用
            Debug.Log(other.GetComponent<BasePlayer>().name + ": in磁場エリア");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(Player.PLAYER_TAG) || other.CompareTag(CPUController.CPU_TAG))
        {
            IPlayerStatus ps = other.GetComponent<BasePlayer>();
            ps.UnSetSpeedDown();


            //デバッグ用
            Debug.Log(other.GetComponent<BasePlayer>().name + ": out磁場エリア");
        }
    }
}
