using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class BarrierStrength : NetworkBehaviour
{
    static float strengthRate = 0.5f;    //バリアのダメージ軽減率
    static float strengthTime = 10.0f;   //強化時間

    //バリアを強化する
    public static bool Strength(Player player)
    {
        return player.SetBarrierStrength(strengthRate, strengthTime);
    }
}
