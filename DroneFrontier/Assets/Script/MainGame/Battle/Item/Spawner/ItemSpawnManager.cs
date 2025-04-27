using Common;
using Drone.Battle;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Battle.Spawner
{
    public class ItemSpawnManager : MonoBehaviour
    {
        [SerializeField, Tooltip("�t�B�[���h��ɏo��������A�C�e���̏��")]
        private int _maxSpawnNum = 10;

        [SerializeField, Tooltip("�A�C�e�����o������Ԋu")]
        private float _spawnInterval = 10f;

        [SerializeField, Tooltip("����I�ɃX�|�[������A�C�e���̐�")]
        private int _spawnNum = 1;

        /// <summary>
        /// �A�C�e���X�|�i�[���X�g
        /// </summary>
        private List<IItemSpawner> _spawnerList = new List<IItemSpawner>();

        /// <summary>
        /// �X�|�[�������A�C�e���ƑΉ�����X�|�i�[
        /// </summary>
        private Dictionary<ISpawnItem, IItemSpawner> _spawnedMap = new Dictionary<ISpawnItem, IItemSpawner>();

        /// <summary>
        /// ����X�|�[���v��
        /// </summary>
        private float _spawnTimer = 0;

        /// <summary>
        /// �A�C�e���X�|�[����L���ɂ��邩�w�肵�ď�����
        /// </summary>
        /// <param name="enableSpawn">�A�C�e���X�|�[����L���ɂ���ꍇ��true</param>
        public void Initialize(bool enableSpawn)
        {
            if (enableSpawn)
            {
                // �e�X�|�i�[���������Ď擾
                MonoBehaviour[] objects = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
                foreach (MonoBehaviour obj in objects)
                {
                    if (obj is IItemSpawner spawner)
                    {
                        _spawnerList.Add(spawner);
                    }
                }

                // �A�C�e���̃����_���X�|�[��
                ItemSpawn(_spawnerList, _maxSpawnNum);
            }
            else
            {
                GameObject[] items = GameObject.FindGameObjectsWithTag(TagNameConst.ITEM_SPAWN);
                foreach (GameObject item in items)
                {
                    Destroy(item);
                }
                enabled = false;
            }
        }

        private void Update()
        {
            _spawnTimer += Time.deltaTime;
            if (_spawnTimer < _spawnInterval) return;

            // �o�ߎ��ԃ��Z�b�g
            _spawnTimer = 0;

            // �ő吔�X�|�[�����Ă���ꍇ�͐V�K�ɃX�|�[�����Ȃ�
            if (_spawnedMap.Count >= _maxSpawnNum) return;

            // ���X�|�[���̃X�|�i�[���W�v
            List<IItemSpawner> notSpawned = new List<IItemSpawner>();
            lock (_spawnedMap)
            {
                foreach (IItemSpawner spawner in _spawnerList)
                {
                    if (!_spawnedMap.ContainsValue(spawner))
                    {
                        notSpawned.Add(spawner);
                    }
                }
            }

            // �X�|�[�����s
            ItemSpawn(notSpawned, _spawnNum);
        }

        /// <summary>
        /// �w�肳�ꂽ���̃A�C�e���X�|�[��
        /// </summary>
        /// <param name="spawnerList">�A�C�e���X�|�[��������X�|�i�[</param>
        /// <param name="spawnNum">�X�|�[����</param>
        private void ItemSpawn(List<IItemSpawner> spawnerList, int spawnNum)
        {
            // �X�|�i�[�̐����X�|�[�����ȉ��̏ꍇ�͑S�ẴX�|�i�[����A�C�e���X�|�[��
            if (spawnerList.Count <= spawnNum)
            {
                foreach (IItemSpawner spawner in spawnerList)
                {
                    // �X�|�[�����s
                    ISpawnItem item = spawner.Spawn();

                    // �A�C�e�����ŃC�x���g�ݒ�
                    item.OnSpawnItemDestroy += OnSpawnItemDestroy;

                    // �X�|�[���ς݃A�C�e���ɒǉ�
                    lock (_spawnedMap) _spawnedMap.Add(item, spawner);
                }

                return;
            }

            // �e�X�|�i�[���烉���_���ɃA�C�e���X�|�[��
            int num = 0;
            while (true)
            {
                foreach (IItemSpawner spawner in spawnerList)
                {
                    // ���ɃX�|�[���ς̏ꍇ�̓X�|�[�����s��Ȃ�
                    if (_spawnedMap.ContainsValue(spawner)) continue;

                    // �X�|�[�����s
                    ISpawnItem item = spawner.SpawnRandom();

                    // �X�|�[��������
                    if (item == null) continue;

                    // �A�C�e�����ŃC�x���g�ݒ�
                    item.OnSpawnItemDestroy += OnSpawnItemDestroy;

                    // �X�|�[���ς݃A�C�e���ɒǉ�
                    lock (_spawnedMap) _spawnedMap.Add(item, spawner);

                    // �w�肳�ꂽ�X�|�[�����ɒB�����ꍇ�͏I��
                    num++;
                    if (num >= spawnNum) return;
                }
            }
        }

        /// <summary>
        /// �X�|�[���A�C�e�����ŃC�x���g
        /// </summary>
        /// <param name="item">���ł����A�C�e���̃X�|�i�[</param>
        private void OnSpawnItemDestroy(object sender, EventArgs e)
        {
            // �A�C�e���擾
            ISpawnItem item = sender as ISpawnItem;

            // ���ł����A�C�e���̃X�|�i�[�擾
            IItemSpawner spawner = _spawnedMap[item];

            // ���ł����A�C�e������C�x���g�폜
            item.OnSpawnItemDestroy -= OnSpawnItemDestroy;

            // �X�|�[���ς݃A�C�e������폜
            lock (_spawnedMap) _spawnedMap.Remove(item);
        }
    }
}