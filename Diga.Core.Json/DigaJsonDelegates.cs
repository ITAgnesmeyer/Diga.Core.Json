
// ReSharper disable UnusedMember.Local



namespace Diga.Core.Json
{
    internal delegate TResult JFunc<T, TResult>(T arg);
    internal delegate void JAction<T1, T2>(T1 arg1, T2 arg2);


    /// <summary>
    /// Defines a callback delegate to customize JSON serialization and deserialization.
    /// </summary>
    public delegate void DigaJsonCallback(DigaJsonEventArgs e);

}
