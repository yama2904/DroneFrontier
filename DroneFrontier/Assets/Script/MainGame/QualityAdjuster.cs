using UnityEngine;

public class QualityAdjuster : MonoBehaviour
{
    private const int MIN_QUALITY_LEVEL = 0;
    private const int MAX_QUALITY_LEVEL = 5;

    [SerializeField, Tooltip("フレームレートチェック間隔（秒）")]
    private float _checkInterval = 1f;

    [SerializeField, Tooltip("指定したフレームレートを下回ると品質を下げる")]
    private int _qualityDownFps = 30;

    [SerializeField, Tooltip("指定したフレームレートを上回ると品質を上げる")]
    private int _qualityUpFps = 100;

    [SerializeField, Tooltip("指定した時間（秒）フレームレートが閾値に達している場合は品質を変更する")]
    private int _qualityChangeSec = 5;

    /// <summary>
    /// 現在のフレームレート
    /// </summary>
    private int _currentFps = 0;

    /// <summary>
    /// 前回チェック時点からの経過フレーム数
    /// </summary>
    private int _frameCount = 0;

    /// <summary>
    /// 前回チェック時間
    /// </summary>
    private float _prevCheckTime = 0;

    private float _qualityChangeTimer = 0;

    private void Update()
    {
        _frameCount++;
        float time = Time.realtimeSinceStartup - _prevCheckTime;
        if (time < _checkInterval) return;

        // FPS計算
        _currentFps = Mathf.CeilToInt(_frameCount / time);

        _frameCount = 0;
        _prevCheckTime = Time.realtimeSinceStartup;

        // FPSが低い場合は品質を下げる
        if (_currentFps < _qualityDownFps)
        {
            if (_qualityChangeTimer < _qualityChangeSec)
            {
                _qualityChangeTimer += _checkInterval;
                return;
            }

            int currQuality = QualitySettings.GetQualityLevel();
            if (currQuality > MIN_QUALITY_LEVEL)
            {
                QualitySettings.SetQualityLevel(currQuality - 1);
            }
        }

        // FPSが高い場合は品質を上げる
        if (_currentFps > _qualityUpFps)
        {
            if (_qualityChangeTimer < _qualityChangeSec)
            {
                _qualityChangeTimer += _checkInterval;
                return;
            }

            int currQuality = QualitySettings.GetQualityLevel();
            if (currQuality < MAX_QUALITY_LEVEL)
            {
                QualitySettings.SetQualityLevel(currQuality + 1);
            }
        }

        _qualityChangeTimer = 0;
    }
}