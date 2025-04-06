public interface IItemSpawner
{
    /// <summary>
    /// �X�|�[���m���i0�`1�j
    /// </summary>
    public float SpawnPercent { get; }

    /// <summary>
    /// �����_���ȃA�C�e�����X�|�[��������
    /// </summary>
    /// <returns>�X�|�[�������A�C�e��</returns>
    public ISpawnItem Spawn();

    /// <summary>
    /// �X�|�[���m������ɐ����ۂ����肵�A���������ꍇ�̓����_���ȃA�C�e�����X�|�[��������
    /// </summary>
    /// <returns>�X�|�[�������A�C�e���B���s�����ꍇ��null</returns>
    public ISpawnItem SpawnRandom();
}
