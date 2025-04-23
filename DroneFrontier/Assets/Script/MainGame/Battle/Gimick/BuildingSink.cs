using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public class BuildingSink : MonoBehaviour
{
    [SerializeField, Tooltip("���������")] 
    private int _sinkCount = 0;

    [SerializeField, Tooltip("����N��ڂ̒������x")]
    private float[] _speeds = null;

    [SerializeField, Tooltip("����N��ڂ̒����J�n���ԁi�b�j")]
    private float[] _startTimes = null;

    [SerializeField, Tooltip("����N��ڂ̒������I������Y���W")]
    private float[] _downEndPositions = null;

    [SerializeField, Tooltip("����������r���I�u�W�F�N�g")]
    private Transform _building = null;

    [SerializeField, Tooltip("����ParticleSystem")]
    private GameObject _particle = null;

    [SerializeField, Tooltip("�r���ƈꏏ�ɒ���������I�u�W�F�N�g")]
    private Transform[] _linkObjects = null;
    
    private int _currentStep = 0;
    private bool _isSink = false;
    private CancellationTokenSource _cancel = new CancellationTokenSource();

    private async void Start()
    {
        _particle.SetActive(false);

        try
        {
            for (int step = 0; step < _sinkCount; step++)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_startTimes[step]), cancellationToken: _cancel.Token);
                _particle.SetActive(true);
                _currentStep = step;
                _isSink = true;
            }
        }
        catch (OperationCanceledException)
        {

        }
    }

    private void Update()
    {
        if (!_isSink) return;

        // �I�u�W�F�N�g�̒���
        _building.Translate(0, _speeds[_currentStep] * Time.deltaTime * -1, 0);
        foreach (Transform t in _linkObjects)
        {
            t.Translate(0, _speeds[_currentStep] * Time.deltaTime * -1, 0);
        }

        // ������~���C���̔���
        if (_building.localPosition.y <= _downEndPositions[_currentStep])
        {
            _particle.SetActive(false);
            _isSink = false;
            if (_currentStep == _sinkCount - 1)
            {
                Destroy(gameObject);
            }
        }
    }

    private void OnDestroy()
    {
        _cancel.Cancel();
    }
}
