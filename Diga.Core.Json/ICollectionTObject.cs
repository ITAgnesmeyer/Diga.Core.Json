using System.Collections.Generic;
// ReSharper disable UnusedMember.Local



namespace Diga.Core.Json
{
    internal class ICollectionTObject<T> : JsonListObject
    {
        private ICollection<T> _coll;

        public override object List
        {
            get => base.List;
            set
            {
                base.List = value;
                this._coll = (ICollection<T>)value;
            }
        }

        public override void Clear()
        {
            this._coll.Clear();
        }

        public override void Add(object value, JsonOptions options = null)
        {
            if (value == null && typeof(T).IsValueType)
            {
                Json.HandleException(new JsonException("JSO0014: JSON error detected. Cannot add null to a collection of '" + typeof(T) + "' elements."), options);
            }

            this._coll.Add((T)value);
        }
    }



}
