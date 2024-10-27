using Offline;
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
    /// �e�h���[���̏����ʒu
    /// </summary>
    private Dictionary<string, Transform> _initPositions = new Dictionary<string, Transform>();

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
    public IBattleDrone SpawnDrone(string name, BaseWeapon.Weapon weapon, bool isPlayer)
    {
        // �X�|�[���ʒu�擾
        Transform spawnPos = _droneSpawnPositions[_nextSpawnIndex];

        // �h���[������
        IBattleDrone drone = CreateDrone(name, weapon, spawnPos, isPlayer);

        // �X�|�[���ʒu��ۑ�
        _initPositions.Add(drone.Name, spawnPos);

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

    void Start() { }

    void Update() { }

    /// <summary>
    /// �h���[������
    /// </summary>
    /// <param name="weapon">�h���[���ɐݒ肷�閼�O</param>
    /// <param name="weapon">�ݒ肷�镐��</param>
    /// <param name="spawnPosition">�X�|�[���ʒu</param>
    /// <param name="isPlayer">�v���C���[�ł��邩</param>
    /// <returns>���������h���[��</returns>
    private IBattleDrone CreateDrone(string name, BaseWeapon.Weapon weapon, Transform spawnPosition, bool isPlayer)
    {
        // �������I�u�W�F�N�g�I��
        GameObject drone = isPlayer ? _playerDrone : _cpuDrone;

        IBattleDrone createdDrone = Instantiate(drone, spawnPosition.position, spawnPosition.rotation).GetComponent<IBattleDrone>();
        createdDrone.Name = name;
        createdDrone.SubWeapon = weapon;
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

        // �j�󂳂ꂽ�h���[���̏����ʒu�擾
        Transform initPos = _initPositions[drone.Name];

        // ���X�|�[���������h���[��
        IBattleDrone respawnDrone = null;

        if (drone.StockNum > 0)
        {
            if (drone is BattleDrone)
            {
                // ���X�|�[��
                respawnDrone = CreateDrone(drone.Name, drone.SubWeapon, initPos, true);

                // ����SE�Đ�
                SoundManager.Play(SoundManager.SE.RESPAWN);
            }
            else
            {
                // ���X�|�[��
                respawnDrone = CreateDrone(drone.Name, drone.SubWeapon, initPos, false);
            }

            // �X�g�b�N���X�V
            respawnDrone.StockNum = drone.StockNum - 1;
        }

        // �C�x���g����
        DroneDestroyEvent?.Invoke(drone, respawnDrone);

        // �j�󂳂ꂽ�h���[������C�x���g�̍폜
        drone.DroneDestroyEvent -= DroneDestroy;
    }
}
