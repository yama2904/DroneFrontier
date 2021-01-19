using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    const string BGMFolder = "BGM/";
    const string SEFolder = "SE/";

    protected static AudioClip[] BGMClips;   //BGM群
    protected static AudioClip[] SEClips;    //SE群

    public enum BGM
    {
        DRONE_UP,

        NONE
    }
    protected static BGM playingBGM = BGM.NONE;

    public enum SE
    {
        ACCELERAION,        //加速
        BARRIER_DAMAGE,     //バリアダメージ
        BEAM_1,             //ビーム1
        BEAM_2,             //ビーム2
        BEAM_CAHRGE,        //チャージ
        BEAM_START,         //ビーム開始
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

    //AudioSource配列用
    protected enum Audio
    {
        BGM,
        SE_1,   //SEは10個まで再生可
        SE_2,
        SE_3,
        SE_4,
        SE_5,
        SE_6,
        SE_7,
        SE_8,
        SE_9,
        SE_10,

        NONE
    }

    //ゲーム全体の音量
    static float baseBGMVolue = 1.0f;   //初期値は最大
    static float baseSEVolume = 1.0f;

    protected class AudioSourceData
    {
        public AudioSource audioSource;
        public float gameVolume;
        public bool isPause; //一時停止しているか
        public bool isFree;  //使っていなかったらtrue
    }
    protected static AudioSourceData[] audioSourceDatas;

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
        string[] BGMPath = new string[(int)BGM.NONE];
        BGMPath[(int)BGM.DRONE_UP] = "Drone_up";

        string[] SEPath = new string[(int)SE.NONE];
        SEPath[(int)SE.ACCELERAION] = "Acceleration";
        SEPath[(int)SE.BARRIER_DAMAGE] = "BarrierDamage";
        SEPath[(int)SE.BEAM_1] = "Beam_1";
        SEPath[(int)SE.BEAM_2] = "Beam_2";
        SEPath[(int)SE.BEAM_CAHRGE] = "BeamCharge";
        SEPath[(int)SE.BEAM_START] = "BeamStart";
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
        BGMClips = new AudioClip[(int)BGM.NONE];
        SEClips = new AudioClip[(int)SE.NONE];

        //BGMをロード
        for (int i = 0; i < (int)BGM.NONE; i++)
        {
            BGMClips[i] = Resources.Load<AudioClip>(BGMFolder + BGMPath[i]);
        }

        //SEをロード
        for (int i = 0; i < (int)SE.NONE; i++)
        {
            SEClips[i] = Resources.Load<AudioClip>(SEFolder + SEPath[i]);
        }

        //AudioSourceの初期化
        audioSourceDatas = new AudioSourceData[(int)Audio.NONE];
        AudioSource[] audios = GetComponents<AudioSource>();   //AudioSourceコンポーネントが複数いるので
        for (int i = 0; i < (int)Audio.NONE; i++)
        {
            audioSourceDatas[i] = new AudioSourceData();
            audioSourceDatas[i].audioSource = audios[i];
        }


        //BGMの初期化
        AudioSourceData BGMDatas = audioSourceDatas[(int)Audio.BGM];    //名前省略
        InitAudioSourceData(ref BGMDatas);
        BGMDatas.audioSource.loop = true;             //BGMはデフォルトでループさせる
        BGMDatas.audioSource.playOnAwake = false;     //オブジェクト起動後再生させない

        //SEの初期化
        for (int i = (int)Audio.SE_1; i < (int)Audio.NONE; i++)
        {
            AudioSourceData asd = audioSourceDatas[i];  //名前省略

            InitAudioSourceData(ref audioSourceDatas[i]);
            asd.audioSource.playOnAwake = false;      //オブジェクト起動後再生させない
        }
    }

    void Update()
    {
        //再生が終わったSEがあるか走査
        for (int i = (int)Audio.SE_1; i < (int)Audio.NONE; i++)
        {
            AudioSourceData asd = audioSourceDatas[i];  //名前省略

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
            AudioSource audio = audioSourceDatas[(int)Audio.BGM].audioSource;  //名前省略

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
            AudioSource audio = audioSourceDatas[(int)Audio.BGM].audioSource;  //名前省略

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
                v= 0;
            }
            if (v > 1.0f)
            {
                v = 1.0f;
            }
            baseBGMVolue = v;

            AudioSourceData asd = audioSourceDatas[(int)Audio.BGM]; //名前省略
            asd.audioSource.volume = AddVolumeBGM(asd.gameVolume);
        }
    }

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

            for (int i = (int)Audio.SE_1; i < (int)Audio.NONE; i++)
            {
                AudioSourceData asd = audioSourceDatas[i]; //名前省略
                if (asd.isFree) continue;
                asd.audioSource.volume = AddVolumeSE(asd.gameVolume);
            }
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
        AudioSourceData asd = audioSourceDatas[(int)Audio.BGM]; //名前省略

        asd.gameVolume = volume;
        asd.audioSource.volume = AddVolumeBGM(volume);
    }


    //AudioClipを取得
    public static AudioClip GetAudioClip(SE se)
    {
        if (se == SE.NONE) return null;  //バグ防止
        return SEClips[(int)se];
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
        AudioSourceData asd = audioSourceDatas[(int)Audio.BGM]; //名前省略

        asd.gameVolume = volume;
        asd.isFree = false;
        asd.audioSource.volume = AddVolumeBGM(volume);
        asd.audioSource.clip = BGMClips[(int)bgm];
        asd.audioSource.Play();

        playingBGM = bgm;
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
        for (int i = (int)Audio.SE_1; i < (int)Audio.NONE; i++)
        {
            AudioSourceData data = audioSourceDatas[i];
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
        AudioSourceData asd = audioSourceDatas[index]; //名前省略

        asd.gameVolume = volume;
        asd.isFree = false;
        asd.audioSource.volume = AddVolumeSE(volume);
        asd.audioSource.clip = SEClips[(int)se];
        asd.audioSource.loop = loop;

        //再生時間がclipの長さ以下か調べる
        if (time > 0 && time < SEClips[(int)se].length)
        {
            asd.audioSource.time = time;
        }
        asd.audioSource.Play();

        return index;
    }

    //BGMを一時停止
    public static void PauseBGM()
    {
        audioSourceDatas[(int)Audio.BGM].audioSource.Pause();
    }

    //一時停止しているBGMを再開
    public static void UnPauseBGM()
    {
        audioSourceDatas[(int)Audio.BGM].audioSource.UnPause();
    }

    //BGMを停止
    public static void StopBGM()
    {
        AudioSourceData asd = audioSourceDatas[(int)Audio.BGM]; //名前省略

        InitAudioSourceData(ref asd);
        playingBGM = BGM.NONE;
        isFadeIn = false;
        isFadeOut = false;
        fadeVolume = 0;
    }

    //全てのSEを停止
    public static void StopSEAll()
    {
        //FreeSEManagerも整理
        for (int i = (int)Audio.SE_1; i < (int)Audio.NONE; i++)
        {
            AudioSourceData asd = audioSourceDatas[i];  //名前省略

            if (asd.isFree) continue;
            InitAudioSourceData(ref asd);
        }
    }

    //Playの時に返した値を引数に渡すと
    //Playで再生したSEを一時停止させる
    public static void PauseSE(int num)
    {
        //AudioSourceの配列内かチェック
        if (!AudioSourceArrayCheck(num)) return;

        AudioSourceData asd = audioSourceDatas[num];  //名前省略
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
        if (!AudioSourceArrayCheck(num)) return;

        AudioSourceData asd = audioSourceDatas[num];  //名前省略
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
        if (!AudioSourceArrayCheck(num)) return;

        AudioSourceData asd = audioSourceDatas[num];  //名前省略
        if (asd.audioSource.isPlaying)
        {
            InitAudioSourceData(ref asd);
        }
    }

    //ループ設定しているSEのループを停止させる
    //その場でSEが止まるわけではなくclipのSEが鳴り終わるまで再生される
    public static void StopLoop(int num)
    {
        //AudioSourceの配列内かチェック
        if (!AudioSourceArrayCheck(num)) return;

        AudioSource audio = audioSourceDatas[num].audioSource;  //名前省略
        if (audio.loop)
        {
            audio.loop = false;
        }
    }

    //SEが再生されているか
    public static bool IsPlayingSE(int num)
    {
        //AudioSourceの配列内かチェック
        if (!AudioSourceArrayCheck(num)) return false;

        if (audioSourceDatas[num].audioSource.isPlaying)
        {
            return true;
        }
        return false;
    }

    //再生されているBGMを返す
    public static BGM IsPlayingBGM
    {
        get { return playingBGM; }
    }

    //SEの長さを返す
    public static float Length(SE se)
    {
        return SEClips[(int)se].length;
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

        AudioSource audio = audioSourceDatas[(int)Audio.BGM].audioSource;
        audio.clip = BGMClips[(int)bgm];
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
        float diff = 1.0f - audioSourceDatas[(int)Audio.BGM].audioSource.volume; //今の音量と最大音量の差
        fadeVolume = (deltaTime / time) * diff;
    }

    //フェードアウト(徐々にBGMを小さくする)
    //timeは音量が0になるまでの時間
    public static void FadeOut(BGM bgm, float time)
    {
        //バグ防止
        if (bgm == BGM.NONE) return;

        AudioSource audio = audioSourceDatas[(int)Audio.BGM].audioSource;
        audio.clip = BGMClips[(int)bgm];
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
        float diff = audioSourceDatas[(int)Audio.BGM].audioSource.volume; //今の音量と最小音量の差
        fadeVolume = (deltaTime / time) * diff;
    }

    //フェードイン・フェードアウトを途中で止めて音量をそのままにする
    public static void StopFade()
    {
        isFadeIn = false;
        isFadeOut = false;
        fadeVolume = 0;
    }

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
    protected static bool AudioSourceArrayCheck(int num)
    {
        if (num < 0 || num > (int)Audio.NONE)
        {
            //配列の範囲外
            return false;
        }
        return true;
    }
}