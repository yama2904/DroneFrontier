using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Offline
{
    public class ItemCreate : MonoBehaviour
    {
        [SerializeField] Item spawnItem = null;
        [SerializeField, Tooltip("スポーン確率(0～1)")] float spawnPercent = 0.5f;
        [SerializeField, Tooltip("地面にアイテムが潜らない用")] float minPosY = 57f;
        Item createdItem = null;
        public float SpawnPercent { get { return spawnPercent; } }

        //キャッシュ用
        Transform cacheTransform = null;


        void Awake()
        {
            cacheTransform = transform;
            GetComponent<Renderer>().enabled = false;

            //scaleを1に戻す
            transform.localScale = new Vector3(1, 1, 1);
        }

        void LateUpdate()
        {
            if (transform.position.y < minPosY)
            {
                transform.position = new Vector3(transform.position.x, minPosY, transform.position.z);
            }
        }

        //スポーンしたらtrue
        public bool RandomSpawn()
        {
            //既にスポーンしていて取得されていなかったらスポーンしない
            if (createdItem != null) return false;

            if (Random.Range(0, 1.0f) <= spawnPercent)
            {
                createdItem = Instantiate(spawnItem, transform);
                createdItem.SetRandomItemType();

                return true;
            }
            return false;
        }
    }
}