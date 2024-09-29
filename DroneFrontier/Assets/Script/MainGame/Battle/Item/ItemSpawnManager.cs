using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ItemSpawnManager : MonoBehaviour
{
    [SerializeField, Tooltip("�t�B�[���h��ɏo��������A�C�e���̏��")] 
    private int _maxSpawnNum = 10;

    [SerializeField, Tooltip("�A�C�e�����o������Ԋu")] 
    private float _spawnInterval = 10f;

    [SerializeField, Tooltip("����I�ɃX�|�[������A�C�e���̐�")]
    private int _spawnNum = 1;

    /// <summary>
    /// �A�C�e���X�|�i�[�i�A�C�e���X�|�[���ς݂̏ꍇ��true�j
    /// </summary>
    private Dictionary<ItemSpawner, bool> _spawners = new Dictionary<ItemSpawner, bool>();

    /// <summary>
    /// �O��X�|�[������̌o�ߎ���
    /// </summary>
    float _spawnTimeCount = 0;

    private void Start()
    {
        // �e�X�|�i�[���������Ď擾
        ItemSpawner[] spawners = FindObjectsByType<ItemSpawner>(FindObjectsSortMode.None).ToArray();
        
        // �X�|�i�[���ێ�
        foreach (ItemSpawner spawner in spawners)
        {
            _spawners.Add(spawner, false);
        }

        // �A�C�e���̃����_���X�|�[��
        ItemSpawn(spawners, _maxSpawnNum);
    }

    private void Update()
    {
        _spawnTimeCount += Time.deltaTime;
        if (_spawnTimeCount < _spawnInterval) return;

        // �o�ߎ��ԃ��Z�b�g
        _spawnTimeCount = 0;

        // ���X�|�[���̃X�|�i�[���W�v
        List<ItemSpawner> notSpawned = new List<ItemSpawner>();
        lock (_spawners)
        {
            foreach (ItemSpawner spawner in _spawners.Keys)
            {
                if (!_spawners[spawner])
                {
                    notSpawned.Add(spawner);
                }
            }

            if (_spawners.Count - notSpawned.Count >= _maxSpawnNum) return;
        }

        // �X�|�[�����s
        ItemSpawn(notSpawned.ToArray(), _spawnNum);
    }

    /// <summary>
    /// �w�肳�ꂽ�X�|�i�[�̒����烉���_���ɃA�C�e���X�|�[��
    /// </summary>
    /// <param name="spawners">�A�C�e���X�|�[��������X�|�i�[</param>
    /// <param name="spawnNum">�X�|�[����</param>
    private void ItemSpawn(ItemSpawner[] spawners, int spawnNum)
    {
        for (int num = 0; num < spawnNum; num++)
        {
            // �e�X�|�i�[�̃X�|�[���m�����v�Z
            int maxRandom = 0;
            List<int> percents = new List<int>();
            foreach (ItemSpawner spawner in  spawners)
            {
                int percent = (int)(spawner.SpawnPercent * 100);
                percents.Add(maxRandom + percent);
                maxRandom += percent;
            }

            // �X�|�[���A�C�e���������_���Ɍ���
            int value = Random.Range(0, maxRandom + 1);
            for (int i = 0; i < percents.Count; i++)
            {
                ItemSpawner spawner = spawners[i];

                if (value <= percents[i])
                {
                    spawner.SpawnItem();
                    _spawners[spawner] = true;
                    continue;
                }
            }
        }
    }

    /// <summary>
    /// �X�|�[���A�C�e�����ŃC�x���g
    /// </summary>
    /// <param name="spawner">���ł����A�C�e���̃X�|�i�[</param>
    private void SpawnItemDestroy(ItemSpawner spawner)
    {
        // ���X�|�[����Ԃ֍X�V
        lock (_spawners)
        {
            _spawners[spawner] = false;
        }
    }
}
