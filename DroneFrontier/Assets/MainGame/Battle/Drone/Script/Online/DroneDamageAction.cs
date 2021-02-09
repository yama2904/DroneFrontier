using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace Online
{
    public class DroneDamageAction : NetworkBehaviour
    {
        const float MAX_HP = 30;
        [SyncVar] float syncHP = MAX_HP;
        public float HP { get { return syncHP; } }

        BattleDrone drone = null;
        DroneBarrierAction barrierAction = null;
        [SerializeField] float nonDamageTime = 4f;
        [SyncVar] bool syncIsNonDamage = false;

        //1フレームに8ヒットまで
        [SyncVar] int syncDamageCount = 0;
        const int MAX_COUNT_ONE_FRAME = 8;


        void Awake() { }
        void Start() { }

        public override void OnStartClient()
        {
            base.OnStartClient();
            drone = GetComponent<BattleDrone>();
            barrierAction = GetComponent<DroneBarrierAction>();
        }

        [ServerCallback]
        void Update()
        {
            if (Time.time - drone.StartTime <= nonDamageTime)
            {
                if (!syncIsNonDamage)
                {
                    SetNonDamage(true);
                }
            }
            else
            {
                if (syncIsNonDamage)
                {
                    SetNonDamage(false);
                }
            }
        }

        private void LateUpdate()
        {
            syncDamageCount = 0;
        }

        [Command(ignoreAuthority = true)]
        public void CmdDamage(float power)
        {
            if (syncHP <= 0) return;
            if (syncIsNonDamage) return;

            DamageMe(power);
        }

        [Server]
        void SetNonDamage(bool flag)
        {
            syncIsNonDamage = flag;
        }

        [Server]
        void DamageMe(float power)
        {
            if (syncDamageCount > MAX_COUNT_ONE_FRAME) return;

            //小数点第2以下切り捨て
            float p = Useful.DecimalPointTruncation(power, 1);

            if (barrierAction.HP > 0)
            {
                barrierAction.Damage(p);
            }
            else
            {
                syncHP -= p;
                if (syncHP <= 0)
                {
                    syncHP = 0;
                }

                //デバッグ用
                Debug.Log(name + "の残りHP: " + syncHP);
            }

            syncDamageCount++;
        }
    }
}