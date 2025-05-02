using System.IO;
using UnityEngine;

namespace Common
{

    [RequireComponent(typeof(AudioSource))]
    [DefaultExecutionOrder(-99)]
    public class SoundManager : MonoBehaviour
    {
        /// <summary>
        /// BGM格納先フォルダ
        /// </summary>
        private const string BGMFolder = "BGM/";

        /// <summary>
        /// SE格納先フォルダ
        /// </summary>
        private const string SEFolder = "SE/";

        /// <summary>
        /// BGM音量の初期値
        /// </summary>
        private const float INIT_BGM_VOLUME = 0.5f;

        /// <summary>
        /// SE音量の初期値
        /// </summary>
        private const float INIT_SE_VOLUME = 0.5f;

        /// <summary>
        /// 同一SEの最大同時再生可能数
        /// </summary>
        private const int MAX_SAME_SE_PLAY = 2;

        /// <summary>
        /// BGM一覧
        /// </summary>
        public enum BGM
        {
            /// <summary>
            /// ホーム画面
            /// </summary>
            Home,

            /// <summary>
            /// ゲーム画面
            /// </summary>
            Loop,

            /// <summary>
            /// 用途不明
            /// </summary>
            THREE_MIN,

            None
        }

        /// <summary>
        /// SE一覧
        /// </summary>
        public enum SE
        {
            /// <summary>
            /// バリアダメージ
            /// </summary>
            BarrierDamage,

            /// <summary>
            /// ビーム
            /// </summary>
            Beam,

            /// <summary>
            /// ビーム1
            /// </summary>
            Beam1,

            /// <summary>
            /// ビーム2
            /// </summary>
            Beam2,

            /// <summary>
            /// チャージ
            /// </summary>
            BeamChange,

            /// <summary>
            /// ビーム開始
            /// </summary>
            BeamStart,

            /// <summary>
            /// ブースト
            /// </summary>
            Boost,

            /// <summary>
            /// 選択
            /// </summary>
            Select,

            /// <summary>
            /// キャンセル選択
            /// </summary>
            Cancel,

            /// <summary>
            /// ドローン破壊
            /// </summary>
            Death,

            /// <summary>
            /// バリア破壊
            /// </summary>
            DestroyBarrier,

            /// <summary>
            /// ミサイル爆破
            /// </summary>
            ExplosionMissile,

            /// <summary>
            /// ビル落下
            /// </summary>
            FallBuilding,

            /// <summary>
            /// ゲーム終了
            /// </summary>
            Finish,

            /// <summary>
            /// ガトリング
            /// </summary>
            Gatling,

            /// <summary>
            /// ジャミングノイズ
            /// </summary>
            JammingNoise,

            /// <summary>
            /// かまいたち
            /// </summary>
            Kamaitachi,

            /// <summary>
            /// 磁場エリア
            /// </summary>
            MagneticArea,

            /// <summary>
            /// ミサイル
            /// </summary>
            Missile,

            /// <summary>
            /// プロペラ
            /// </summary>
            Propeller,

            /// <summary>
            /// レーダー
            /// </summary>
            Radar,

            /// <summary>
            /// リスポーン
            /// </summary>
            Respawn,

            /// <summary>
            /// ショットガン
            /// </summary>
            Shotgun,

            /// <summary>
            /// 開始カウントダウン(ド)
            /// </summary>
            StartCountDownD,

            /// <summary>
            /// 開始カウントダウン(ミ)
            /// </summary>
            StartCountDownM,

            /// <summary>
            /// アイテム使用
            /// </summary>
            UseItem,

            /// <summary>
            /// 壁スタン
            /// </summary>
            WallStun,

            None
        }

        /// <summary>
        /// 再生中のBGM
        /// </summary>
        public static BGM PlayingBGM => _bgmAudioData.BGM;

