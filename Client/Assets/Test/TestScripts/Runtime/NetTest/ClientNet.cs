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

namespace TestRuntime
{
    public class ClientNet : MonoBehaviour
    {
        private RSACrypto _rsaKeyMgr;
        private string publicKey;
        private void Awake()
        {
            _rsaKeyMgr= new RSACrypto();

        }

        async Task Start()
        {
            //FW.NetMgr.Create("10.23.50.187", 8888); //Company
            //FW.NetMgr.Create("10.1.2.144", 8888); Home
            GameEntry.FW.NetMgr.Create("127.0.0.1", 8888); //local
            await GameEntry.FW.NetMgr.ConnectAsync();

            GameEntry.FW.NetMgr.RegisterMsg<MonsterInfoAck>(OnMonsterInfoAck);
            GameEntry.FW.NetMgr.RegisterMsg<ServerPublicKey>(OnServerPublicKey);
        }

        private void OnServerPublicKey(ServerPublicKey msg)
        {
            publicKey = msg.Key;
            Debug.Log($"收到公钥 {msg}");


        }

        public void Send()
        {
            MonsterInfoReq msg = new MonsterInfoReq()
            {
                Id = 5,
            };

            GameEntry.FW.NetMgr.SendMsg(msg);
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
