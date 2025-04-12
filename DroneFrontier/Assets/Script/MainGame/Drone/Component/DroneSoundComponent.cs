using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class DroneSoundComponent : MonoBehaviour, IDroneComponent
{
    /// <summary>
    /// 同一SEの最大同時再生可能数
    /// </summary>
    private const int MAX_SAME_PLAY = 2;

    /// <summary>
    /// AudioSource管理クラス
    /// </summary>
    private class AudioSourceData
    {
        /// <summary>
        /// AudioSource
        /// </summary>
        public AudioSource AudioSource { get; set; } = null;

        /// <summary>
        /// 再生SE
        /// </summary>
        public SoundManager.SE SE { get; set; } = SoundManager.SE.None;

        /// <summary>
        /// 未使用であるか
        /// </summary>
        public bool IsFree { get; set; } = true;
    }

    /// <summary>
    /// Audio情報
    /// </summary>
    private AudioSourceData[] _audioDatas = null;

    /// <summary>
    /// SE同時再生用ロック
    /// </summary>
    private  object _lock = new object();

    public void Initialize() { }

    /// <summary>
    /// 指定したSEを再生する
    /// </summary>
    /// <param name="se">再生するSE</param>
    /// <param name="volume">再生音量を0～1で指定</param>
    /// <param name="loop">ループ再生させるか</param>
    /// <returns>SE管理番号。再生に失敗した場合は-1</returns>
    public int Play(SoundManager.SE se, float volume = 1, bool loop = false)
    {
        if (se == SoundManager.SE.None) return -1;

        int index = -1;
        lock (_lock)
        {
            // AudioSourceに空きがあるか調べる
            for (int i = 0; i < _audioDatas.Length; i++)
            {
                AudioSourceData data = _audioDatas[i];
                if (data.IsFree)
                {
                    index = i;
                    break;
                }
            }

            // 空きがなかったら処理しない
            if (index == -1) return -1;

            // 同一SEが既に一定数以上再生されている場合は処理しない
            int sameSe = 0;
            foreach (var data in _audioDatas)
            {
                if (data.SE == se)
                {
                    sameSe++;
                    if (sameSe >= MAX_SAME_PLAY) return -1;
                }
            }

            _audioDatas[index].AudioSource.volume = SoundManager.GetTotalSEVolume(volume);
            _audioDatas[index].AudioSource.clip = SoundManager.GetAudioClip(se);
            _audioDatas[index].AudioSource.loop = loop;
            _audioDatas[index].SE = se;
            _audioDatas[index].IsFree = false;
        }

        // SE再生
        _audioDatas[index].AudioSource.Play();

        return index;
    }

    /// <summary>
    /// 指定した管理番号のSEを停止
    /// </summary>
    /// <param name="id">停止するSEの管理番号</param>
    public void StopSE(int id)
    {
        // 有効なSE管理番号でない場合は処理しない
        if (!IsValidSEId(id)) return;

        AudioSourceData asd = _audioDatas[id];
        if (asd.AudioSource.isPlaying)
        {
            StopAudio(asd);
        }
    }

    private void Awake()
    {
        // ドローンにアタッチされているAudioSourceコンポーネント群を取得
        AudioSource[] audios = GetComponents<AudioSource>();

        // オーディオ初期化
        _audioDatas = new AudioSourceData[audios.Length];
        for (int i = 0; i < audios.Length; i++)
        {
            _audioDatas[i] = new AudioSourceData()
            {
                AudioSource = audios[i]
            };
            StopAudio(_audioDatas[i]);
        }
    }

    private void LateUpdate()
    {
        // 再生が終わったSEがあるかチェック
        for (int i = 0; i < _audioDatas.Length; i++)
        {
            // 再生が終わったAudioSourceを初期化
            AudioSourceData data = _audioDatas[i];
            if (!data.AudioSource.isPlaying && !data.IsFree)
            {
                StopAudio(data);
            }
        }
    }

    /// <summary>
    /// オーディオ停止
    /// </summary>
    /// <param name="audio">停止するオーディオ</param>
    private void StopAudio(AudioSourceData audio)
    {
        audio.AudioSource.Stop();
        audio.AudioSource.clip = null;
        audio.AudioSource.loop = false;
        audio.AudioSource.time = 0;
        audio.SE = SoundManager.SE.None;
        audio.IsFree = true;
    }

    /// <summary>
    /// 有効なSE管理番号であるかチェックする
    /// </summary>
    /// <param name="id">チェックするSE管理番号</param>
    /// <returns>有効な場合はtrue</returns>
    private bool IsValidSEId(int id)
    {
        if (id < 0 || id >= _audioDatas.Length)
        {
            return false;
        }
        return true;
    }
}