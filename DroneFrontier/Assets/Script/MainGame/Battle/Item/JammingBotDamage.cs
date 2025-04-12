using UnityEngine;

public class JammingBotDamage : MonoBehaviour, IDamageable
{
    public GameObject NoDamageObject => Creater;

    public GameObject Creater { get; set; } = null;

    /// <summary>
    /// �W���~���O�{�b�g�̎c��HP
    /// </summary>
    public float HP
    {
        get { return _hp; }
    }

    [SerializeField]
    private float _hp = 30.0f;

    [SerializeField, Tooltip("�W���~���O�{�b�g�{��")]
    private GameObject _jammingBot;

    public void Damage(GameObject source, float value)
    {
        // �����_��2�ȉ��؂�̂�
        value = Useful.Floor(value, 1);
        _hp -= value;
        if (_hp < 0)
        {
            // �I�u�W�F�N�g�폜
            Destroy(_jammingBot);
        }

        Debug.Log($"{name}��{value}�̃_���[�W �c��HP:{_hp}");
    }
}
