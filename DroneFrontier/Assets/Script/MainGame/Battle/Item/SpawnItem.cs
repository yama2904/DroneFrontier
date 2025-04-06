using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpawnItem : MonoBehaviour, ISpawnItem, IRadarable
{
    /// <summary>
    /// �X�|�[���A�C�e�����ŃC�x���g
    /// </summary>
    public event EventHandler SpawnItemDestroyEvent;

    /// <summary>
    /// �擾���Ɏg�p�\�ƂȂ�A�C�e��
    /// </summary>
    public IDroneItem DroneItem { get; private set; }

    public IRadarable.ObjectType Type => IRadarable.ObjectType.Item;

    public bool IsRadarable => true;

    public List<GameObject> NotRadarableList => new List<GameObject>();

    [SerializeField, Tooltip("�A�C�e���������ɕ\������A�C�R��")]
    private Image _iconImage = null;

    [SerializeField, Tooltip("�擾���Ɏg�p�\�ƂȂ�A�C�e���i���vIDroneItem�C���^�[�t�F�[�X�����j")]
    private GameObject _droneItem = null;

    public Image InstantiateIcon()
    {
        return Instantiate(_iconImage);
    }

    private void Awake()
    {
        DroneItem = Instantiate(_droneItem, Vector3.zero, Quaternion.identity).GetComponent<IDroneItem>();
    }

    private void OnDestroy()
    {
        SpawnItemDestroyEvent?.Invoke(this, EventArgs.Empty);
    }
}
