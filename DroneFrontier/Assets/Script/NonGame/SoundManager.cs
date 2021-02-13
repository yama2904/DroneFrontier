using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SoundManager : MonoBehaviour
{
    const string BGMFolder = "BGM/";
    const string SEFolder = "SE/";

    protected static AudioClip[] bgmClips;   //BGM群
    protected static AudioClip[] seClips;    //SE群

    public enum BGM
    {
        DRONE_UP,
        LOOP,
        THREE_MIN,

        NONE
    }
    protected static BGM playingBGM = BGM.NONE;

    public enum SE
    {
        BARRIER_DAMAGE,     //バリアダメージ
        BEAM,               //ビーム
        BEAM_1,             //ビーム1
        BEAM_2,             //ビーム2
        BEAM_CAHRGE,        //チャージ
        BEAM_START,         //ビーム開始
        BOOST,              //ブースト
        CANCEL,             //キャンセル
        DEATH,              //死亡
        DESTROY_BARRIER,    //バリア破壊
        EXPLOSION_MISSILE,  //ミサイル爆破
        FALL_BUILDING,      //ビル落下
        FINISH,             //終了
        GATLING,            //ガトリング
        JAMMING_NOISE,      //ジャミングノイズ
        KAMAITACHI,         //かまいたち
        MAGNETIC_AREA,      //磁場エリア
        MISSILE,            //ミサイル
        PROPELLER,          //プロペラ
        RADAR,              //レーダー
        RESPAWN,            //リスポーン
        SELECT,             //選択
        SHOTGUN,            //ショットガン
        START_COUNT_DOWN_D, //開始カウントダウン(ド)
        START_COUNT_DOWN_M, //開始カウントダウン(ミ)
        USE_ITEM,           //アイテム使用
        WALL_STUN,          //壁スタン

        NONE
    }

    //ゲーム全体の音量
    static float baseBGMVolue = 1.0f;   //初期値は最大
    static float baseSEVolume = 1.0f;

    protected class AudioSourceData
    {
        public AudioSource audioSource = null;
        public float gameVolume = 0;
        public bool isPause = false; //一時停止しているか
        public bool isFree = false;  //使っていなかったらtrue
    }
    protected static AudioSourceData bgmAudioData = null;  //BGM用AudioSourceData
    protected static AudioSourceData[] seAudioDatas;       //SE用AudioSourceData

    static float fadeVolume = 0;    //フェードイン又はフェードアウトの1フレームの音量変化量
    static float deltaTime = 0;     //static関数で使えるようにTime.deltaTimeを代入する変数
    static bool isFadeIn = false;   //フェードインするか
    static bool isFadeOut = false;  //フェードアウトするか


    //シーン間をまたいでもSoundManagerオブジェクトが消えない処理
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeBeforeSceneLoad()
    {
        GameObject manager = GameObject.Instantiate(Resources.Load("SoundManager")) as GameObject;
        GameObject.DontDestroyOnLoad(manager);
    }


    void Awake()
    {
        #region SoundLoad

        string[] BGMPath = new string[(int)BGM.NONE];
        BGMPath[(int)BGM.DRONE_UP] = "Drone_up";
        BGMPath[(int)BGM.LOOP] = "LoopBGM";
        BGMPath[(int)BGM.THREE_MIN] = "ThreeMinBGM";

        string[] SEPath = new string[(int)SE.NONE];
        SEPath[(int)SE.BARRIER_DAMAGE] = "BarrierDamage";
        SEPath[(int)SE.BEAM] = "Beam";
        SEPath[(int)SE.BEAM_1] = "Beam_1";
        SEPath[(int)SE.BEAM_2] = "Beam_2";
        SEPath[(int)SE.BEAM_CAHRGE] = "BeamCharge";
        SEPath[(int)SE.BEAM_START] = "BeamStart";
        SEPath[(int)SE.BOOST] = "Boost";
        SEPath[(int)SE.CANCEL] = "Cancel";
        SEPath[(int)SE.DEATH] = "Death";
        SEPath[(int)SE.DESTROY_BARRIER] = "DestroyBarrier";
        SEPath[(int)SE.EXPLOSION_MISSILE] = "ExplosionMissile";
        SEPath[(int)SE.FALL_BUILDING] = "FallBuilding";
        SEPath[(int)SE.FINISH] = "Finish";
        SEPath[(int)SE.GATLING] = "Gatling";
        SEPath[(int)SE.JAMMING_NOISE] = "JammingNoise";
        SEPath[(int)SE.KAMAITACHI] = "Kamaitachi";
        SEPath[(int)SE.MAGNETIC_AREA] = "MagneticArea";
        SEPath[(int)SE.MISSILE] = "Missile";
        SEPath[(int)SE.PROPELLER] = "Propeller";
        SEPath[(int)SE.RADAR] = "Radar";
        SEPath[(int)SE.RESPAWN] = "Respawn";
        SEPath[(int)SE.SELECT] = "Select";
        SEPath[(int)SE.SHOTGUN] = "Shotgun";
        SEPath[(int)SE.START_COUNT_DOWN_D] = "StartCountDown(D)";
        SEPath[(int)SE.START_COUNT_DOWN_M] = "StartCountDown(M)";
        SEPath[(int)SE.USE_ITEM] = "UseItem";
        SEPath[(int)SE.WALL_STUN] = "WallStun";

        //ResourcesファイルからClipをロード
        bgmClips = new AudioClip[(int)BGM.NONE];
        seClips = new AudioClip[(int)SE.NONE];

        //BGMをロード
        for (int i = 0; i < (int)BGM.NONE; i++)
        {
            bgmClips[i] = Resources.Load<AudioClip>(BGMFolder + BGMPath[i]);
        }

        //SEをロード
        for (int i = 0; i < (int)SE.NONE; i++)
        {
            seClips[i] = Resources.Load<AudioClip>(SEFolder + SEPath[i]);
        }

        #endregion

        #region Init

        //AudioSourceDataの初期化
        AudioSource[] audios = GetComponents<AudioSource>();   //全てのAudioSourceコンポーネントを取得

        //BGM用AudioSourceDataの初期化
        AudioSource bgmAudio = audios[0];
        bgmAudio.loop = true;  //BGMはデフォルトでループ
        bgmAudio.playOnAwake = false;  //起動直後再生させない
        bgmAudioData = new AudioSourceData
        {
            audioSource = bgmAudio
        };
        InitAudioSourceData(ref bgmAudioData);

        //SE用AudioSourceDataの初期化
        seAudioDatas = new AudioSourceData[audios.Length - 1];
        for (int i = 0; i < audios.Length - 1; i++)
        {
            AudioSource seAudio = audios[i + 1];
            seAudio.playOnAwake = false;  //起動直後再生させない
            seAudioDatas[i] = new AudioSourceData
            {
                audioSource = audios[i + 1]
            };
            InitAudioSourceData(ref seAudioDatas[i]);
        }

        #endregion
    }

    void Update()
    {
        //再生が終わったSEがあるか走査
        for (int i = 0; i < seAudioDatas.Length; i++)
        {
            AudioSourceData asd = seAudioDatas[i];  //名前省略

            //一時停止しているSEがあったら走査をスキップ
            if (asd.isPause) continue;

            //再生が終わったAudioSourceを初期化
            if (!asd.audioSource.isPlaying)
            {
                if (!asd.isFree)
                {
                    InitAudioSourceData(ref asd);
                }
            }
        }

        /*
         * 注意
         * フェード処理中はAudioSourceDatas.gameVolumeの処理を行っていないので
         * バグが起こる可能性あり
         * 要:いつか修正
         */
        //フェードイン処理
        if (isFadeIn)
        {
            AudioSource audio = bgmAudioData.audioSource;  //名前省略

            //フェード処理
            if (audio.isPlaying)
            {
                audio.volume += AddVolumeBGM(fadeVolume);

                if (audio.volume >= baseBGMVolue)
                {
                    audio.volume = baseBGMVolue;
                    isFadeIn = false;
                    fadeVolume = 0;
                }
            }
        }

        //フェードアウト処理
        if (isFadeOut)
        {
            AudioSource audio = bgmAudioData.audioSource;  //名前省略

            //フェード処理
            if (audio.isPlaying)
            {
                audio.volume -= AddVolumeBGM(fadeVolume);

                if (audio.volume <= 0)
                {
                    audio.volume = 0;
                    isFadeOut = false;
                    fadeVolume = 0;
                }
            }
        }
        deltaTime = Time.deltaTime;
    }

    //AudioClipを取得
    public static AudioClip GetAudioClip(SE se)
    {
        if (se == SE.NONE) return null;  //バグ防止
        return seClips[(int)se];
    }

    #region BGM

    //ゲーム全体のBGMの音量を0～1で設定
    //設定画面での音量の設定など
    public static float BaseBGMVolume
    {
        get { return baseBGMVolue; }
        set
        {
            float v = value;
            if (v < 0)
            {
                v = 0;
            }
            if (v > 1.0f)
            {
                v = 1.0f;
            }
            baseBGMVolue = v;

            AudioSourceData asd = bgmAudioData; //名前省略
            asd.audioSource.volume = AddVolumeBGM(asd.gameVolume);
        }
    }

    //BGMの音量を0～1で変更
    public static void SetGameBGMValume(float volume)
    {
        if (volume < 0)
        {
            volume = 0;
        }
        if (volume > 1.0f)
        {
            volume = 1.0f;
        }
        AudioSourceData asd = bgmAudioData; //名前省略

        asd.gameVolume = volume;
        asd.audioSource.volume = AddVolumeBGM(volume);
    }

    //ループしてBGMを再生
    //複数BGMを再生できない
    public static void Play(BGM bgm, float volume)
    {
        //バグ防止
        if (bgm == BGM.NONE) return;

        if (volume < 0)
        {
            volume = 0;
        }
        if (volume > 1.0f)
        {
            volume = 1.0f;
        }
        AudioSourceData asd = bgmAudioData; //名前省略

        asd.audioSource.Stop();
        asd.gameVolume = volume;
        asd.isFree = false;
        asd.audioSource.volume = AddVolumeBGM(volume);
        asd.audioSource.clip = bgmClips[(int)bgm];
        asd.audioSource.loop = true;
        asd.audioSource.Play();

        playingBGM = bgm;
    }

    //BGMを一時停止
    public static void PauseBGM()
    {
        bgmAudioData.audioSource.Pause();
    }

    //一時停止しているBGMを再開
    public static void UnPauseBGM()
    {
        bgmAudioData.audioSource.UnPause();
    }

    //BGMを停止
    public static void StopBGM()
    {
        AudioSourceData asd = bgmAudioData; //名前省略

        InitAudioSourceData(ref asd);
        playingBGM = BGM.NONE;
        isFadeIn = false;
        isFadeOut = false;
        fadeVolume = 0;
    }

    //再生されているBGMを返す
    public static BGM IsPlayingBGM
    {
        get { return playingBGM; }
    }

    /*
   フェード系処理は1フレームごとの音量変化の桁数が多すぎて
   桁落ちがやばいので指定したtimeより数秒長く音量が
   MAXになるまで時間がかかります
   Debug.Logで確認しないとわからないくらいのバグだったのと
   直せなかったので放置してます
   */

    //フェードイン(徐々にBGMを大きくする)
    //timeは最大音量になるまでの時間
    public static void FadeIn(BGM bgm, float time)
    {
        //バグ防止
        if (bgm == BGM.NONE) return;

        AudioSource audio = bgmAudioData.audioSource;  //名前省略
        audio.clip = bgmClips[(int)bgm];
        audio.Play();

        if (isFadeOut)
        {
            isFadeOut = false;
        }
        isFadeIn = true;
        float diff = 1.0f - audio.volume; //今の音量と最大音量の差
        fadeVolume = (deltaTime / time) * diff;
    }

    //既に鳴らしているBGMをフェードイン
    //BGMを最初から鳴らさずに済む
    public static void FadeIn(float time)
    {
        if (isFadeOut)
        {
            isFadeOut = false;
        }
        isFadeIn = true;
        float diff = 1.0f - bgmAudioData.audioSource.volume; //今の音量と最大音量の差
        fadeVolume = (deltaTime / time) * diff;
    }

    //フェードアウト(徐々にBGMを小さくする)
    //timeは音量が0になるまでの時間
    public static void FadeOut(BGM bgm, float time)
    {
        //バグ防止
        if (bgm == BGM.NONE) return;

        AudioSource audio = bgmAudioData.audioSource;
        audio.clip = bgmClips[(int)bgm];
        audio.Play();

        if (isFadeIn)
        {
            isFadeIn = false;
        }
        isFadeOut = true;
        float diff = audio.volume; //今の音量と最小音量の差
        fadeVolume = (deltaTime / time) * diff;
    }

    //既に鳴らしているBGMをフェードアウト
    //BGMを最初から鳴らさずに済む
    public static void FadeOut(float time)
    {
        if (isFadeIn)
        {
            isFadeIn = false;
        }
        isFadeOut = true;
        float diff = bgmAudioData.audioSource.volume; //今の音量と最小音量の差
        fadeVolume = (deltaTime / time) * diff;
    }

    //フェードイン・フェードアウトを途中で止めて音量をそのままにする
    public static void StopFade()
    {
        isFadeIn = false;
        isFadeOut = false;
        fadeVolume = 0;
    }

    #endregion

    #region SE

    //ゲーム全体のSEの音量を0～1で設定
    //設定画面での音量の設定など
    public static float BaseSEVolume
    {
        get { return baseSEVolume; }
        set
        {
            float v = value;
            if (v < 0)
            {
                v = 0;
            }
            if (v > 1.0f)
            {
                v = 1.0f;
            }
            baseSEVolume = v;

            for (int i = 0; i < seAudioDatas.Length; i++)
            {
                AudioSourceData asd = seAudioDatas[i]; //名前省略
                if (asd.isFree) continue;
                asd.audioSource.volume = AddVolumeSE(asd.gameVolume);
            }
        }
    }

    /*
    概要:
        SEを再生
        10個までSEを再生できる

    引数1:
        再生したいSEをSE列挙型から指定
    引数2:
        ループしたいときはloopをtrue
        ループしないときは引数に何も入れなくてもfalseでもどっちでも
    引数3:
        途中から再生したいときはtimeに値を入力

    戻り値：
        あとで再生したSEを止めたい場合はここの戻り値を
        取っておいてStopSE関数の引数に使ってください
    */
    public static int Play(SE se, float volume, bool loop = false, float time = 0)
    {
        //バグ防止
        if (se == SE.NONE) return -1;

        int index = -1;
        for (int i = 0; i < seAudioDatas.Length; i++)
        {
            AudioSourceData data = seAudioDatas[i];
            if (data.isFree)
            {
                index = i;
                break;
            }
        }

        //空きがなかったら処理しない
        if (index == -1) return -1;

        //空きがあるので処理
        if (volume < 0)
        {
            volume = 0;
        }
        if (volume > 1.0f)
        {
            volume = 1.0f;
        }
        AudioSourceData asd = seAudioDatas[index]; //名前省略

        asd.gameVolume = volume;
        asd.isFree = false;
        asd.audioSource.volume = AddVolumeSE(volume);
        asd.audioSource.clip = seClips[(int)se];
        asd.audioSource.loop = loop;

        //再生時間がclipの長さ以下か調べる
        if (time > 0 && time < seClips[(int)se].length)
        {
            asd.audioSource.time = time;
        }
        asd.audioSource.Play();

        return index;
    }

    //全てのSEを停止
    public static void StopSEAll()
    {
        //FreeSEManagerも整理
        for (int i = 0; i < seAudioDatas.Length; i++)
        {
            AudioSourceData asd = seAudioDatas[i];  //名前省略

            if (asd.isFree) continue;
            InitAudioSourceData(ref asd);
        }
    }

    //Playの時に返した値を引数に渡すと
    //Playで再生したSEを一時停止させる
    public static void PauseSE(int num)
    {
        //AudioSourceの配列内かチェック
        if (!AudioSourceArrayCheck(seAudioDatas, num)) return;

        AudioSourceData asd = seAudioDatas[num];  //名前省略
        if (asd.audioSource.isPlaying)
        {
            asd.audioSource.Pause();
            asd.isPause = true;
        }
    }

    //一時停止したSEを再開する
    public static void UnPauseSE(int num)
    {
        //AudioSourceの配列内かチェック
        if (!AudioSourceArrayCheck(seAudioDatas, num)) return;

        AudioSourceData asd = seAudioDatas[num];  //名前省略
        if (!asd.audioSource.isPlaying)
        {
            asd.audioSource.UnPause();
            asd.isPause = false;
        }
    }
    //Playの時に返した値を引数に渡すと
    //Playで再生したSEを停止させる
    public static void StopSE(int num)
    {
        //AudioSourceの配列内かチェック
        if (!AudioSourceArrayCheck(seAudioDatas, num)) return;

        AudioSourceData asd = seAudioDatas[num];  //名前省略
        if (asd.audioSource.isPlaying)
        {
            InitAudioSourceData(ref asd);
        }
    }

    //ループ設定しているSEのループを停止させる
    //その場でSEが止まるわけではなくclipのSEが鳴り終わるまで再生される
    public static void StopLoopSE(int num)
    {
        //AudioSourceの配列内かチェック
        if (!AudioSourceArrayCheck(seAudioDatas, num)) return;

        AudioSource audio = seAudioDatas[num].audioSource;  //名前省略
        if (audio.loop)
        {
            audio.loop = false;
        }
    }

    //SEが再生されているか
    public static bool IsPlayingSE(int num)
    {
        //AudioSourceの配列内かチェック
        if (!AudioSourceArrayCheck(seAudioDatas, num)) return false;

        if (seAudioDatas[num].audioSource.isPlaying)
        {
            return true;
        }
        return false;
    }

    //SEの長さを返す
    public static float Length(SE se)
    {
        return seClips[(int)se].length;
    }

    #endregion


    //baseVolumeBGMとgameVolumeBGMを合わせた最終的なBGMの音量を取得
    private static float AddVolumeBGM(float volume)
    {
        if (volume < 0)
        {
            volume = 0;
        }
        if (volume > 1.0f)
        {
            volume = 1.0f;
        }
        return volume * baseBGMVolue;
    }

    //baseVolumeSEとgameVolumeSEを合わせた最終的なSEの音量を取得
    private static float AddVolumeSE(float volume)
    {
        if (volume < 0)
        {
            volume = 0;
        }
        if (volume > 1.0f)
        {
            volume = 1.0f;
        }
        return volume * baseSEVolume;
    }

    //AudioSourceの初期化
    static void InitAudioSourceData(ref AudioSourceData audioData)
    {
        audioData.audioSource.Stop();
        audioData.audioSource.clip = null;    //初期化
        audioData.audioSource.volume = 0;
        audioData.audioSource.loop = false;   //ループを解除
        audioData.audioSource.time = 0;       //途中再生を解除

        audioData.gameVolume = 0;
        audioData.isPause = false;
        audioData.isFree = true;
    }

    //AudioSourceの配列内かチェック
    protected static bool AudioSourceArrayCheck(AudioSourceData[] array, int num)
    {
        if (num < 0 || num >= array.Length)
        {
            //配列の範囲外
            return false;
        }
        return true;
    }
}