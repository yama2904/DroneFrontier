using System;
using UnityEngine;

public interface IWeapon
{
    /// <summary>
    /// ���폊�L��
    /// </summary>
    public GameObject Owner { get; set; }

    /// <summary>
    /// �e�۔��ˍ��W
    /// </summary>
    public Transform ShotPosition { get; set; }

    /// <summary>
    /// �c�eUI�\��Canvas
    /// </summary>
    public Canvas BulletUICanvas { get; set; }

    /// <summary>
    /// �S�e��[�C�x���g
    /// </summary>
    public event EventHandler OnBulletFull;

    /// <summary>
    /// �c�e�����C�x���g
    /// </summary>
    public event EventHandler OnBulletEmpty;

    /// <summary>
    /// �e�۔���
    /// </summary>
    /// <param name="target">�Ǐ]��I�u�W�F�N�g</param>
    public void Shot(GameObject target = null);
}
