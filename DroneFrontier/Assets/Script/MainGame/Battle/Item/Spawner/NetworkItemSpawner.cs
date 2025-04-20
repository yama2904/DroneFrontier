using Drone.Battle;
using UnityEngine;

namespace Network
{
    public class NetworkItemSpawner : MyNetworkBehaviour, IItemSpawner
    {
        /// <summary>
        /// �X�|�[���m���i0�`1�j
        /// </summary>
        public float SpawnPercent
        {
            get { return _spawnPercent; }
        }

        [SerializeField, Tooltip("�X�|�[��������A�C�e���ꗗ")]
        private MyNetworkBehaviour[] _spawnItems = null;

        [SerializeField, Tooltip("�X�|�[���m��(0�`1)")]
        private float _spawnPercent = 0.5f;

        /// <summary>
        /// �L���b�V���pTransform
        /// </summary>
        private Transform _transform = null;

        /// <summary>
        /// �����_���ȃA�C�e�����X�|�[��������
        /// </summary>
        /// <returns>�X�|�[�������A�C�e��</returns>
        public ISpawnItem Spawn()
        {
            // �����_���ȃA�C�e�����X�|�[��
            int index = Random.Range(0, _spawnItems.Length);
            MyNetworkBehaviour item = Instantiate(_spawnItems[index], _transform);
            item.transform.SetParent(_transform);

            // �S�N���C�A���g�ɃX�|�[��������
            NetworkObjectSpawner.Spawn(item);

            // �X�|�[�������A�C�e����Ԃ�
            return item.GetComponent<ISpawnItem>();
        }

        /// <summary>
        /// �X�|�[���m������ɐ����ۂ����肵�A���������ꍇ�̓����_���ȃA�C�e�����X�|�[��������
        /// </summary>
        /// <returns>�X�|�[�������A�C�e���B���s�����ꍇ��null</returns>
        public ISpawnItem SpawnRandom()
        {
            // �X�|�[���m������ɐ����ۂ�����
            if (Random.Range(0, 101) > _spawnPercent * 100) return null;

            // �A�C�e��
            return Spawn();
        }

        protected override void Awake()
        {
            // Transform���L���b�V���ۑ����Ă���
            _transform = transform;
        }
    }
}