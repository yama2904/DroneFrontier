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

    [SerializeField, Tooltip("�X�|�[��������A�C�e���ꗗ")]
    private SpawnItem[] _spawnItems = null;

    [SerializeField,  Tooltip("�X�|�[���m��(0�`1)")] 
    private float _spawnPercent = 0.5f;

    /// <summary>
    /// ���������A�C�e��
    /// </summary>
    private SpawnItem _createdItem = null;

    /// <summary>
    /// �L���b�V���pTransform
    /// </summary>
    private Transform _transform = null;

    /// <summary>
    /// �����_���ȃA�C�e�����X�|�[��������
    /// </summary>
    /// <returns>�X�|�[�������A�C�e��</returns>
    public SpawnItem Spawn()
    {
        // �����_���ɃX�|�[��
        int index = Random.Range(0, _spawnItems.Length);
        _createdItem = Instantiate(_spawnItems[index], _transform);
        _createdItem.transform.SetParent(_transform);

        // �X�|�[�������A�C�e����Ԃ�
        return _createdItem;
    }

    /// <summary>
    /// �X�|�[���m������ɐ����ۂ����肵�A���������ꍇ�̓����_���ȃA�C�e�����X�|�[��������
    /// </summary>
    /// <returns>�X�|�[�������A�C�e���B���s�����ꍇ��null</returns>
    public SpawnItem SpawnRandom()
    {
        // �X�|�[���m������ɐ����ۂ�����
        if (Random.Range(0, 101) > _spawnPercent * 100) return null;

        // �A�C�e��
        return Spawn();
    }

    private void Awake()
    {
        // Transform���L���b�V���ۑ����Ă���
        _transform = transform;
    }
}
