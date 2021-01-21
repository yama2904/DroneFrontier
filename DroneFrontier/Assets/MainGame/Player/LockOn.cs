using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class LockOn : MonoBehaviour, ILockOn
{
    //プレイヤー系変数
    [SerializeField] BattlePlayer player = null;
    Transform playerTransform = null;

    //カメラ用変数
    [SerializeField] Camera _camera = null;
    Transform cameraTransform = null;

    //ターゲット用変数
    public GameObject Target { get; private set; } = null;   //ロックオンしているオブジェクト
    Transform targetTransform = null;
    bool isTarget = false;   //ロックオンしているか

    //ロックオン処理用変数
    [SerializeField] Image lockOnImage = null;    //ロックオンした際に表示する画像
    List<GameObject> notLockOnObjects = new List<GameObject>();
    [SerializeField, Tooltip("ロックオン距離")] float searchRadius = 100.0f; //ロックオンする範囲
    public float TrackingSpeed { get; set; } = 0;     //ロックオンした際に敵にカメラを向ける速度


    void Awake()
    {
        playerTransform = player.transform;
        cameraTransform = _camera.transform;

        //自分をロックオンしない対象に入れる
        notLockOnObjects.Add(player.gameObject);
    }

    void Update()
    {
        //ロックオン中なら追従処理
        if (isTarget)
        {
            //ロックオンの対象オブジェクトが消えていないなら継続して追尾
            if (Target != null)
            {
                Vector3 diff = targetTransform.position - cameraTransform.position;   //ターゲットとの距離
                Quaternion rotation = Quaternion.LookRotation(diff);      //ロックオンしたオブジェクトの方向

                //カメラの角度からtrackingSpeed(0～1)の速度でロックオンしたオブジェクトの角度に向く
                playerTransform.rotation = Quaternion.Slerp(playerTransform.rotation, rotation, TrackingSpeed);
            }
            //ロックオンしている最中に対象が消えたらロックオン解除
            else
            {
                isTarget = false;
                lockOnImage.enabled = false;
            }
        }
    }

    //リストから必要な要素だけ抜き取る
    List<GameObject> FilterTargetObject(List<GameObject> hits)
    {
        return hits.Where(h =>
        {
            //各要素の座標をビューポートに変換(画面左下が0:0、右上が1:1)して条件に合うものだけリストに詰め込む
            Vector3 screenPoint = _camera.WorldToViewportPoint(h.transform.position);
            return screenPoint.x > 0.25f && screenPoint.x < 0.75f && screenPoint.y > 0.15f && screenPoint.y < 0.85f && screenPoint.z > 0;
        }).Where(h => h.CompareTag(TagNameManager.PLAYER) || h.CompareTag(TagNameManager.CPU) ||   //ロックオン対象を選択
           h.CompareTag(TagNameManager.JAMMING_BOT))
           .Where(h =>   //notLockOnObjects内のオブジェクトがある場合は除外
           {
               if (notLockOnObjects.FindIndex(o => ReferenceEquals(o, h.gameObject)) == -1)
               {
                   return true;
               }
               return false;
           })
          .ToList();
    }


    //ロックオンする
    public void StartLockOn(float speed)
    {
        TrackingSpeed = speed;

        //何もロックオンしていない場合はロックオン対象を探す
        if (!isTarget)
        {
            //取得したRaycastHit配列から各RaycastHitクラスのgameObjectを抜き取ってリスト化する
            var hits = Physics.SphereCastAll(
                cameraTransform.position,
                searchRadius,
                cameraTransform.forward,
                0.01f).Select(h => h.transform.gameObject).ToList();

            hits = FilterTargetObject(hits);
            if (hits.Count > 0)
            {
                float minTargetDistance = float.MaxValue;   //初期化
                GameObject t = null;    //target

                foreach (var hit in hits)
                {
                    //ビューポートに変換
                    Vector3 targetScreenPoint = _camera.WorldToViewportPoint(hit.transform.position);

                    //画面の中央との距離を計算
                    float targetDistance = (new Vector2(0.5f, 0.5f) - new Vector2(targetScreenPoint.x, targetScreenPoint.y)).sqrMagnitude;

                    //距離が最小だったら更新
                    if (targetDistance < minTargetDistance)
                    {
                        minTargetDistance = targetDistance;
                        t = hit.gameObject;
                    }
                }
                Target = t;
                targetTransform = t.transform;
                lockOnImage.enabled = true;
                isTarget = true;
            }
        }
    }

    public void ReleaseLockOn()
    {
        if (isTarget)
        {
            Target = null;
            targetTransform = null;
            isTarget = false;
            lockOnImage.enabled = false;
        }
    }

    //ロックオンしないオブジェクトを設定
    public void SetNotLockOnObject(GameObject o)
    {
        //既にオブジェクトが含まれている場合はスルー
        if (notLockOnObjects.FindIndex(listObject => ReferenceEquals(listObject, o)) == -1)
        {
            notLockOnObjects.Add(o);
        }
    }

    //SetNotLockOnObjectで設定したオブジェクトをロックオンするように設定
    public void UnSetNotLockOnObject(GameObject o)
    {
        int index = notLockOnObjects.FindIndex(listObject => ReferenceEquals(listObject, o));
        if (index >= 0)
        {
            notLockOnObjects.RemoveAt(index);
        }
    }
}