        /// <summary>
        /// BGMのマスター音量（0～1）
        /// </summary>
        public static float MasterBGMVolume
        {
            get { return _masterBGMVolume; }
            set
            {
                _masterBGMVolume = value;
                if (value < 0)
                {
                    _masterBGMVolume = 0;
                }
                if (value > 1)
                {
                    _masterBGMVolume = 1;
                }
                _bgmAudioData.AudioSource.volume = GetTotalBGMVolume();
            }
        }
        private static float _masterBGMVolume = INIT_BGM_VOLUME;

        /// <summary>
        /// SEのマスター音量（0～1）
        /// </summary>
        public static float MasterSEVolume { get; set; } = INIT_SE_VOLUME;

        /// <summary>
        /// BGMの音量（0～1）
        /// </summary>
        public static float BGMVolume
        {
            get { return _bgmVolume; }
            set
            {
                _bgmVolume = value;
                if (value < 0)
                {
                    _bgmVolume = 0;
                }
                if (value > 1)
                {
                    _bgmVolume = 1;
                }
                _bgmAudioData.AudioSource.volume = GetTotalBGMVolume();
            }
        }
        private static float _bgmVolume = 1.0f;

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
            /// 再生BGM
            /// </summary>
            public BGM BGM { get; set; } = BGM.None;

            /// <summary>
            /// 再生SE
            /// </summary>
            public SE SE { get; set; } = SE.None;

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

        /// <summary>
        /// フェードイン/フェードアウト時の毎フレーム音量増減量
        /// </summary>
        private static float _fadeValue = 0;

        /// <summary>
        /// フェードイン中であるか
        /// </summary>
        private static bool _isFadeIn = false;

        /// <summary>
        /// フェードアウト中であるか
        /// </summary>
        private static bool _isFadeOut = false;

        /// <summary>
        /// SE同時再生用ロック
        /// </summary>
        private static object _lock = new object();

        /// <summary>
        /// オブジェクト生成済みであるか
        /// </summary>
        private static bool _isCreated = false;

        /// <summary>
        /// 指定したBGMのAudioClipを取得
        /// </summary>
        public static AudioClip GetAudioClip(BGM bgm)
        {
            if (bgm == BGM.None) return null;
            return _bgmClips[(int)bgm];
        }

        /// <summary>
        /// 指定したSEのAudioClipを取得
        /// </summary>
        public static AudioClip GetAudioClip(SE se)
        {
            if (se == SE.None) return null;
            return _seClips[(int)se];
        }

        /// <summary>
        /// マスター音量と掛け合わせた最終的なBGM音量を取得
        /// </summary>
        /// <returns></returns>
        public static float GetTotalBGMVolume()
        {
            return MasterBGMVolume * BGMVolume;
        }

        /// <summary>
        /// マスター音量と掛け合わせた最終的なSE音量を取得
        /// </summary>
        /// <returns></returns>
        public static float GetTotalSEVolume(float seVolume)
        {
            return MasterSEVolume * seVolume;
        }

        #region BGM

        /// <summary>
        /// 指定したBGMを再生
        /// </summary>
        /// <param name="bgm">再生するBGM</param>
        public static void Play(BGM bgm)
        {
            StopBGM();
            if (bgm == BGM.None) return;

            _bgmAudioData.AudioSource.clip = _bgmClips[(int)bgm];
            _bgmAudioData.AudioSource.loop = true;
            _bgmAudioData.AudioSource.Play();
            _bgmAudioData.BGM = bgm;
            _bgmAudioData.IsFree = false;
        }

        /// <summary>
        /// 指定したBGMを再生
        /// </summary>
        /// <param name="bgm">再生するBGM</param>
        /// <param name="volume">音量（0～1）</param>
        public static void Play(BGM bgm, float volume = 1)
        {
            if (bgm == BGM.None) return;
            BGMVolume = volume;
            Play(bgm);
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
            StopAudio(_bgmAudioData);
            StopFadeInOut();
        }

