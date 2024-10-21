using UnityEngine;
using UnityEngine.UI;

public class SpawnItem : MonoBehaviour
{
    /// <summary>
    /// �X�|�[���A�C�e�����ŃC�x���g
    /// </summary>
    /// <param name="item">���ł����X�|�[���A�C�e��</param>
    public delegate void SpawnItemDestroyHandler(SpawnItem item);

    /// <summary>
    /// �X�|�[���A�C�e�����ŃC�x���g
    /// </summary>
    public event SpawnItemDestroyHandler SpawnItemDestroyEvent; 

    [SerializeField, Tooltip("�A�C�e���������ɕ\������A�C�R��")]
    private Image _iconImage = null;

    [SerializeField, Tooltip("�X�|�[��������A�C�e��")]
    private GameObject _item = null;

    /// <summary>
    /// �A�C�e���̃A�C�R��
    /// </summary>
    public Image IconImage { get { return _iconImage; } }

    /// <summary>
    /// �X�|�[�������A�C�e��
    /// </summary>
    public GameObject Item { get { return _item; } }

    private void OnDestroy()
    {
        SpawnItemDestroyEvent?.Invoke(this);
    }
}
