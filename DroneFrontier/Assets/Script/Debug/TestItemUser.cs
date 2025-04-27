using Battle.DroneItem;
using UnityEngine;

public class TestItemUser : MonoBehaviour
{
    [SerializeField]
    private int _useInterval = 10;

    private float _timer = 0;

    // Update is called once per frame
    void Update()
    {
        if (_timer > _useInterval)
        {
            new JammingItem().UseItem(gameObject);
            _timer = 0;
        }

        _timer += Time.deltaTime;
    }
}
