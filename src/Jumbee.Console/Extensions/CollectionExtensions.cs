using System;
using System.Collections.Generic;
using System.Text;

namespace Jumbee.Console;

internal static class CollectionExtensions
{
    extension<T>(T[] arr)
    {
        public U[] Map<U>(Func<T, U> map) 
        {
            U[] ret = new U[arr.Length];
            for (int i = 0; i < arr.Length; i++)
            {
                ret[i] = map(arr[i]);
            }
            return ret;
        }
    }
}
