using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class DroneSoundComponent : MonoBehaviour, IDroneComponent
{
    /// <summary>
    /// 1回きりのSE再生用AudioSource
    /// </summary>
    private AudioSource _oneShotAudio = null;

    /// <summary>
    /// ループSE再生用AudioSource
    /// </summary>
    private AudioSource[] _loopPlayAudios = null;

    /// <summary>
    /// 現在ループ再生中のAudioSource<br/>
    /// key:採番したSE再生番号<br/>
    /// value:ループSEを再生しているAudioSource
    /// </summary>
    private Dictionary<int, AudioSource> _loopPlayingAudioMap = new Dictionary<int, AudioSource>();

    /// <summary>
    /// SE再生番号採番値
    /// </summary>
    private int _numberingSeNumber = 0;

    public void Initialize() { }

    /// <summary>
    /// ループせずにSE再生用
    /// </summary>
    /// <param name="se">再生するSE</param>
    /// <param name="volume">再生音量を0～1で指定（指定しない場合はSoundManagerのSE音量を使用）</param>
    public void PlayOneShot(SoundManager.SE se, float volume = -1)
    {
        if (se == SoundManager.SE.None) return;

        if (volume == -1)
        {
            volume = SoundManager.MasterSEVolume;
        }
        _oneShotAudio.PlayOneShot(SoundManager.GetAudioClip(se), volume);
    }

    /// <summary>
    /// ループしてSE再生用<br/>
    /// 再生に失敗した場合は-1を返却
    /// </summary>
    /// <param name="se">再生するSE</param>
    /// <param name="volume">再生音量を0～1で指定（指定しない場合はSoundManagerのSE音量を使用）</param>
    /// <returns>SE再生番号（SEを停止する際に使用）</returns>
    public int PlayLoopSE(SoundManager.SE se, float volume = -1)
    {
        if (se == SoundManager.SE.None) return -1;

        if (volume == -1)
        {
            volume = SoundManager.MasterSEVolume;
        }

        //再生可能なAudioSourceを調べる
        foreach (AudioSource audio in _loopPlayAudios)
        {
            // 再生中の場合は次のAudioSource
            if (audio.isPlaying) continue;

            // AudioSourceに再生SE設定
            audio.clip = SoundManager.GetAudioClip(se);
            audio.volume = volume;
            audio.Play();

            // SE再生番号採番
            int seNumber = _numberingSeNumber++;

            // ループ再生中AudioSourceとしてMapに追加
            _loopPlayingAudioMap.Add(seNumber, audio);

            // SE再生番号返却
            return seNumber;
        }

        //再生できなかった
        return -1;
    }

    /// <summary>
    /// SE再生番号を指定してループ再生中のSEを停止
    /// </summary>
    /// <param name="seNumber">停止するSE再生番号</param>
    /// <returns>成功した場合はtrue</returns>
    public bool StopLoopSE(int seNumber)
    {
        // SE番号は0以上
        if (seNumber <= -1) return false;

        // ループ再生中マップに指定されたSE再生番号が存在しない場合は失敗
        if (!_loopPlayingAudioMap.ContainsKey(seNumber)) return false;

        // SE停止
        _loopPlayingAudioMap[seNumber].Stop();
        _loopPlayingAudioMap.Remove(seNumber);

        return true;
    }

    private void Awake()
    {
        // ドローンにアタッチされているAudioSourceコンポーネント群を取得
        AudioSource[] audios = GetComponents<AudioSource>();

        // oneShot用AudioSource設定
        _oneShotAudio = audios[0];

        // ループ用AudioSource設定
        _loopPlayAudios = new AudioSource[audios.Length - 1];
        for (int i = 1; i < audios.Length; i++)
        {
            audios[i].loop = true;
            _loopPlayAudios[i - 1] = audios[i];
        }
    }
}