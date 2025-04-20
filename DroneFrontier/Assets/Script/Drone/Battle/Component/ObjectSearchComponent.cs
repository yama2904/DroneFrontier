using UnityEngine;

namespace Drone.Battle
{
    /// <summary>
    /// �I�u�W�F�N�g�T���R���|�[�l���g
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