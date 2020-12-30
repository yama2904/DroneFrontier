using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagnetArea : MonoBehaviour
{
    [SerializeField] float downMgnf = 0.3f;  //下がる倍率
    float restoreMgnf;  //元に戻すときの倍率(先に計算させる用)

    void Start()
    {
        restoreMgnf = 1 / downMgnf;
    }

    void Update()
    {
    }

    void ModifyBasePlayerValue(BasePlayer basePlayer, float mgnf)
    {
        basePlayer.MoveSpeed *= mgnf;
        basePlayer.MaxSpeed *= mgnf;
        basePlayer.AtackingDecreaseSpeed *= mgnf;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(Player.PLAYER_TAG) || other.CompareTag(CPUController.CPU_TAG))
        {
            BasePlayer bp = other.GetComponent<BasePlayer>();
            ModifyBasePlayerValue(bp, downMgnf);


            //デバッグ用
            Debug.Log(bp.name + ": in磁場エリア");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(Player.PLAYER_TAG) || other.CompareTag(CPUController.CPU_TAG))
        {
            BasePlayer bp = other.GetComponent<BasePlayer>();
            ModifyBasePlayerValue(bp, restoreMgnf);


            //デバッグ用
            Debug.Log(bp.name + ": out磁場エリア");
        }
    }
}