        /// <summary>
        /// フェードインを開始して徐々にBGMを大きくする
        /// </summary>
        /// <param name="fadeSec">最大の音量になるまでの時間（秒）</param>
        public static void FadeIn(float fadeSec)
        {
            if (_isFadeOut)
            {
                _isFadeOut = false;
            }
            _isFadeIn = true;
            _fadeValue = (Time.fixedDeltaTime / fadeSec) * (1 - BGMVolume);
        }

        /// <summary>
        /// フェードインを開始して徐々にBGMを大きくする
        /// </summary>
        /// <param name="bgm">再生BGM</param>
        /// <param name="fadeSec">最大の音量になるまでの時間（秒）</param>
        public static void FadeIn(BGM bgm, float fadeSec)
        {
            Play(bgm);

            if (bgm == BGM.None)
            {
                StopFadeInOut();
                return;
            }

            FadeIn(fadeSec);
        }

        /// <summary>
        /// フェードアウトを開始して徐々にBGMを小さくする
        /// </summary>
        /// <param name="fadeSec">ミュートになるまでの時間（秒）</param>
        public static void FadeOut(float fadeSec)
        {
            if (_isFadeIn)
            {
                _isFadeIn = false;
            }
            _isFadeOut = true;
            _fadeValue = (Time.fixedDeltaTime / fadeSec) * BGMVolume;
        }

        /// <summary>
        /// フェードアウトを開始して徐々にBGMを小さくする
        /// </summary>
        /// <param name="bgm">再生BGM</param>
        /// <param name="fadeSec">ミュートになるまでの時間（秒）</param>
        public static void FadeOut(BGM bgm, float fadeSec)
        {
            Play(bgm);

            if (bgm == BGM.None)
            {
                StopFadeInOut();
                return;
            }

            FadeOut(fadeSec);
        }

        /// <summary>
        /// フェードイン/フェードアウトを停止
        /// </summary>
        public static void StopFadeInOut()
        {
            _isFadeIn = false;
            _isFadeOut = false;
            _fadeValue = 0;
        }

        #endregion

        #region SE

        /// <summary>
        /// 指定したSEを再生する。最大10個まで同時に再生可能。
        /// </summary>
        /// <param name="se">再生するSE</param>
        /// <param name="volume">音量（0～1）</param>
        /// <param name="loop">ループ再生させるか</param>
        /// <param name="time">再生位置（秒）</param>
        /// <returns>SE管理番号。再生に失敗した場合は-1</returns>
        public static int Play(SE se, float volume = 1f, bool loop = false, float time = 0)
        {
            if (se == SE.None) return -1;

            int index = -1;
            lock (_lock)
            {
                // AudioSourceに空きがあるか調べる
                for (int i = 0; i < _seAudioDatas.Length; i++)
                {
                    AudioSourceData data = _seAudioDatas[i];
                    if (data.IsFree)
                    {
                        index = i;
                        break;
                    }
                }

                // 空きがなかったら処理しない
                if (index == -1) return -1;

                // 再生位置がclipの長さ以上の場合は再生失敗
                if (time > 0 && time >= _seClips[(int)se].length) return -1;

                // 同一SEが既に一定数以上再生されている場合は処理しない
                int sameSe = 0;
                foreach (var data in _seAudioDatas)
                {
                    if (data.SE == se)
                    {
                        sameSe++;
                        if (sameSe >= MAX_SAME_SE_PLAY) return -1;
                    }
                }

                _seAudioDatas[index].AudioSource.volume = GetTotalSEVolume(volume);
                _seAudioDatas[index].AudioSource.clip = _seClips[(int)se];
                _seAudioDatas[index].AudioSource.loop = loop;
                _seAudioDatas[index].SE = se;
                _seAudioDatas[index].IsFree = false;
            }


            // 再生位置が指定されている場合は適用
            if (time > 0)
            {
                _seAudioDatas[index].AudioSource.time = time;
            }

            // SE再生
            _seAudioDatas[index].AudioSource.Play();

            return index;
        }

