using System.Reflection;

namespace Diga.Core.Json
{
    internal class FieldInfoAccessor : IMemberAccessor
    {
        private readonly FieldInfo _fi;

        public FieldInfoAccessor(FieldInfo fi)
        {
            this._fi = fi;
        }

        public object Get(object component)
        {
            return this._fi.GetValue(component);
        }

        public void Set(object component, object value)
        {
            this._fi.SetValue(component, value);
        }
    }
}