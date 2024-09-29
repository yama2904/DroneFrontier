using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    /// <summary>
    /// �X�|�[���m���i0�`1�j
    /// </summary>
    public float SpawnPercent
    {
        get { return _spawnPercent; }
    }

    /// <summary>
    /// �X�|�[���A�C�e�����ŃC�x���g
    /// </summary>
    /// <param name="spawner">���ł����A�C�e���̃X�|�i�[</param>
    public delegate void SpawnItemDestroyHandler(ItemSpawner spawner);

    /// <summary>
    /// �X�|�[���A�C�e�����ŃC�x���g
    /// </summary>
    public event SpawnItemDestroyHandler SpawnItemDestroyEvent;

    [SerializeField, Tooltip("�X�|�[��������A�C�e���ꗗ")]
    private GameObject[] _spawnItems = null;

    [SerializeField,  Tooltip("�X�|�[���m��(0�`1)")] 
    private float _spawnPercent = 0.5f;

    /// <summary>
    /// �X�|�[��������A�C�e���̊e�N���X
    /// </summary>
    private System.Type[] _spawnItemClasses = null;

    /// <summary>
    /// ���������A�C�e��
    /// </summary>
    private GameObject _createdItem = null;

    /// <summary>
    /// �L���b�V���pTransform
    /// </summary>
    private Transform _transform = null;

    /// <summary>
    /// �����_���ȃA�C�e�����X�|�[��������
    /// </summary>
    public void SpawnItem()
    {
        // �O��X�|�[�������A�C�e�����c���Ă��邱�Ƃ��l�����č폜
        if (_createdItem != null)
        {
            Destroy(_createdItem);
        }

        // �����_���ɃX�|�[��
        int index = Random.Range(0, _spawnItems.Length);
        _createdItem = Instantiate(_spawnItems[index], _transform);
        _createdItem.transform.SetParent(_transform);

        // �A�C�e�������C�x���g�ݒ�
        _createdItem.GetComponent<SpawnItem>().SpawnItemDestroyEvent += (item) =>
        {
            SpawnItemDestroyEvent?.Invoke(this);
        };
    }

    /// <summary>
    /// �w�肵���A�C�e�����X�|�[��������
    /// </summary>
    /// <param name="itemType">�X�|�[��������A�C�e��</param>
    /// <returns>true:�w�肳�ꂽ�A�C�e�������݂���ꍇ��true</returns>
    public bool SpawnItem(System.Type itemType)
    {
        // �w�肳�ꂽ�A�C�e���ɍ��v������X�|�[��
        for (int i = 0; i < _spawnItems.Length; i++)
        {
            // �w�肳�ꂽ�A�C�e���ł��邩
            if (_spawnItemClasses[i] == itemType)
            {
                // �O��X�|�[�������A�C�e�����c���Ă��邱�Ƃ��l�����č폜
                if (_createdItem != null)
                {
                    Destroy(_createdItem);
                }

                // �A�C�e���X�|�[��
                _createdItem = Instantiate(_spawnItems[i], _transform);
                _createdItem.transform.SetParent(_transform);

                // �A�C�e�������C�x���g�ݒ�
                _createdItem.GetComponent<SpawnItem>().SpawnItemDestroyEvent += (item) =>
                {
                    SpawnItemDestroyEvent?.Invoke(this);
                };

                return true;
            }
        }

        // �w�肳�ꂽ�A�C�e�������݂��Ȃ�
        return false;
    }

    void Start()
    {
        // Transform���L���b�V���ۑ����Ă���
        _transform = transform;

        // �e�A�C�e���̃N���X��ێ����Ă���
        _spawnItemClasses = new System.Type[_spawnItems.Length];
        for (int i = 0; i < _spawnItems.Length; i++)
        {
            _spawnItemClasses[i] = _spawnItems[i].GetType();
        }
    }

    void Update() { }
}