        /// <summary>
        /// 指定した管理番号のSEを停止
        /// </summary>
        /// <param name="id">停止するSEの管理番号</param>
        public static void StopSE(int id)
        {
            // 有効なSE管理番号でない場合は処理しない
            if (!IsValidSEId(id)) return;

            AudioSourceData asd = _seAudioDatas[id];
            if (asd.AudioSource.isPlaying)
            {
                StopAudio(asd);
            }
        }

        /// <summary>
        /// 全てのSEを停止
        /// </summary>
        public static void StopSEAll()
        {
            for (int i = 0; i < _seAudioDatas.Length; i++)
            {
                if (_seAudioDatas[i].IsFree) continue;
                StopAudio(_seAudioDatas[i]);
            }
        }

        /// <summary>
        /// 指定した管理番号のSEを一時停止
        /// </summary>
        /// <param name="id">一時停止するSEの管理番号</param>
        public static void PauseSE(int id)
        {
            // 有効なSE管理番号でない場合は処理しない
            if (!IsValidSEId(id)) return;

            AudioSourceData asd = _seAudioDatas[id];
            if (asd.AudioSource.isPlaying)
            {
                asd.AudioSource.Pause();
                asd.IsPause = true;
            }
        }

        /// <summary>
        /// 一時停止したSEを再開
        /// </summary>
        /// <param name="id">再開するSEの管理番号</param>
        public static void UnPauseSE(int id)
        {
            // 有効なSE管理番号でない場合は処理しない
            if (!IsValidSEId(id)) return;

            AudioSourceData asd = _seAudioDatas[id];
            if (!asd.AudioSource.isPlaying)
            {
                asd.AudioSource.UnPause();
                asd.IsPause = false;
            }
        }

