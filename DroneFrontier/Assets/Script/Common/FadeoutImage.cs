using System;
using UnityEngine;
using UnityEngine.UI;

public class FadeoutImage : MonoBehaviour
{
    /// <summary>
    /// フェードアウト時間（秒）
    /// </summary>
    public float FadeoutSec { get; set; } = 0;

    /// <summary>
    /// フェードアウト終了イベント
    /// </summary>
    public event EventHandler FadeoutEndEvent;

    [SerializeField, Tooltip("フェードアウトさせる画像")] 
    private Image _image = null;

    /// <summary>
    /// フェードアウト経過時間
    /// </summary>
    private float _time = 0f;

    private void Update()
    {
        _time += Time.deltaTime;
        if (_time < FadeoutSec)
        {
            float alpha = 1.0f - _time / FadeoutSec;
            Color color = _image.color;
            color.a = alpha;
            _image.color = color;
        }
        else
        {
            // フェードアウト終了イベントを発火してスクリプト停止
            FadeoutEndEvent?.Invoke(this, EventArgs.Empty);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        _time = 0;
    }
}
