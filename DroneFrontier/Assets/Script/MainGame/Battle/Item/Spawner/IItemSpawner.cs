public interface IItemSpawner
{
    /// <summary>
    /// スポーン確率（0～1）
    /// </summary>
    public float SpawnPercent { get; }

    /// <summary>
    /// ランダムなアイテムをスポーンさせる
    /// </summary>
    /// <returns>スポーンしたアイテム</returns>
    public ISpawnItem Spawn();

    /// <summary>
    /// スポーン確率を基に成功可否を決定し、成功した場合はランダムなアイテムをスポーンさせる
    /// </summary>
    /// <returns>スポーンしたアイテム。失敗した場合はnull</returns>
    public ISpawnItem SpawnRandom();
}
