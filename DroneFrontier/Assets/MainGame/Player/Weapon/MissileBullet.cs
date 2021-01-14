using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class MissileBullet : Bullet
{
    [SerializeField] Explosion explosion = null;
    [SyncVar, HideInInspector] public Transform myTransform = null;
    [SyncVar, HideInInspector] public Transform parentTransform = null;
    [SyncVar] bool isShot = false;

    public override void OnStartClient()
    {
        base.OnStartClient();
        Init();
        transform.SetParent(parentTransform);
        transform.localPosition = new Vector3(0, 0, 0);
    }

    void Awake()
    {
    }

    protected override void Start()
    {
    }

    protected override void Update()
    {
    }

    protected override void FixedUpdate()
    {
        if (!isShot) return;

        //90度傾けたままだと誘導がバグるので一旦直す
        cacheTransform.Rotate(new Vector3(-90, 0, 0)); 
        base.FixedUpdate();
        cacheTransform.Rotate(new Vector3(90, 0, 0));
    }

    protected override void OnTriggerEnter(Collider other)
    {
        if (!isShot) return;

        //当たり判定を行わないオブジェクトだったら処理をしない
        if (ReferenceEquals(other.gameObject, Shooter))
        {
            return;
        }

        if (other.CompareTag(Player.PLAYER_TAG) || other.CompareTag(CPUController.CPU_TAG))
        {
            BasePlayer bp = other.GetComponent<BasePlayer>();
            
            bp.Damage(Power);
            DestroyMe();
        }
        else if (other.CompareTag(JammingBot.JAMMING_BOT_TAG))
        {
            JammingBot jb = other.GetComponent<JammingBot>();
            if(jb.Creater == Shooter)
            {
                return;
            }
            jb.Damage(Power);
            DestroyMe();
        }
    }

   void DestroyMe()
    {
        if (MainGameManager.IsMulti)
        {
            CmdCreateExplosion();
            NetworkServer.Destroy(gameObject);
        }
        else
        {
            CreateExplosion();
            Destroy(gameObject);
        }
    }

    private Explosion CreateExplosion()
    {
        Explosion e = Instantiate(explosion, cacheTransform.position, Quaternion.identity);
        e.Shooter = Shooter;
        return e;
    }

    [Command]
    void CmdCreateExplosion()
    {
        Explosion e = CreateExplosion();
        NetworkServer.Spawn(e.gameObject);
    }

    public void Init()
    {
        cacheTransform = transform;
        myTransform = transform;
        myTransform.localRotation = Quaternion.Euler(90, 0, 0);    //オブジェクトを90度傾ける
    }

    public void Shot()
    {
        Invoke(nameof(DestroyMe), DestroyTime);
        isShot = true;
    }
}