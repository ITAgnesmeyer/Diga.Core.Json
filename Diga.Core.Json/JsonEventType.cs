

namespace Diga.Core.Json
{
    /// <summary>
    /// Defines a type of JSON event.
    /// </summary>
    public enum JsonEventType
    {
        /// <summary>
        /// An unspecified type of event.
        /// </summary>
        Unspecified,

        /// <summary>
        /// The write value event type.
        /// </summary>
        WriteValue,

        /// <summary>
        /// The before write object event type.
        /// </summary>
        BeforeWriteObject,

        /// <summary>
        /// The after write object event type.
        /// </summary>
        AfterWriteObject,

        /// <summary>
        /// The write named value object event type.
        /// </summary>
        WriteNamedValueObject,

        /// <summary>
        /// The create instance event type.
        /// </summary>
        CreateInstance,

        /// <summary>
        /// The map entry event type.
        /// </summary>
        MapEntry,

        /// <summary>
        /// The apply entry event type.
        /// </summary>
        ApplyEntry,

        /// <summary>
        /// The get list object event type.
        /// </summary>
        GetListObject,
    }



}
