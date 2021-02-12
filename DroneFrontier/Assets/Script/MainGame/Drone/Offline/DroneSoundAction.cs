using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Offline
{
    [RequireComponent(typeof(AudioSource))]
    public class DroneSoundAction : MonoBehaviour
    {
        //ループ専用
        struct LoopAudioData
        {
            public AudioSource audioSource;
            public bool isFree;  //使用可能か
        }
        LoopAudioData[] loopAudioDatas;

        //1回きりの再生用
        AudioSource oneShotAudio = null;


        void Awake()
        {
            AudioSource[] audios = GetComponents<AudioSource>();
            loopAudioDatas = new LoopAudioData[audios.Length];

            //初期化
            oneShotAudio = audios[0];
            for (int i = 1; i < audios.Length; i++)
            {
                loopAudioDatas[i].audioSource = audios[i];
                loopAudioDatas[i].audioSource.loop = true;
                loopAudioDatas[i].isFree = true;
            }
        }

        void Start()
        {
        }

        void Update()
        {
        }

        public void PlayOneShot(SoundManager.SE se, float volume)
        {
            //バグ防止
            if (se == SoundManager.SE.NONE) return;

            oneShotAudio.PlayOneShot(SoundManager.GetAudioClip(se), volume);
        }

        public int PlayLoopSE(SoundManager.SE se, float volume)
        {
            //バグ防止
            if (se == SoundManager.SE.NONE) return -1;

            //再生可能なAudioSourceを調べる
            for (int i = 0; i < loopAudioDatas.Length; i++)
            {
                LoopAudioData lpd = loopAudioDatas[i];  //名前省略
                if (!lpd.isFree) continue;

                lpd.audioSource.clip = SoundManager.GetAudioClip(se);
                lpd.audioSource.volume = volume;
                lpd.audioSource.Play();
                lpd.isFree = false;
                return i;
            }

            //再生できなかった
            return -1;
        }

        public bool StopLoopSE(int id)
        {
            if (id == -1) return false;
            if (loopAudioDatas.Length >= id) return false;

            LoopAudioData lpd = loopAudioDatas[id];  //名前省略
            if (lpd.isFree) return false;

            lpd.audioSource.Stop();
            lpd.isFree = true;

            return true;
        }
    }
}