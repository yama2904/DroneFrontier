using UnityEngine;

namespace Offline
{
    public class BarrierStrength : MonoBehaviour, IGameItem
    {
        static float strengthRate = 0.5f;    //バリアのダメージ軽減率
        static float strengthTime = 10.0f;   //強化時間

        //バリアを強化する
        public static bool Strength(Player.DroneStatusAction player)
        {
            return player.SetBarrierStrength(strengthRate, strengthTime);
        }

        public static bool Strength(CPU.DroneStatusAction player)
        {
            return player.SetBarrierStrength(strengthRate, strengthTime);
        }

        public bool UseItem(GameObject drone)
        {
            return drone.GetComponent<Player.DroneStatusAction>().SetBarrierStrength(strengthRate, strengthTime);
        }
    }
}