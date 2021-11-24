using System;
using System.Collections;
// ReSharper disable UnusedMember.Local



namespace Diga.Core.Json
{
    internal class KeyValueTypeDictionary : IDictionary
    {
        private readonly KeyValueTypeEnumerator _enumerator;

        public KeyValueTypeDictionary(object value)
        {
            this._enumerator = new KeyValueTypeEnumerator(value);
        }

        public int Count => throw new NotSupportedException();
        public bool IsSynchronized => throw new NotSupportedException();
        public object SyncRoot => throw new NotSupportedException();
        public bool IsFixedSize => throw new NotSupportedException();
        public bool IsReadOnly => throw new NotSupportedException();
        public ICollection Keys => throw new NotSupportedException();
        public ICollection Values => throw new NotSupportedException();
        public object this[object key]
        {
            get => throw new NotSupportedException(); set => throw new NotSupportedException();
        }

        void IDictionary.Add(object key, object value)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(object key)
        {
            throw new NotSupportedException();
        }

        public IDictionaryEnumerator GetEnumerator()
        {
            return this._enumerator;
        }

        public void Remove(object key)
        {
            throw new NotSupportedException();
        }

        public void CopyTo(Array array, int index)
        {
            throw new NotSupportedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotSupportedException();
        }
    }
}
