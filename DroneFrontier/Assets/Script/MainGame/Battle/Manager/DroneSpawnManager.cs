using Common;
using Drone.Battle;
using System;
using System.Collections.Generic;
using UnityEngine;

public class DroneSpawnManager : MonoBehaviour
{
    /// <summary>
    /// �h���[���j��C�x���g
    /// </summary>
    /// <param name="destroyDrone">�j�󂳂ꂽ�h���[��</param>
    /// <param name="respawnDrone">���X�|�[�������h���[���i�c�@�������Ȃ����ꍇ��null�j</param>
    public delegate void DroneDestroyHandler(IBattleDrone destroyDrone, IBattleDrone respawnDrone);

    /// <summary>
    /// �h���[���j��C�x���g
    /// </summary>
    public event DroneDestroyHandler DroneDestroyEvent;

    [SerializeField, Tooltip("�v���C���[�h���[��")]
    private GameObject _playerDrone = null;

    [SerializeField, Tooltip("CPU�h���[��")]
    private GameObject _cpuDrone = null;

    [SerializeField, Tooltip("�h���[���X�|�[���ʒu")]
    private Transform[] _droneSpawnPositions = null;

    /// <summary>
    /// �e�h���[���̏������
    /// </summary>
    private Dictionary<string, (WeaponType weapon, Transform pos)> _initDatas = new Dictionary<string, (WeaponType weapon, Transform pos)>();

    /// <summary>
    /// ���̃X�|�[�����Ɏg�p����z��C���f�b�N�X
    /// </summary>
    private int _nextSpawnIndex = 0;

    /// <summary>
    /// �h���[�����X�|�[��������
    /// </summary>
    /// <param name="name">�X�|�[��������h���[���̖��O</param>
    /// <param name="weapon">�X�|�[��������h���[���̃T�u����</param>
    /// <param name="isPlayer">�v���C���[�ł��邩</param>
    /// <returns>�X�|�[���������h���[��</returns>
    public IBattleDrone SpawnDrone(string name, WeaponType weapon, bool isPlayer)
    {
        // �X�|�[���ʒu�擾
        Transform spawnPos = _droneSpawnPositions[_nextSpawnIndex];

        // �h���[������
        IBattleDrone drone = CreateDrone(spawnPos, isPlayer);
        IWeapon main = WeaponCreater.CreateWeapon(WeaponType.GATLING);
        IWeapon sub = WeaponCreater.CreateWeapon(weapon);
        drone.Initialize(name, main, sub, drone.StockNum);

        // �X�|�[�����_����ۑ�
        _initDatas.Add(drone.Name, (weapon, spawnPos));

        // ���̃X�|�[���ʒu
        _nextSpawnIndex++;
        if (_nextSpawnIndex >= _droneSpawnPositions.Length)
        {
            _nextSpawnIndex = 0;
        }

        return drone;
    }

    private void Awake()
    {
        // �����X�|�[���ʒu�������_���ɑI��
        _nextSpawnIndex = UnityEngine.Random.Range(0, _droneSpawnPositions.Length);
    }

    /// <summary>
    /// �h���[������
    /// </summary>
    /// <param name="spawnPosition">�X�|�[���ʒu</param>
    /// <param name="isPlayer">�v���C���[�ł��邩</param>
    /// <returns>���������h���[��</returns>
    private IBattleDrone CreateDrone(Transform spawnPosition, bool isPlayer)
    {
        // �������I�u�W�F�N�g�I��
        GameObject drone = isPlayer ? _playerDrone : _cpuDrone;

        IBattleDrone createdDrone = Instantiate(drone, spawnPosition.position, spawnPosition.rotation).GetComponent<IBattleDrone>();
        createdDrone.DroneDestroyEvent += DroneDestroy;

        return createdDrone;
    }

    /// <summary>
    /// �h���[���j��C�x���g
    /// </summary>
    /// <param name="sender">�C�x���g�I�u�W�F�N�g</param>
    /// <param name="e">�C�x���g����</param>
    private void DroneDestroy(object sender, EventArgs e)
    {
        IBattleDrone drone = sender as IBattleDrone;

        // �j�󂳂ꂽ�h���[���̏������擾
        var initData = _initDatas[drone.Name];

        // ���X�|�[���������h���[��
        IBattleDrone respawnDrone = null;

        if (drone.StockNum > 0)
        {
            if (drone is BattleDrone)
            {
                // ���X�|�[��
                respawnDrone = CreateDrone(initData.pos, true);

                // ����SE�Đ�
                SoundManager.Play(SoundManager.SE.Respawn);
            }
            else
            {
                // ���X�|�[��
                respawnDrone = CreateDrone(initData.pos, false);
            }

            // �h���[��������
            IWeapon main = WeaponCreater.CreateWeapon(WeaponType.GATLING);
            IWeapon sub = WeaponCreater.CreateWeapon(initData.weapon);
            respawnDrone.Initialize(drone.Name, main, sub, drone.StockNum - 1);
        }

        // �C�x���g����
        DroneDestroyEvent?.Invoke(drone, respawnDrone);

        // �j�󂳂ꂽ�h���[������C�x���g�̍폜
        drone.DroneDestroyEvent -= DroneDestroy;
    }
}
