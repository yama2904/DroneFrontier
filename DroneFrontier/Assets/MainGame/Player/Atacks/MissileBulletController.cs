using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissileBulletController : MonoBehaviour
{
    //子オブジェクトの名前
    const string BULLET_OBJECT_NAME = "Missile";
    const string EXPLOSION_OBJECT_NAME = "Explosion";

    GameObject missile;
    GameObject explosion;

    void Start()
    {
        missile = transform.Find(BULLET_OBJECT_NAME).gameObject;
        explosion = transform.Find(EXPLOSION_OBJECT_NAME).gameObject;

        missile.SetActive(true);
        explosion.SetActive(false);
    }

    public void StartExplosion(Vector3 position)
    {
        missile.SetActive(false);

        explosion.transform.position = position;
        explosion.SetActive(true);
    }

    public void DestroyMissiles()
    {
        Destroy(gameObject);
    }
}
