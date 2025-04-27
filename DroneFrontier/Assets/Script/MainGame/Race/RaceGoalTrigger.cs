using System;
using System.Collections.Generic;
using Common;
using Drone.Network;
using Drone.Race.Network;
using UnityEngine;

namespace Race
{
    public class RaceGoalTrigger : MonoBehaviour
    {
        /// <summary>
        /// プレイヤーゴールイベント
        /// </summary>
        public static event EventHandler OnGoal;

        /// <summary>
        /// ゴールプレイヤー
        /// </summary>
        public List<string> GoalPlayers { get; private set; } = new List<string>();

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(TagNameConst.PLAYER))
            {
                GoalPlayers.Add(other.gameObject.GetComponent<NetworkRaceDrone>().Name);
                OnGoal?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}