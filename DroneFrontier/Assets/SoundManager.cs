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

    //SetBaseVolumeで調整したゲーム全体の音量
    static float settingBaseVolueBGM = 1.0f;   //初期値は最大
    static float settingBaseVolumeSE = 1.0f;

    protected static AudioSource[] audioSources;
    static float fadeVolume = 0;    //フェードイン又はフェードアウトの1フレームの音量変化量
    static float deltaTime = 0;     //static関数で使えるようにTime.deltaTimeを代入する変数
    static bool isFadeIn = false;   //フェードインするか
    static bool isFadeOut = false;  //フェードアウトするか

    //空いているSEの要素を管理する
    protected static List<int> freeSEManager = new List<int>();

    //一時停止しているSEの要素を管理する
    protected static List<int> pauseSEManager = new List<int>();


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

        BGMClips = new AudioClip[(int)BGM.NONE];
        SEClips = new AudioClip[(int)SE.NONE];
        //ResourcesファイルからBGMをロード
        for (int i = 0; i < (int)BGM.NONE; i++)
        {
            BGMClips[i] = Resources.Load<AudioClip>(BGMFolder + BGMPath[i]);
        }
        //ResourcesファイルからSEをロード
        for (int i = 0; i < (int)SE.NONE; i++)
        {
            SEClips[i] = Resources.Load<AudioClip>(SEFolder + SEPath[i]);
        }
        audioSources = new AudioSource[(int)Audio.NONE];
        audioSources = GetComponents<AudioSource>();   //AudioSourceコンポーネントが複数いるので

        //BGMの初期化
        audioSources[(int)Audio.BGM].clip = null;
        audioSources[(int)Audio.BGM].volume = settingBaseVolueBGM;
        audioSources[(int)Audio.BGM].loop = true;             //BGMはデフォルトでループさせる
        audioSources[(int)Audio.BGM].playOnAwake = false;     //オブジェクト起動後再生させない

        //SEの初期化
        for (int i = (int)Audio.SE_1; i < (int)Audio.NONE; i++)
        {
            InitAudio(ref audioSources[i]);
            audioSources[i].volume = settingBaseVolumeSE;
            audioSources[i].playOnAwake = false;      //オブジェクト起動後再生させない

            freeSEManager.Add(i);
        }
    }

    void Update()
    {
        //再生が終わったSEがあるか走査
        for (int i = (int)Audio.SE_1; i < (int)Audio.NONE; i++)
        {
            //一時停止しているSEがあったら走査をスキップ
            for (int j = 0; j < pauseSEManager.Count; j++)
            {
                if (pauseSEManager[j] == i)
                {
                    continue;
                }
            }
            //再生が終わっていたらFreeSEManagerに追加して
            //再生が終わったAudioSourceを初期化
            if (!audioSources[i].isPlaying)
            {
                if (!freeSEManager.Contains(i))
                {
                    InitAudio(ref audioSources[i]);
                    freeSEManager.Add(i);
                }
            }
        }

        //フェード処理
        if (audioSources[(int)Audio.BGM].isPlaying)
        {
            //フェードイン処理
            if (isFadeIn)
            {
                audioSources[(int)Audio.BGM].volume += AddVolumeBGM(fadeVolume);

                if (audioSources[(int)Audio.BGM].volume >= settingBaseVolueBGM)
                {
                    audioSources[(int)Audio.BGM].volume = settingBaseVolueBGM;
                    isFadeIn = false;
                    fadeVolume = 0;
                }
            }

            //フェードアウト処理
            if (isFadeOut)
            {
                audioSources[(int)Audio.BGM].volume -= AddVolumeBGM(fadeVolume);

                if (audioSources[(int)Audio.BGM].volume <= 0)
                {
                    audioSources[(int)Audio.BGM].volume = 0;
                    isFadeOut = false;
                    fadeVolume = 0;
                }
            }
        }
        deltaTime = Time.deltaTime;
    }

    //ゲーム全体のBGMの音量を0～1で設定
    //設定画面での音量の設定など
    public static float SettingVolumeBGM
    {
        get { return settingBaseVolueBGM; }
        set
        {
            if (value < 0)
            {
                settingBaseVolueBGM = 0;
            }
            if (value > 1.0f)
            {
                settingBaseVolueBGM = 1.0f;
            }
        }
    }

    //ゲーム全体のSEの音量を0～1で設定
    //設定画面での音量の設定など
    public static float SettingVolumeSE
    {
        get { return settingBaseVolumeSE; }
        set
        {
            if (value < 0)
            {
                settingBaseVolumeSE = 0;
            }
            if (value > 1.0f)
            {
                settingBaseVolumeSE = 1.0f;
            }
        }
    }

    //BGMの音量を0～1で変更
    public static void SetBGMValume(float volume)
    {
        if (volume < 0)
        {
            volume = 0;
        }
        if (volume > 1.0f)
        {
            volume = 1.0f;
        }
        audioSources[(int)Audio.BGM].volume = volume * settingBaseVolueBGM;
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
        audioSources[(int)Audio.BGM].volume = volume;

        audioSources[(int)Audio.BGM].clip = BGMClips[(int)bgm];
        audioSources[(int)Audio.BGM].Play();
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

        if (freeSEManager.Count > 0)
        {
            if (volume < 0)
            {
                volume = 0;
            }
            if (volume > 1.0f)
            {
                volume = 1.0f;
            }
            audioSources[freeSEManager[0]].volume = volume * settingBaseVolumeSE;

            audioSources[freeSEManager[0]].clip = SEClips[(int)se];
            audioSources[freeSEManager[0]].loop = loop;
            if (time > 0 && time < SEClips[(int)se].length)
            {
                audioSources[freeSEManager[0]].time = time;
            }
            audioSources[freeSEManager[0]].Play();

            int returnNum = freeSEManager[0];
            freeSEManager.RemoveAt(0);

            return returnNum;
        }

        //エラー
        //これ以上SEを再生できません
        return -1;
    }

    //BGMを一時停止
    public static void PauseBGM()
    {
        audioSources[(int)Audio.BGM].Pause();
    }
    //一時停止しているBGMを再開
    public static void UnPauseBGM()
    {
        audioSources[(int)Audio.BGM].UnPause();
    }
    //BGMを停止
    public static void StopBGM()
    {
        audioSources[(int)Audio.BGM].Stop();
        InitAudio(ref audioSources[(int)Audio.BGM]);
        playingBGM = BGM.NONE;
        isFadeIn = false;
        isFadeOut = false;
        fadeVolume = 0;
    }

    //全てのSEを停止
    public static void StopSEAll()
    {
        //FreeSEManagerも整理
        freeSEManager.Clear();
        for (int i = (int)Audio.SE_1; i < (int)Audio.NONE; i++)
        {
            audioSources[i].Stop();
            InitAudio(ref audioSources[i]);
            freeSEManager.Add(i);
        }
    }

    //Playの時に返した値を引数に渡すと
    //Playで再生したSEを一時停止させる
    public static void PauseSE(int num)
    {
        //AudioSourceの配列内かチェック
        if (!AudioSourceArrayCheck(num)) return;

        if (audioSources[num].isPlaying)
        {
            audioSources[num].Pause();
            pauseSEManager.Add(num);
        }
    }

    //一時停止したSEを再開する
    public static void UnPauseSE(int num)
    {
        //AudioSourceの配列内かチェック
        if (!AudioSourceArrayCheck(num)) return;

        if (!audioSources[num].isPlaying)
        {
            for (int i = 0; i < pauseSEManager.Count; i++)
            {
                if (pauseSEManager[i] == num)
                {
                    audioSources[num].UnPause();
                    pauseSEManager.RemoveAt(i);
                }
            }
        }
    }
    //Playの時に返した値を引数に渡すと
    //Playで再生したSEを停止させる
    public static void StopSE(int num)
    {
        //AudioSourceの配列内かチェック
        if (!AudioSourceArrayCheck(num)) return;

        if (audioSources[num].isPlaying)
        {
            audioSources[num].Stop();
            InitAudio(ref audioSources[num]);
            freeSEManager.Add(num);
        }
    }

    //ループ設定しているSEのループを停止させる
    //その場でSEが止まるわけではなくclipのSEが鳴り終わるまで再生される
    public static void StopLoop(int num)
    {
        //AudioSourceの配列内かチェック
        if (!AudioSourceArrayCheck(num)) return;

        if (audioSources[num].loop)
        {
            audioSources[num].loop = false;
        }
    }

    //SEが再生されているか
    public static bool IsPlayingSE(int num)
    {
        //AudioSourceの配列内かチェック
        if (!AudioSourceArrayCheck(num)) return false;

        if (audioSources[num].isPlaying)
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

        AudioSource audio = audioSources[(int)Audio.BGM];
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
        float diff = 1.0f - audioSources[(int)Audio.BGM].volume; //今の音量と最大音量の差
        fadeVolume = (deltaTime / time) * diff;
    }

    //フェードアウト(徐々にBGMを小さくする)
    //timeは音量が0になるまでの時間
    public static void FadeOut(BGM bgm, float time)
    {
        //バグ防止
        if (bgm == BGM.NONE) return;

        AudioSource audio = audioSources[(int)Audio.BGM];
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
        float diff = audioSources[(int)Audio.BGM].volume; //今の音量と最小音量の差
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
        return volume * settingBaseVolueBGM;
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
        return volume * settingBaseVolumeSE;
    }

    //AudioSourceの初期化
    static void InitAudio(ref AudioSource audio)
    {
        audio.clip = null;    //初期化
        audio.loop = false;     //ループを解除
        audio.time = 0;         //途中再生を解除
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