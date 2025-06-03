﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Framework
{
    public static class AsyncOperationHandleEx
    {
        public static AsyncOperationHandle<TObject> AddCompleted<TObject>(this AsyncOperationHandle<TObject> handle, Action<AsyncOperationHandle<TObject>> value)
        {
            if (handle.IsValid() && !handle.IsDone)
                handle.Completed += value;
            else
                value(handle);
            return handle;
        }

        public static AsyncOperationHandle<TObject> RemoveCompleted<TObject>(this AsyncOperationHandle<TObject> handle, Action<AsyncOperationHandle<TObject>> value)
        {
            if (handle.IsValid())
                handle.Completed -= value;
            return handle;
        }

        public static AsyncOperationHandle AddCompleted(this AsyncOperationHandle handle, Action<AsyncOperationHandle> value)
        {
            if (handle.IsValid() && !handle.IsDone)
                handle.Completed += value;
            else
                value(handle);
            return handle;
        }

        public static AsyncOperationHandle RemoveCompleted(this AsyncOperationHandle handle, Action<AsyncOperationHandle> value)
        {
            if (handle.IsValid())
                handle.Completed -= value;
            return handle;
        }
    }
}
