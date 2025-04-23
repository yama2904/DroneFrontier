using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public class BuildingSink : MonoBehaviour
{
    [SerializeField, Tooltip("沈下する回数")] 
    private int _sinkCount = 0;

    [SerializeField, Tooltip("沈下N回目の沈下速度")]
    private float[] _speeds = null;

    [SerializeField, Tooltip("沈下N回目の沈下開始時間（秒）")]
    private float[] _startTimes = null;

    [SerializeField, Tooltip("沈下N回目の沈下が終了するY座標")]
    private float[] _downEndPositions = null;

    [SerializeField, Tooltip("沈下させるビルオブジェクト")]
    private Transform _building = null;

    [SerializeField, Tooltip("砂埃ParticleSystem")]
    private GameObject _particle = null;

    [SerializeField, Tooltip("ビルと一緒に沈下させるオブジェクト")]
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

        // オブジェクトの沈下
        _building.Translate(0, _speeds[_currentStep] * Time.deltaTime * -1, 0);
        foreach (Transform t in _linkObjects)
        {
            t.Translate(0, _speeds[_currentStep] * Time.deltaTime * -1, 0);
        }

        // 沈下停止ラインの判定
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
