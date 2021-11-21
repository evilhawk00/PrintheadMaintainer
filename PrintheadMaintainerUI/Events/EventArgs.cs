//Solution for communicating between viewmodels is adopted from the method provided by Andy at technical-recipes.com
//Source: https://www.technical-recipes.com/2018/navigating-between-views-in-wpf-mvvm/

using System;

namespace PrintheadMaintainerUI.Events
{
    public class EventArgs<T> : EventArgs
    {
        public EventArgs(T value)
        {
            Value = value;
        }

        public T Value { get; private set; }
    }
}