        /// <summary>
        /// 指定した管理番号のSEが一時停止であるか調べる
        /// </summary>
        /// <param name="id">チェックするSEの管理番号</param>
        public static bool IsPlayingSE(int id)
        {
            // 有効なSE管理番号でない場合は処理しない
            if (!IsValidSEId(id)) return false;

            if (_seAudioDatas[id].AudioSource.isPlaying)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// SEの再生時間を返す
        /// </summary>
        /// <param name="se">再生時間を取得するSE</param>
        /// <returns>再生時間（秒）</returns>
        public static float PlayTime(SE se)
        {
            return _seClips[(int)se].length;
        }

        #endregion

        private void Awake()
        {
            if (_isCreated)
            {
                Destroy(gameObject);
                return;
            }
            _isCreated = true;
            DontDestroyOnLoad(gameObject);

            #region SoundLoad

            // 各BGMのファイル名設定
            string[] BGMPath = new string[(int)BGM.None];
            BGMPath[(int)BGM.Home] = "Drone_up";
            BGMPath[(int)BGM.Loop] = "LoopBGM";
            BGMPath[(int)BGM.THREE_MIN] = "ThreeMinBGM";

            // 各SEのファイル名設定
            string[] SEPath = new string[(int)SE.None];
            SEPath[(int)SE.BarrierDamage] = "BarrierDamage";
            SEPath[(int)SE.Beam] = "Beam";
            SEPath[(int)SE.Beam1] = "Beam_1";
            SEPath[(int)SE.Beam2] = "Beam_2";
            SEPath[(int)SE.BeamChange] = "BeamCharge";
            SEPath[(int)SE.BeamStart] = "BeamStart";
            SEPath[(int)SE.Boost] = "Boost";
            SEPath[(int)SE.Cancel] = "Cancel";
            SEPath[(int)SE.Death] = "Death";
            SEPath[(int)SE.DestroyBarrier] = "DestroyBarrier";
            SEPath[(int)SE.ExplosionMissile] = "ExplosionMissile";
            SEPath[(int)SE.FallBuilding] = "FallBuilding";
            SEPath[(int)SE.Finish] = "Finish";
            SEPath[(int)SE.Gatling] = "Gatling";
            SEPath[(int)SE.JammingNoise] = "JammingNoise";
            SEPath[(int)SE.Kamaitachi] = "Kamaitachi";
            SEPath[(int)SE.MagneticArea] = "MagneticArea";
            SEPath[(int)SE.Missile] = "Missile";
            SEPath[(int)SE.Propeller] = "Propeller";
            SEPath[(int)SE.Radar] = "Radar";
            SEPath[(int)SE.Respawn] = "Respawn";
            SEPath[(int)SE.Select] = "Select";
            SEPath[(int)SE.Shotgun] = "Shotgun";
            SEPath[(int)SE.StartCountDownD] = "StartCountDown(D)";
            SEPath[(int)SE.StartCountDownM] = "StartCountDown(M)";
            SEPath[(int)SE.UseItem] = "UseItem";
            SEPath[(int)SE.WallStun] = "WallStun";

            // ResourcesフォルダからBGMをロード
            _bgmClips = new AudioClip[(int)BGM.None];
            for (int i = 0; i < (int)BGM.None; i++)
            {
                _bgmClips[i] = Resources.Load<AudioClip>(Path.Combine(BGMFolder, BGMPath[i]));
            }

            // ResourcesフォルダからSEをロード
            _seClips = new AudioClip[(int)SE.None];
            for (int i = 0; i < (int)SE.None; i++)
            {
                _seClips[i] = Resources.Load<AudioClip>(Path.Combine(SEFolder + SEPath[i]));
            }

            #endregion

            // 全てのAudioSourceコンポーネントを取得
            AudioSource[] audios = GetComponents<AudioSource>();

            // BGM用AudioSourceDataの初期化
            audios[0].loop = true;
            _bgmAudioData = new AudioSourceData
            {
                AudioSource = audios[0]
            };
            StopAudio(_bgmAudioData);
            _bgmAudioData.AudioSource.volume = GetTotalBGMVolume();

            // SE用AudioSourceDataの初期化
            _seAudioDatas = new AudioSourceData[audios.Length - 1];
            for (int i = 0; i < audios.Length - 1; i++)
            {
                _seAudioDatas[i] = new AudioSourceData
                {
                    AudioSource = audios[i + 1]
                };
                StopAudio(_seAudioDatas[i]);
            }
        }

        private void LateUpdate()
        {
            // 再生が終わったSEがあるかチェック
            for (int i = 0; i < _seAudioDatas.Length; i++)
            {
                AudioSourceData asd = _seAudioDatas[i];

                // 一時停止しているSEはスキップ
                if (asd.IsPause) continue;

                // 再生が終わったAudioSourceを初期化
                if (!asd.AudioSource.isPlaying && !asd.IsFree)
                {
                    StopAudio(asd);
                }
            }
        }

        private void FixedUpdate()
        {
            if (!_isFadeIn && !_isFadeOut) return;

            // フェードイン
            if (_isFadeIn)
            {
                BGMVolume += _fadeValue;
                if (BGMVolume == 1)
                {
                    _isFadeIn = false;
                    Debug.Log("a");
                }
            }

            // フェードアウト
            if (_isFadeOut)
            {
                BGMVolume -= _fadeValue;
                if (BGMVolume == 0)
                {
                    _isFadeOut = false;
                    Debug.Log("b");
                }
            }
        }

        /// <summary>
        /// オーディオ停止
        /// </summary>
        /// <param name="audio">停止するオーディオ</param>
        private static void StopAudio(AudioSourceData audio)
        {
            audio.AudioSource.Stop();
            audio.AudioSource.clip = null;
            audio.AudioSource.loop = false;
            audio.AudioSource.time = 0;
            audio.BGM = BGM.None;
            audio.SE = SE.None;
            audio.IsPause = false;
            audio.IsFree = true;
        }

        /// <summary>
        /// 有効なSE管理番号であるかチェックする
        /// </summary>
        /// <param name="id">チェックするSE管理番号</param>
        /// <returns>有効な場合はtrue</returns>
        private static bool IsValidSEId(int id)
        {
            if (id < 0 || id >= _seAudioDatas.Length)
            {
                return false;
            }
            return true;
        }
    }
}