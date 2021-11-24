using System.Collections;
using System.Reflection;
// ReSharper disable UnusedMember.Local



namespace Diga.Core.Json
{
    internal class KeyValueTypeEnumerator : IDictionaryEnumerator
    {
        private readonly IEnumerator _enumerator;
        private PropertyInfo _keyProp;
        private PropertyInfo _valueProp;

        public KeyValueTypeEnumerator(object value)
        {
            this._enumerator = ((IEnumerable)value).GetEnumerator();
        }

        public DictionaryEntry Entry
        {
            get
            {
                if (this._keyProp == null)
                {
                    object current = this._enumerator.Current;
                    if (current != null)
                    {
                        this._keyProp = current.GetType().GetProperty("Key");
                        this._valueProp = current.GetType().GetProperty("Value");
                    }

                }

                var kValue = this._keyProp?.GetValue(this._enumerator.Current, null);
                var vValue = this._valueProp?.GetValue(this._enumerator.Current, null);
                if (kValue == null)
                    return default;

                return new DictionaryEntry(kValue, vValue);
            }
        }

        public object Key => this.Entry.Key;
        public object Value => this.Entry.Value;
        public object Current => this.Entry;

        public bool MoveNext()
        {
            return this._enumerator.MoveNext();
        }

        public void Reset()
        {
            this._enumerator.Reset();
        }
    }
}
