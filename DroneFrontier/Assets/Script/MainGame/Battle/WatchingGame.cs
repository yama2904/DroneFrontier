using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WatchingGame : MonoBehaviour
{
    [SerializeField, Tooltip("�h���[���X�|�[���Ǘ��I�u�W�F�N�g")]
    private DroneSpawnManager _droneSpawnManager = null;

    /// <summary>
    /// �ϐ풆��CPU�h���[��
    /// </summary>
    private List<CpuBattleDrone> _watchDrones = new List<CpuBattleDrone>();

    /// <summary>
    /// ���݃J�����Q�ƒ��̃h���[���̃C���f�b�N�X
    /// </summary>
    private int _watchingDrone = 0;

    private void Start() { }

    private void Update()
    {
        if (_watchDrones.Count <= 0) return;

        // �X�y�[�X�L�[�Ŏ���CPU�փJ�����؂�ւ�
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // �J�����[�x������
            _watchDrones[_watchingDrone].SetCameraDepth(0);

            // ����CPU
            _watchingDrone++;
            if (_watchingDrone >= _watchDrones.Count)
            {
                _watchingDrone = 0;
            }

            // �J�����Q�Ɛݒ�
            _watchDrones[_watchingDrone].SetCameraDepth(5);
        }
    }

    private void OnEnable()
    {
        // ��������CPU�擾
        _watchDrones = FindObjectsByType<CpuBattleDrone>(FindObjectsSortMode.None).ToList();

        // �S�Ẵh���[���̃J�����[�x������
        foreach (CpuBattleDrone drone in _watchDrones)
        {
            drone.SetCameraDepth(0);
        }

        // �Q�Ɛ�J�����ݒ�
        _watchingDrone = 0;
        _watchDrones[_watchingDrone].SetCameraDepth(5);

        // �h���[���j��C�x���g�ݒ�
        _droneSpawnManager.DroneDestroyEvent += DroneDestroy;

        // AudioListener�L����
        GetComponent<AudioListener>().enabled = true;
    }


    private void OnDisable()
    {
        // �S�Ẵh���[���̃J�����[�x������
        foreach (CpuBattleDrone drone in _watchDrones)
        {
            drone.SetCameraDepth(0);
        }

        // �h���[���j��C�x���g�폜
        _droneSpawnManager.DroneDestroyEvent -= DroneDestroy;

        // AudioListener������
        GetComponent<AudioListener>().enabled = false;
    }

    /// <summary>
    /// �h���[���j��C�x���g
    /// </summary>
    /// <param name="destroyDrone">�j�󂳂ꂽ�h���[��</param>
    /// <param name="respawnDrone">���X�|�[�������h���[��</param>
    private void DroneDestroy(IBattleDrone destroyDrone, IBattleDrone respawnDrone)
    {
        if (destroyDrone is CpuBattleDrone drone)
        {
            // �j�󂳂ꂽ�h���[�������X�|�[�������h���[���ɓ���ւ���
            int index = _watchDrones.IndexOf(drone);
            _watchDrones.RemoveAt(index);
            _watchDrones.Insert(index, drone);

            // �j�󂳂ꂽ�h���[�������݊ϐ풆��CPU�̏ꍇ�̓J�����[�x����
            if (index == _watchingDrone)
            {
                drone.SetCameraDepth(5);
            }
             else
            {
                // �ϐ풆CPU�łȂ��ꍇ�̓J�����[�x������
                drone.SetCameraDepth(0);
            }
        }
    }
}
