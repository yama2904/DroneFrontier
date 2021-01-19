using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class MissileBullet : Bullet
{
    [SerializeField] Explosion explosion = null;
    [SyncVar, HideInInspector] public uint parentNetId = 0;
    [SyncVar] bool isShot = false;
    AudioSource audioSource = null;


    public override void OnStartClient()
    {
        base.OnStartClient();
        cacheTransform = GetComponent<Rigidbody>().transform;
        GameObject parent = NetworkIdentity.spawned[parentNetId].gameObject;
        cacheTransform.SetParent(parent.transform);
        cacheTransform.localPosition = new Vector3(0, 0, 0);
        cacheTransform.localRotation = Quaternion.Euler(90, 0, 0);

        audioSource = GetComponent<AudioSource>();
        audioSource.clip = SoundManager.GetAudioClip(SoundManager.SE.MISSILE);
    }

    void Start() { }

    [ServerCallback]
    protected override void FixedUpdate()
    {
        if (!isShot) return;

        //90度傾けたままだと誘導がバグるので一旦直す
        cacheTransform.Rotate(new Vector3(-90, 0, 0));
        base.FixedUpdate();
        cacheTransform.Rotate(new Vector3(90, 0, 0));
    }

    [ServerCallback]
    protected override void OnTriggerEnter(Collider other)
    {
        if (!isShot) return;

        //当たり判定を行わないオブジェクトだったら処理をしない
        if (ReferenceEquals(other.gameObject, Shooter))
        {
            return;
        }

        if (other.CompareTag(TagNameManager.PLAYER))
        {
            other.GetComponent<Player>().CmdDamage(Power);
            DestroyMe();
        }
        else if (other.CompareTag(TagNameManager.JAMMING_BOT))
        {
            JammingBot jb = other.GetComponent<JammingBot>();
            if (jb.creater == Shooter)
            {
                return;
            }
            jb.CmdDamage(Power);
            DestroyMe();
        }
    }

    void DestroyMe()
    {
        CmdCreateExplosion();
        NetworkServer.Destroy(gameObject);
    }

    private Explosion CreateExplosion()
    {
        Explosion e = Instantiate(explosion, cacheTransform.position, Quaternion.identity);
        e.Shooter = Shooter;
        return e;
    }

    [Command(ignoreAuthority = true)]
    void CmdCreateExplosion()
    {
        Explosion e = CreateExplosion();
        NetworkServer.Spawn(e.gameObject);
    }

    [Command(ignoreAuthority = true)]
    public void CmdShot(GameObject target)
    {
        RpcParentNull();
        RpcPlaySE();

        Invoke(nameof(DestroyMe), DestroyTime);
        Target = target;
        isShot = true;
    }

    [ClientRpc]
    void RpcPlaySE()
    {
        audioSource.volume = SoundManager.BaseSEVolume;
        audioSource.Play();
    }

    [ClientRpc]
    void RpcParentNull()
    {
        transform.parent = null;
    }
}