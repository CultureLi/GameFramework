using Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts
{
    internal class CustomObject : ObjectBase
    {
        public CustomObject(string name, GameObject go)
        {
            Initialize(name, go);
        }
        protected override void Release(bool isShutdown)
        {
        }

        protected override void OnSpawn()
        {
            var go = (Target as GameObject);
            go.SetActive(true);
        }

        protected override void OnUnspawn()
        {
            (Target as GameObject).SetActive(false);
        }
    }
}
