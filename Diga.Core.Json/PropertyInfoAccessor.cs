using System;
using System.Reflection;
// ReSharper disable UnusedMember.Local



namespace Diga.Core.Json
{
    internal class PropertyInfoAccessor<TComponent, TMember> : IMemberAccessor
    {
        private readonly JFunc<TComponent, TMember> _get;
        private readonly JAction<TComponent, TMember> _set;

        public PropertyInfoAccessor(PropertyInfo pi)
        {
            var get = pi.GetGetMethod();
            if (get != null)
            {
                this._get = (JFunc<TComponent, TMember>)Delegate.CreateDelegate(typeof(JFunc<TComponent, TMember>), get);
            }

            var set = pi.GetSetMethod();
            if (set != null)
            {
                this._set = (JAction<TComponent, TMember>)Delegate.CreateDelegate(typeof(JAction<TComponent, TMember>), set);
            }
        }

        public object Get(object component)
        {
            if (this._get == null)
                return null;

            return this._get((TComponent)component);
        }

        public void Set(object component, object value)
        {
            if (this._set == null)
                return;

            this._set((TComponent)component, (TMember)value);
        }
    }



}
