using UnityEngine;

public class QualityAdjuster : MonoBehaviour
{
    private const int MIN_QUALITY_LEVEL = 0;
    private const int MAX_QUALITY_LEVEL = 5;

    [SerializeField, Tooltip("�t���[�����[�g�`�F�b�N�Ԋu�i�b�j")]
    private float _checkInterval = 1f;

    [SerializeField, Tooltip("�w�肵���t���[�����[�g�������ƕi����������")]
    private int _qualityDownFps = 30;

    [SerializeField, Tooltip("�w�肵���t���[�����[�g������ƕi�����グ��")]
    private int _qualityUpFps = 100;

    [SerializeField, Tooltip("�w�肵�����ԁi�b�j�t���[�����[�g��臒l�ɒB���Ă���ꍇ�͕i����ύX����")]
    private int _qualityChangeSec = 5;

    /// <summary>
    /// ���݂̃t���[�����[�g
    /// </summary>
    private int _currentFps = 0;

    /// <summary>
    /// �O��`�F�b�N���_����̌o�߃t���[����
    /// </summary>
    private int _frameCount = 0;

    /// <summary>
    /// �O��`�F�b�N����
    /// </summary>
    private float _prevCheckTime = 0;

    private float _qualityChangeTimer = 0;

    private void Update()
    {
        _frameCount++;
        float time = Time.realtimeSinceStartup - _prevCheckTime;
        if (time < _checkInterval) return;

        // FPS�v�Z
        _currentFps = Mathf.CeilToInt(_frameCount / time);

        _frameCount = 0;
        _prevCheckTime = Time.realtimeSinceStartup;

        // FPS���Ⴂ�ꍇ�͕i����������
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

        // FPS�������ꍇ�͕i�����グ��
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