using UnityEngine;

/// <summary>
/// �I�u�W�F�N�g�T���R���|�[�l���g
/// </summary>
public class ObjectSearchComponent : MonoBehaviour
{
    public delegate void ObjectStayHandler(Collider other);
    public event ObjectStayHandler ObjectStayEvent;

    private void OnTriggerStay(Collider other)
    {
        ObjectStayEvent?.Invoke(other);
    }
}
