using Common;
using Drone.Race.Network;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Race
{
    public class RaceGoalTrigger : MonoBehaviour
    {
        /// <summary>
        /// �v���C���[�S�[���C�x���g
        /// </summary>
        public static event EventHandler OnGoal;

        /// <summary>
        /// �S�[���v���C���[
        /// </summary>
        public List<string> GoalPlayers { get; private set; } = new List<string>();

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(TagNameConst.PLAYER))
            {
                string player = other.gameObject.GetComponent<NetworkRaceDrone>().Name;
                if (GoalPlayers.Contains(player)) return;

                GoalPlayers.Add(player);
                OnGoal?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}