using System.Collections.Generic;
using System.Runtime.CompilerServices;
// ReSharper disable UnusedMember.Local



namespace Diga.Core.Json
{
    internal class ReferenceComparer : IEqualityComparer<object>
    {
        internal static readonly ReferenceComparer _current = new ReferenceComparer();

        bool IEqualityComparer<object>.Equals(object x, object y)
        {
            return ReferenceEquals(x, y);
        }

        int IEqualityComparer<object>.GetHashCode(object obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }

}
