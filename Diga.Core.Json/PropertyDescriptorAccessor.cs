using System.ComponentModel;
// ReSharper disable UnusedMember.Local



namespace Diga.Core.Json
{
    internal class PropertyDescriptorAccessor : IMemberAccessor
    {
        private readonly PropertyDescriptor _pd;

        public PropertyDescriptorAccessor(PropertyDescriptor pd)
        {
            this._pd = pd;
        }

        public object Get(object component)
        {
            return this._pd.GetValue(component);
        }

        public void Set(object component, object value)
        {
            if (this._pd.IsReadOnly)
                return;

            this._pd.SetValue(component, value);
        }
    }



}
