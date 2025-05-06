using Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

public class ConfigManager
{
    /// <summary>
    /// �ݒ�t�@�C����
    /// </summary>
    private const string CONFIG_FILE_NAME = "config.json";

    /// <summary>
    /// �ݒ�t�@�C���p�X
    /// </summary>
    private static readonly string CONFIG_FILE_PATH;

    /// <summary>
    /// �ݒ�t�@�C���G���R�[�f�B���O
    /// </summary>
    private static readonly Encoding CONFIG_FILE_ENCODING = Encoding.UTF8;

    private const string BGM_KEY = "bgm";
    private const string SE_KEY = "se";
    private const string BRIGHTNESS_KEY = "brightness";
    private const string CAMERA_KEY = "camera";

    private static SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    static ConfigManager()
    {
#if UNITY_EDITOR
        CONFIG_FILE_PATH = CONFIG_FILE_NAME;
#else
        CONFIG_FILE_PATH = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CONFIG_FILE_NAME);
#endif
    }

    /// <summary>
    /// �ݒ�t�@�C����������
    /// </summary>
    public static async void WriteConfig()
    {
        try
        {
            await _semaphore.WaitAsync(10 * 1000);

            Dictionary<string, float> config = new Dictionary<string, float>()
            {
                {BGM_KEY,  SoundManager.MasterBGMVolume},
                {SE_KEY,  SoundManager.MasterSEVolume},
                {BRIGHTNESS_KEY,  BrightnessManager.Brightness},
                {CAMERA_KEY,  CameraManager.CameraSpeed}
            };

            using (StreamWriter writer = new StreamWriter(CONFIG_FILE_PATH, false, CONFIG_FILE_ENCODING))
            {
                await writer.WriteAsync(JsonConvert.SerializeObject(config));
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// �ݒ�t�@�C���ǂݍ���
    /// </summary>
    public static async void ReadConfig()
    {
        // config�t�@�C�������݂��Ȃ��ꍇ�͏������Ȃ�
        if (!File.Exists(CONFIG_FILE_PATH)) return;

        Dictionary<string, float> config;
        using (StreamReader reader = new StreamReader(CONFIG_FILE_PATH, CONFIG_FILE_ENCODING))
        {
            string json = await reader.ReadToEndAsync();
            config = JsonConvert.DeserializeObject<Dictionary<string, float>>(json);
        }

        SoundManager.MasterBGMVolume = config[BGM_KEY];
        SoundManager.MasterSEVolume = config[SE_KEY];
        BrightnessManager.Brightness = config[BRIGHTNESS_KEY];
        CameraManager.CameraSpeed = config[CAMERA_KEY];
    }
}
