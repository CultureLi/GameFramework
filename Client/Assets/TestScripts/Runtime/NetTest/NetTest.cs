using Framework;
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
        TcpInstance tcpInstance;
        private void Awake()
        {
            MsgTypeIdUtility.Init();

            tcpInstance = new TcpInstance();
            tcpInstance.Connect("10.23.50.187", 8888);

            tcpInstance.RegisterMsg<MonsterInfoAck>(OnMonsterInfoAck);
        }

        public void Send()
        {
            MonsterInfoReq msg = new MonsterInfoReq()
            {
                Id = 5,
            };

            tcpInstance.SendMsg(msg);
        }

        private void OnMonsterInfoAck(MonsterInfoAck ack)
        {
            Debug.Log("客户端收到了 MonsterInfoAck");

            foreach (var data in ack.Data.Buffs)
            {
                Debug.Log($"{data.Id}");
            }
        }

        public void Update()
        {
            tcpInstance.Update(Time.deltaTime, Time.fixedDeltaTime);
        }

        private void OnDestroy()
        {
            tcpInstance.Dispose();
        }
    }
}
