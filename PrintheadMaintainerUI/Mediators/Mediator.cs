//Solution for communicating between viewmodels is adopted from the method provided by Andy at technical-recipes.com
//Source: https://www.technical-recipes.com/2018/navigating-between-views-in-wpf-mvvm/

using System;
using System.Collections.Generic;

namespace PrintheadMaintainerUI.Mediators
{
    public static class Mediator
    {
        private static IDictionary<string, List<Action<object>>> pl_dict =
           new Dictionary<string, List<Action<object>>>();

        public static void Subscribe(string token, Action<object> callback)
        {
            if (!pl_dict.ContainsKey(token))
            {
                var list = new List<Action<object>>();
                list.Add(callback);
                pl_dict.Add(token, list);
            }
            else
            {
                bool found = false;
                foreach (var item in pl_dict[token])
                {
                    if (item.Method.ToString() == callback.Method.ToString())
                    {
                        found = true;
                    }
                }

                if (!found)
                {
                    pl_dict[token].Add(callback);
                }
            }
        }

        public static void Unsubscribe(string token, Action<object> callback)
        {
            if (pl_dict.ContainsKey(token))
            {
                pl_dict[token].Remove(callback);
            }
        }

        public static void Notify(string token, object args = null)
        {
            if (pl_dict.ContainsKey(token))
            {
                foreach (var callback in pl_dict[token])
                {
                    callback(args);
                }
            }
        }
    }
}
