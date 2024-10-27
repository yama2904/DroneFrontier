using UnityEngine;

/// <summary>
/// オブジェクト探索コンポーネント
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
