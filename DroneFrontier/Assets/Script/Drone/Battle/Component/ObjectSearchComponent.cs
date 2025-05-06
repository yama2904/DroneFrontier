using UnityEngine;

namespace Drone.Battle
{
    /// <summary>
    /// オブジェクト探索コンポーネント
    /// </summary>
    public class ObjectSearchComponent : MonoBehaviour, IDroneComponent
    {
        public delegate void ObjectStayHandler(Collider other);
        public event ObjectStayHandler OnObjectStay;

        public void Initialize() { }

        private void OnTriggerStay(Collider other)
        {
            OnObjectStay?.Invoke(other);
        }
    }
}