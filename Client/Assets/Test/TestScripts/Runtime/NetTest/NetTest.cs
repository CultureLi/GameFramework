using Framework;
using GameEntry;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.TestScripts.Runtime.NetTest
{
    public class NetTest : MonoBehaviour
    {
        
        private void Awake()
        {
            FW.NetMgr.Create("10.23.50.187", 8888);
            FW.NetMgr.Connect();

            FW.NetMgr.RegisterMsg<MonsterInfoAck>(OnMonsterInfoAck);
        }

        public void Send()
        {
            MonsterInfoReq msg = new MonsterInfoReq()
            {
                Id = 5,
            };

            FW.NetMgr.SendMsg(msg);
        }

        private void OnMonsterInfoAck(MonsterInfoAck ack)
        {
            Debug.Log("客户端收到了 MonsterInfoAck");

            foreach (var data in ack.Data.Buffs)
            {
                Debug.Log($"{data.Id}");
            }
        }
    }
}
