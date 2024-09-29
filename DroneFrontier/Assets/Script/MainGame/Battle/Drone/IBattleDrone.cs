using Offline;
using System;
using UnityEngine.EventSystems;

public interface IBattleDrone
{
    // <summary>
    /// �h���[���̖��O
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// ���݂̃X�g�b�N��
    /// </summary>
    public int StockNum { get; set; }

    /// <summary>
    /// �T�u����
    /// </summary>
    public BaseWeapon.Weapon SubWeapon { get; set; }

    /// <summary>
    /// �h���[���j��C�x���g
    /// </summary>
    public event EventHandler DroneDestroyEvent;
}