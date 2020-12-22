using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class LockOn : MonoBehaviour
{
    [SerializeField] GameObject player = null;
    const string IMAGE_NAME = "LockOnImage";
    static Image lockOnImage = null;    //ロックオンした際に表示する画像
    static float searchRadius = 100.0f; //ロックオンする範囲
    public static float TrackingSpeed { get; set; } = 0.1f;     //ロックオンした際に敵にカメラを向ける速度

    public static GameObject Target { get; private set; } = null;   //ロックオンしているオブジェクト
    static bool isTarget = false;   //ロックオンしているか

    static bool useLockOn = true;   //ロックオンを使うか

    void Start()
    {
        TrackingSpeed = 0.1f;
        lockOnImage = transform.Find(IMAGE_NAME).GetComponent<Image>();  //画像のロード
        lockOnImage.enabled = false;    //ロックオンしていない際は非表示
        Target = null;
        isTarget = false;
    }

    void Update()
    {
        //ロックオン中なら追従処理
        if (isTarget)
        {
            //ロックオンの対象オブジェクトが消えていないなら継続して追尾
            if (Target != null)
            {
                GameObject camera = Camera.main.gameObject;   //名前省略
                Vector3 diff = Target.transform.position - camera.transform.position;   //ターゲットとの距離
                Quaternion rotation = Quaternion.LookRotation(diff);      //ロックオンしたオブジェクトの方向

                //カメラの角度からtrackingSpeed(0～1)の速度でロックオンしたオブジェクトの角度に向く
                player.transform.rotation = Quaternion.Slerp(player.transform.rotation, rotation, TrackingSpeed);
            }
            //ロックオンしている最中に対象が消えたらロックオン解除
            else
            {
                isTarget = false;
                lockOnImage.enabled = false;
            }
        }
    }

    //ロックオンする
    public static void StartLockOn()
    {
        //ロックオンを禁止していたら処理をしない
        if (!useLockOn)
        {
            return;
        }

        //何もロックオンしていない場合はロックオン対象を探す
        if (!isTarget)
        {
            //名前省略
            GameObject camera = Camera.main.gameObject;

            //取得したRaycastHit配列から各RaycastHitクラスのgameObjectを抜き取ってリスト化する
            var hits = Physics.SphereCastAll(
                camera.transform.position,
                searchRadius,
                camera.transform.forward,
                0.01f).Select(h => h.transform.gameObject).ToList();

            hits = FilterTargetObject(hits);
            if (hits.Count > 0)
            {
                float minTargetDistance = float.MaxValue;   //初期化
                GameObject t = null;    //target

                foreach (var hit in hits)
                {
                    //ビューポートに変換
                    Vector3 targetScreenPoint = Camera.main.WorldToViewportPoint(hit.transform.position);

                    //画面の中央との距離を計算
                    float targetDistance = Vector2.Distance(
                           new Vector2(0.5f, 0.5f),
                           new Vector2(targetScreenPoint.x, targetScreenPoint.y));

                    //距離が最小だったら更新
                    if (targetDistance < minTargetDistance)
                    {
                        minTargetDistance = targetDistance;
                        t = hit.transform.gameObject;
                    }
                }

                Target = t;
                lockOnImage.enabled = true;
                isTarget = true;
            }
        }
    }

    public static void ReleaseLockOn()
    {
        if (isTarget)
        {
            Target = null;
            isTarget = false;
            lockOnImage.enabled = false;
        }
    }


    //ロックオンを使用するならtrue
    //禁止するならfalse
    public static void UseLockOn(bool use)
    {
        if (!use)
        {
            ReleaseLockOn();
        }
        useLockOn = use;
    }

    //リストから必要な要素だけ抜き取る
    static List<GameObject> FilterTargetObject(List<GameObject> hits)
    {
        return hits.Where(h =>
        {
            //各要素の座標をビューポートに変換(画面左下が0:0、右上が1:1)して条件に合うものだけリストに詰め込む
            Vector3 screenPoint = Camera.main.WorldToViewportPoint(h.transform.position);
            return screenPoint.x > 0.25f && screenPoint.x < 0.75f && screenPoint.y > 0.15f && screenPoint.y < 0.85f && screenPoint.z > 0;
        }).Where(h => h.tag == Player.PLAYER_TAG || h.tag == CPUController.CPU_TAG)       //プレイヤーとCPUが対象
          .Where(h => h.name != Player.ObjectName)      //操作しているプレイヤーは除外
          .ToList();
    }
}