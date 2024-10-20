using System.IO;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[DefaultExecutionOrder(-99)]
public class SoundManager : MonoBehaviour
{
    private const string BGMFolder = "BGM/";
    private const string SEFolder = "SE/";

    /// <summary>
    /// BGM一覧
    /// </summary>
    public enum BGM
    {
        DRONE_UP,
        LOOP,
        THREE_MIN,

        NONE
    }

    /// <summary>
    /// SE一覧
    /// </summary>
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

    /// <summary>
    /// 再生中のBGM
    /// </summary>
    public static BGM PlayingBGM { get; private set; } = BGM.NONE;

    /// <summary>
    /// BGMの音量（0～1）
    /// </summary>
    public static float BGMVolume { get; set; } = 1.0f;

    /// <summary>
    /// SEの音量（0～1）
    /// </summary>
    public static float SEVolume { get; set; } = 1.0f;

    /// <summary>
    /// BGMオーディオデータ
    /// </summary>
    private static AudioClip[] _bgmClips;

    /// <summary>
    /// SEオーディオデータ
    /// </summary>
    private static AudioClip[] _seClips;

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
        /// 一時停止しているか
        /// </summary>
        public bool IsPause { get; set; } = false;

        /// <summary>
        /// 未使用であるか
        /// </summary>
        public bool IsFree { get; set; } = true;
    }

    /// <summary>
    /// BGM用AudioSourceData
    /// </summary>
    private static AudioSourceData _bgmAudioData = null;

    /// <summary>
    /// SE用AudioSourceData
    /// </summary>
    private static AudioSourceData[] _seAudioDatas = null;

    private static float fadeVolume = 0;    //フェードイン又はフェードアウトの1フレームの音量変化量
    private static float deltaTime = 0;     //static関数で使えるようにTime.deltaTimeを代入する変数
    private static bool isFadeIn = false;   //フェードインするか
    private static bool isFadeOut = false;  //フェードアウトするか

    /// <summary>
    /// 指定したBGMのAudioClipを取得
    /// </summary>
    public static AudioClip GetAudioClip(BGM bgm)
    {
        if (bgm == BGM.NONE) return null;
        return _bgmClips[(int)bgm];
    }

    /// <summary>
    /// 指定したSEのAudioClipを取得
    /// </summary>
    public static AudioClip GetAudioClip(SE se)
    {
        if (se == SE.NONE) return null;
        return _seClips[(int)se];
    }

    #region BGM

    /// <summary>
    /// BGMの補正音量を変更
    /// </summary>
    /// <param name="volume">音量（0～1）</param>
    public static void ChangeGameBGMVolume(float volume)
    {
        if (volume < 0)
        {
            volume = 0;
        }
        if (volume > 1.0f)
        {
            volume = 1.0f;
        }
        _bgmAudioData.AudioSource.volume = AddVolumeBGM(volume);
    }

    /// <summary>
    /// 指定したBGMを再生<br/>
    /// 音量を調整する場合は補正音量を指定
    /// </summary>
    /// <param name="bgm">再生するBGM</param>
    /// <param name="volume">補正音量（0～1）</param>
    public static void Play(BGM bgm, float volume = 1.0f)
    {
        if (bgm == BGM.NONE) return;

        if (volume < 0)
        {
            volume = 0;
        }
        if (volume > 1.0f)
        {
            volume = 1.0f;
        }

        _bgmAudioData.AudioSource.Stop();
        _bgmAudioData.IsFree = false;
        _bgmAudioData.AudioSource.volume = AddVolumeBGM(volume);
        _bgmAudioData.AudioSource.clip = _bgmClips[(int)bgm];
        _bgmAudioData.AudioSource.loop = true;
        _bgmAudioData.AudioSource.Play();

        PlayingBGM = bgm;
    }

    /// <summary>
    /// BGMを一時停止
    /// </summary>
    public static void PauseBGM()
    {
        _bgmAudioData.AudioSource.Pause();
    }

    /// <summary>
    /// 一時停止しているBGMを再開
    /// </summary>
    public static void UnPauseBGM()
    {
        _bgmAudioData.AudioSource.UnPause();
    }

    /// <summary>
    /// BGMを停止
    /// </summary>
    public static void StopBGM()
    {
        InitAudioSourceData(ref _bgmAudioData);
        PlayingBGM = BGM.NONE;
        isFadeIn = false;
        isFadeOut = false;
        fadeVolume = 0;
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
        if (bgm == BGM.NONE) return;

        AudioSource audio = _bgmAudioData.AudioSource;  //名前省略
        audio.clip = _bgmClips[(int)bgm];
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
    public static void FadeIn(float time)
    {
        if (isFadeOut)
        {
            isFadeOut = false;
        }
        isFadeIn = true;
        float diff = 1.0f - _bgmAudioData.AudioSource.volume; //今の音量と最大音量の差
        fadeVolume = (deltaTime / time) * diff;
    }

    //フェードアウト(徐々にBGMを小さくする)
    //timeは音量が0になるまでの時間
    public static void FadeOut(BGM bgm, float time)
    {
        //バグ防止
        if (bgm == BGM.NONE) return;

        AudioSource audio = _bgmAudioData.AudioSource;
        audio.clip = _bgmClips[(int)bgm];
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
        float diff = _bgmAudioData.AudioSource.volume; //今の音量と最小音量の差
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

    /// <summary>
    /// 指定したSEを再生する。最大10個まで同時に再生可能。
    /// </summary>
    /// <param name="se">再生するSE</param>
    /// <param name="volume">補正音量（0～1）</param>
    /// <param name="loop">ループ再生させるか</param>
    /// <param name="time">再生位置（秒）</param>
    /// <returns>SE管理番号。再生に失敗した場合は-1</returns>
    public static int Play(SE se, float volume = 1f, bool loop = false, float time = 0)
    {
        if (se == SE.NONE) return -1;

        // AudioSourceに空きがあるか調べる
        int index = -1;
        for (int i = 0; i < _seAudioDatas.Length; i++)
        {
            AudioSourceData data = _seAudioDatas[i];
            if (data.IsFree)
            {
                index = i;
                break;
            }
        }

        //空きがなかったら処理しない
        if (index == -1) return -1;

        // 再生位置がclipの長さ以上の場合は再生失敗
        if (time > 0 && time >= _seClips[(int)se].length) return -1;

        if (volume < 0)
        {
            volume = 0;
        }
        if (volume > 1.0f)
        {
            volume = 1.0f;
        }
        _seAudioDatas[index].IsFree = false;
        _seAudioDatas[index].AudioSource.volume = AddVolumeSE(volume);
        _seAudioDatas[index].AudioSource.clip = _seClips[(int)se];
        _seAudioDatas[index].AudioSource.loop = loop;

        // 再生位置が指定されている場合は適用
        if (time > 0)
        {
            _seAudioDatas[index].AudioSource.time = time;
        }

        // SE再生
        _seAudioDatas[index].AudioSource.Play();

        return index;
    }

    //全てのSEを停止
    public static void StopSEAll()
    {
        //FreeSEManagerも整理
        for (int i = 0; i < _seAudioDatas.Length; i++)
        {
            AudioSourceData asd = _seAudioDatas[i];  //名前省略

            if (asd.IsFree) continue;
            InitAudioSourceData(ref asd);
        }
    }

    //Playの時に返した値を引数に渡すと
    //Playで再生したSEを一時停止させる
    public static void PauseSE(int num)
    {
        //AudioSourceの配列内かチェック
        if (!AudioSourceArrayCheck(_seAudioDatas, num)) return;

        AudioSourceData asd = _seAudioDatas[num];  //名前省略
        if (asd.AudioSource.isPlaying)
        {
            asd.AudioSource.Pause();
            asd.IsPause = true;
        }
    }

    //一時停止したSEを再開する
    public static void UnPauseSE(int num)
    {
        //AudioSourceの配列内かチェック
        if (!AudioSourceArrayCheck(_seAudioDatas, num)) return;

        AudioSourceData asd = _seAudioDatas[num];  //名前省略
        if (!asd.AudioSource.isPlaying)
        {
            asd.AudioSource.UnPause();
            asd.IsPause = false;
        }
    }
    //Playの時に返した値を引数に渡すと
    //Playで再生したSEを停止させる
    public static void StopSE(int num)
    {
        //AudioSourceの配列内かチェック
        if (!AudioSourceArrayCheck(_seAudioDatas, num)) return;

        AudioSourceData asd = _seAudioDatas[num];  //名前省略
        if (asd.AudioSource.isPlaying)
        {
            InitAudioSourceData(ref asd);
        }
    }

    //ループ設定しているSEのループを停止させる
    //その場でSEが止まるわけではなくclipのSEが鳴り終わるまで再生される
    public static void StopLoopSE(int num)
    {
        //AudioSourceの配列内かチェック
        if (!AudioSourceArrayCheck(_seAudioDatas, num)) return;

        AudioSource audio = _seAudioDatas[num].AudioSource;  //名前省略
        if (audio.loop)
        {
            audio.loop = false;
        }
    }

    //SEが再生されているか
    public static bool IsPlayingSE(int num)
    {
        //AudioSourceの配列内かチェック
        if (!AudioSourceArrayCheck(_seAudioDatas, num)) return false;

        if (_seAudioDatas[num].AudioSource.isPlaying)
        {
            return true;
        }
        return false;
    }

    //SEの長さを返す
    public static float Length(SE se)
    {
        return _seClips[(int)se].length;
    }

    #endregion

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        #region SoundLoad

        // 各BGMのファイル名設定
        string[] BGMPath = new string[(int)BGM.NONE];
        BGMPath[(int)BGM.DRONE_UP] = "Drone_up";
        BGMPath[(int)BGM.LOOP] = "LoopBGM";
        BGMPath[(int)BGM.THREE_MIN] = "ThreeMinBGM";

        // 各SEのファイル名設定
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

        // ResourcesフォルダからBGMをロード
        _bgmClips = new AudioClip[(int)BGM.NONE];
        for (int i = 0; i < (int)BGM.NONE; i++)
        {
            _bgmClips[i] = Resources.Load<AudioClip>(Path.Combine(BGMFolder, BGMPath[i]));
        }

        // ResourcesフォルダからSEをロード
        _seClips = new AudioClip[(int)SE.NONE];
        for (int i = 0; i < (int)SE.NONE; i++)
        {
            _seClips[i] = Resources.Load<AudioClip>(Path.Combine(SEFolder + SEPath[i]));
        }

        #endregion

        // 各AudioSourceDataの初期化

        // 全てのAudioSourceコンポーネントを取得
        AudioSource[] audios = GetComponents<AudioSource>();

        // BGM用AudioSourceDataの初期化
        audios[0].loop = true;
        _bgmAudioData = new AudioSourceData
        {
            AudioSource = audios[0]
        };
        InitAudioSourceData(ref _bgmAudioData);

        // SE用AudioSourceDataの初期化
        _seAudioDatas = new AudioSourceData[audios.Length - 1];
        for (int i = 0; i < audios.Length - 1; i++)
        {
            _seAudioDatas[i] = new AudioSourceData
            {
                AudioSource = audios[i + 1]
            };
            InitAudioSourceData(ref _seAudioDatas[i]);
        }
    }

    private void Update()
    {
        // 再生が終わったSEがあるか走査
        for (int i = 0; i < _seAudioDatas.Length; i++)
        {
            AudioSourceData asd = _seAudioDatas[i];

            // 一時停止しているSEがあったら走査をスキップ
            if (asd.IsPause) continue;

            // 再生が終わったAudioSourceを初期化
            if (!asd.AudioSource.isPlaying)
            {
                if (!asd.IsFree)
                {
                    InitAudioSourceData(ref asd);
                }
            }
        }

        //フェードイン処理
        if (isFadeIn)
        {
            AudioSource audio = _bgmAudioData.AudioSource;

            //フェード処理
            if (audio.isPlaying)
            {
                audio.volume += AddVolumeBGM(fadeVolume);

                if (audio.volume >= BGMVolume)
                {
                    audio.volume = BGMVolume;
                    isFadeIn = false;
                    fadeVolume = 0;
                }
            }
        }

        //フェードアウト処理
        if (isFadeOut)
        {
            AudioSource audio = _bgmAudioData.AudioSource;

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
        return volume * BGMVolume;
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
        return volume * SEVolume;
    }

    //AudioSourceの初期化
    static void InitAudioSourceData(ref AudioSourceData audioData)
    {
        audioData.AudioSource.Stop();
        audioData.AudioSource.clip = null;    //初期化
        audioData.AudioSource.volume = 0;
        audioData.AudioSource.loop = false;   //ループを解除
        audioData.AudioSource.time = 0;       //途中再生を解除

        audioData.IsPause = false;
        audioData.IsFree = true;
    }

    //AudioSourceの配列内かチェック
    private static bool AudioSourceArrayCheck(AudioSourceData[] array, int num)
    {
        if (num < 0 || num >= array.Length)
        {
            //配列の範囲外
            return false;
        }
        return true;
    }
}