using System.Collections;
// ReSharper disable UnusedMember.Local



namespace Diga.Core.Json
{
    internal class IListObject : DigaJsonListObject
    {
        private IList _list;

        public override object List
        {
            get => base.List;
            set
            {
                base.List = value;
                this._list = (IList)value;
            }
        }

        public override void Clear()
        {
            this._list.Clear();
        }

        public override void Add(object value, DigaJsonOptions options = null)
        {
            this._list.Add(value);
        }
    }

}
