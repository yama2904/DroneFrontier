using UnityEngine;

public class JammingBotDamage : MonoBehaviour, IDamageable
{
    public GameObject NoDamageObject => Creater;

    public GameObject Creater { get; set; } = null;

    /// <summary>
    /// ジャミングボットの残りHP
    /// </summary>
    public float HP
    {
        get { return _hp; }
    }

    [SerializeField]
    private float _hp = 30.0f;

    [SerializeField, Tooltip("ジャミングボット本体")]
    private GameObject _jammingBot;

    public void Damage(GameObject source, float value)
    {
        // 小数点第2以下切り捨て
        value = Useful.Floor(value, 1);
        _hp -= value;
        if (_hp < 0)
        {
            // オブジェクト削除
            Destroy(_jammingBot);
        }

        Debug.Log($"{name}に{value}のダメージ 残りHP:{_hp}");
    }
}
