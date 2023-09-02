using GameEngine.Runtime.Base;
using System;
using System.Collections;
using System.Collections.Generic;

namespace GameEngine.Runtime.Logic.Data
{
    public class DataMgr : Singleton<DataMgr>
    {
        Dictionary<Type, DataBase> modelMap = new();

        public T CreateModel<T>() where T : DataBase
        {
            var type = typeof(T);
            if (!modelMap.TryGetValue(type, out var model))
            {
                model = Activator.CreateInstance<T>();
                model.OnInit();
                modelMap[type] = model;
            }

            return model as T;
        }


        public void Release()
        {
            foreach (var kv in modelMap)
            {
                kv.Value.OnRelease();
            }

            modelMap.Clear();
        }

    }
}