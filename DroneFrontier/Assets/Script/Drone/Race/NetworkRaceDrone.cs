using Drone.Network;
using System;
using System.Collections.Generic;

namespace Drone.Race.Network
{
    public class NetworkRaceDrone : NetworkDrone
    {
        public override string GetAddressKey()
        {
            return "NetworkRaceDrone";
        }

        public override object CreateSpawnData()
        {
            return new Dictionary<string, object>()
            {
                { "Name", Name },
                { "enabled", enabled }
            };
        }

        public override void ImportSpawnData(object data)
        {
            var dic = data as Dictionary<string, object>;
            Name = (string)dic["Name"];
            enabled = Convert.ToBoolean(dic["enabled"]);
        }
    }
}
