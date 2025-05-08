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
    internal class NetTest : MonoBehaviour
    {
        TcpInstance tcpInstance;
        private void Awake()
        {
            TcpUtility.CollectMsgTypeId();

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

        private void OnMonsterInfoAck(MonsterInfoAck obj)
        {
            throw new NotImplementedException();
        }
    }
}
