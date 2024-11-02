using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestItemUser : MonoBehaviour
{
    [SerializeField]
    private int _useInterval = 10;

    [SerializeField]
    private GameObject _item;

    private float _timer = 0;

    // Update is called once per frame
    void Update()
    {
        if (_timer > _useInterval)
        {
            Instantiate(_item).GetComponent<IDroneItem>().UseItem(gameObject);
            _timer = 0;
        }

        _timer += Time.deltaTime;
    }
}
