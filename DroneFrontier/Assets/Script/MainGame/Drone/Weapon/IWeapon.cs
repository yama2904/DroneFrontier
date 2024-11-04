using UnityEngine;

public interface IWeapon
{
    /// <summary>
    /// ���폊�L��
    /// </summary>
    GameObject Owner { get; set; }

    /// <summary>
    /// �e�۔��ˍ��W
    /// </summary>
    Transform ShotPosition { get; set; }

    /// <summary>
    /// �c�eUI�\��Canvas
    /// </summary>
    Canvas BulletUICanvas { get; set; }

    /// <summary>
    /// �e�۔���
    /// </summary>
    /// <param name="target">�Ǐ]��I�u�W�F�N�g</param>
    void Shot(GameObject target = null);
}
