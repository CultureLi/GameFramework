using Framework;
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
        private void Awake()
        {
            TcpInstance tcpInstance = new TcpInstance();
            tcpInstance.Connect("10.23.50.187", 8888);
        }
    }
}
