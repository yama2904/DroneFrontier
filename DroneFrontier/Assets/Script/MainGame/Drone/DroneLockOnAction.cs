using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class DroneLockOnAction : MonoBehaviour
{
    //カメラ用変数
    [SerializeField, Tooltip("ドローンのカメラ")]
    private Camera _camera = null;

    [SerializeField, Tooltip("レティクル画像")]
    private Image _reticleImage = null;

    //ロックオン処理用変数
    [SerializeField, Tooltip("非ロックオン中のレティクルの色")]
    Color _noLockOnColor = new Color(255, 255, 255, 128);

    [SerializeField, Tooltip("ロックオン中のレティクルの色")]
    Color _lockingOnColor = new Color(255, 0, 0, 200);

    Transform _transform = null;
    Transform cameraTransform = null;

    //ターゲット用変数
    public GameObject Target { get; private set; } = null;   //ロックオンしているオブジェクト
    Transform targetTransform = null;
    bool isTarget = false;   //ロックオンしているか

    List<GameObject> notLockOnObjects = new List<GameObject>();
    public float TrackingSpeed { get; set; } = 0;     //ロックオンした際に敵にカメラを向ける速度
    [SerializeField] float searchRadius = 450f; //ロックオンする範囲
    [SerializeField, Tooltip("ロックオン距離")] float maxDistance = 450f;


    void Awake()
    {
        _transform = transform;
        cameraTransform = _camera.transform;
    }

    public void Init()
    {
        // 非ロックオン状態でレティクル初期化
        _reticleImage.color = _noLockOnColor;

        //自分をロックオンしない対象に入れる
        notLockOnObjects.Add(gameObject);
    }

    //リストから必要な要素だけ抜き取る
    List<GameObject> FilterTargetObject(List<GameObject> hits)
    {
        return hits.Where(h =>
        {
            //各要素の座標をビューポートに変換(画面左下が0:0、右上が1:1)して条件に合うものだけリストに詰め込む
            Vector3 screenPoint = _camera.WorldToViewportPoint(h.transform.position);
            return screenPoint.x > 0.25f && screenPoint.x < 0.75f && screenPoint.y > 0.15f && screenPoint.y < 0.85f && screenPoint.z > 0;
        }).Where(h => h.transform.CompareTag(TagNameConst.PLAYER) ||
                 h.transform.CompareTag(TagNameConst.CPU) ||
                 h.transform.CompareTag(TagNameConst.JAMMING_BOT))
                 .Where(h =>
                 {
                     //notLockOnObjects内のオブジェクトがある場合は除外
                     if (notLockOnObjects.FindIndex(o => ReferenceEquals(o, h.transform.gameObject)) == -1)
                     {
                         return true;
                     }
                     return false;
                 })
                 .ToList();
    }

    //ロックオンする
    public void UseLockOn(float speed)
    {
        TrackingSpeed = speed;

        //取得したRaycastHit配列から必要な情報だけ抜き取ってリスト化
        var hits = Physics.SphereCastAll(
            cameraTransform.position,
            searchRadius,
            cameraTransform.forward,
            maxDistance).Select(h => h.transform.gameObject).ToList();

        hits = FilterTargetObject(hits);
        if (hits.Count > 0)
        {
            //何もロックオンしていない場合はロックオン対象を探す
            if (!isTarget)
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
                        t = hit;
                    }
                }

                //ロックオン画像の色変更
                _reticleImage.color = _lockingOnColor;

                //ターゲット用変数更新
                Target = t;
                targetTransform = t.transform;
                isTarget = true;
            }
            //既にロックオン済みなら追従処理
            else
            {
                //Raycast内にロックオン中だったオブジェクトがない場合はロックオン解除
                if (hits.FindIndex(h => ReferenceEquals(h, Target)) == -1)
                {
                    StopLockOn();
                    return;
                }

                //ロックオンの対象オブジェクトが消えていないなら継続して追尾
                if (Target != null)
                {
                    Vector3 diff = targetTransform.position - cameraTransform.position;   //ターゲットとの距離
                    Quaternion rotation = Quaternion.LookRotation(diff);   //ロックオンしたオブジェクトの方向

                    //カメラの角度からtrackingSpeed(0～1)の速度でロックオンしたオブジェクトの角度に向く
                    _transform.rotation = Quaternion.Slerp(_transform.rotation, rotation, TrackingSpeed);
                }
                //ロックオンしている最中に対象が消滅したらロックオン解除
                else
                {
                    StopLockOn();
                }
            }
        }
        else
        {
            StopLockOn();
        }
    }


    public void StopLockOn()
    {
        if (isTarget)
        {
            //ロックオン画像の色変更
            _reticleImage.color = _noLockOnColor;

            //ターゲット用変数の更新
            Target = null;
            targetTransform = null;
            isTarget = false;
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