using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework
{
    public static class ListExtension
    {
        public static T RemoveLast<T>(this List<T> list)
        {
            var count = list.Count;
            if (count > 0)
            {
                var item = list[count - 1];
                list.RemoveAt(count - 1);
                return item;
            }
            return default(T);
        }

        public static T First<T>(this List<T> list)
        {
            var count = list.Count;
            if (count > 0)
            {
                var item = list[0];
                return item;
            }
            return default(T);
        }

        public static T Last<T>(this List<T> list)
        {
            var count = list.Count;
            if (count > 0)
            {
                var item = list[count - 1];
                return item;
            }
            return default(T);
        }
    }
}
