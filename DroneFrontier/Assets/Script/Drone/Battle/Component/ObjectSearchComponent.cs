using UnityEngine;

namespace Drone.Battle
{
    /// <summary>
    /// オブジェクト探索コンポーネント
    /// </summary>
    public class ObjectSearchComponent : MonoBehaviour, IDroneComponent
    {
        public delegate void ObjectStayHandler(Collider other);
        public event ObjectStayHandler ObjectStayEvent;

        public void Initialize() { }

        private void OnTriggerStay(Collider other)
        {
            ObjectStayEvent?.Invoke(other);
        }
    }
}