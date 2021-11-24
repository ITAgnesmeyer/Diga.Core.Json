using System;
using System.Collections.Generic;
using System.Globalization;



namespace Diga.Core.Json
{
    /// <summary>
    /// Define options for JSON.
    /// </summary>
    public sealed class JsonOptions
    {
        private readonly List<Exception> _exceptions = new List<Exception>();
        internal static DateTimeStyles _defaultDateTimeStyles = DateTimeStyles.AssumeUniversal | DateTimeStyles.AllowInnerWhite | DateTimeStyles.AllowLeadingWhite | DateTimeStyles.AllowTrailingWhite | DateTimeStyles.AllowWhiteSpaces;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonOptions" /> class.
        /// </summary>
        public JsonOptions()
        {
            this.SerializationOptions = JsonSerializationOptions.Default;
            this.ThrowExceptions = true;
            this.DateTimeStyles = _defaultDateTimeStyles;
            this.FormattingTab = " ";
            this.StreamingBufferChunkSize = ushort.MaxValue;
            this.MaximumExceptionsCount = 100;
        }

        /// <summary>
        /// Gets or sets a value indicating whether exceptions can be thrown during serialization or deserialization.
        /// If this is set to false, exceptions will be stored in the Exceptions collection.
        /// However, if the number of exceptions is equal to or higher than MaximumExceptionsCount, an exception will be thrown.
        /// </summary>
        /// <value>
        /// <c>true</c> if exceptions can be thrown on serialization or deserialization; otherwise, <c>false</c>.
        /// </value>
        public bool ThrowExceptions { get; set; }

        /// <summary>
        /// Gets or sets the maximum exceptions count.
        /// </summary>
        /// <value>
        /// The maximum exceptions count.
        /// </value>
        public int MaximumExceptionsCount { get; set; }

        /// <summary>
        /// Gets or sets the JSONP callback. It will be added as wrapper around the result.
        /// Check this article for more: http://en.wikipedia.org/wiki/JSONP
        /// </summary>
        /// <value>
        /// The JSONP callback name.
        /// </value>
        public string JsonPCallback { get; set; }

        /// <summary>
        /// Gets or sets the guid format.
        /// </summary>
        /// <value>
        /// The guid format.
        /// </value>
        public string GuidFormat { get; set; }

        /// <summary>
        /// Gets or sets the date time format.
        /// </summary>
        /// <value>
        /// The date time format.
        /// </value>
        public string DateTimeFormat { get; set; }

        /// <summary>
        /// Gets or sets the date time offset format.
        /// </summary>
        /// <value>
        /// The date time offset format.
        /// </value>
        public string DateTimeOffsetFormat { get; set; }

        /// <summary>
        /// Gets or sets the date time styles.
        /// </summary>
        /// <value>
        /// The date time styles.
        /// </value>
        public DateTimeStyles DateTimeStyles { get; set; }

        /// <summary>
        /// Gets or sets the size of the streaming buffer chunk. Minimum value is 512.
        /// </summary>
        /// <value>
        /// The size of the streaming buffer chunk.
        /// </value>
        public int StreamingBufferChunkSize { get; set; }

        /// <summary>
        /// Gets or sets the formatting tab string.
        /// </summary>
        /// <value>
        /// The formatting tab.
        /// </value>
        public string FormattingTab { get; set; }

        /// <summary>
        /// Gets the deseralization exceptions. Will be empty if ThrowExceptions is set to false.
        /// </summary>
        /// <value>
        /// The list of deseralization exceptions.
        /// </value>
        public Exception[] Exceptions => this._exceptions.ToArray();

        /// <summary>
        /// Finalizes the serialization members from an initial setup of members.
        /// </summary>
        /// <param name="type">The input type. May not be null.</param>
        /// <param name="members">The members. May not be null.</param>
        /// <returns>A non-null list of members.</returns>
        public IEnumerable<MemberDefinition> FinalizeSerializationMembers(Type type, IEnumerable<MemberDefinition> members)
        {
            return members;
        }

        /// <summary>
        /// Finalizes the deserialization members from an initial setup of members.
        /// </summary>
        /// <param name="type">The input type. May not be null.</param>
        /// <param name="members">The members. May not be null.</param>
        /// <returns>A non-null list of members.</returns>
        public IEnumerable<MemberDefinition> FinalizeDeserializationMembers(Type type, IEnumerable<MemberDefinition> members)
        {
            return members;
        }

        /// <summary>
        /// Gets or sets the serialization options.
        /// </summary>
        /// <value>The serialization options.</value>
        public JsonSerializationOptions SerializationOptions { get; set; }

        /// <summary>
        /// Gets or sets a write value callback.
        /// </summary>
        /// <value>The callback.</value>
        public JsonCallback WriteValueCallback { get; set; }

        /// <summary>
        /// Gets or sets a callback that is called before an object (not a value) is serialized.
        /// </summary>
        /// <value>The callback.</value>
        public JsonCallback BeforeWriteObjectCallback { get; set; }

        /// <summary>
        /// Gets or sets a callback that is called before an object (not a value) is serialized.
        /// </summary>
        /// <value>The callback.</value>
        public JsonCallback AfterWriteObjectCallback { get; set; }

        /// <summary>
        /// Gets or sets a callback that is called before an object field or property is serialized.
        /// </summary>
        /// <value>The callback.</value>
        public JsonCallback WriteNamedValueObjectCallback { get; set; }

        /// <summary>
        /// Gets or sets a callback that is called before an instance of an object is created.
        /// </summary>
        /// <value>The callback.</value>
        public JsonCallback CreateInstanceCallback { get; set; }

        /// <summary>
        /// Gets or sets a callback that is called during deserialization, before a dictionary entry is mapped to a target object.
        /// </summary>
        /// <value>The callback.</value>
        public JsonCallback MapEntryCallback { get; set; }

        /// <summary>
        /// Gets or sets a callback that is called during deserialization, before a dictionary entry is applied to a target object.
        /// </summary>
        /// <value>The callback.</value>
        public JsonCallback ApplyEntryCallback { get; set; }

        /// <summary>
        /// Gets or sets a callback that is called during deserialization, to deserialize a list object.
        /// </summary>
        /// <value>The callback.</value>
        public JsonCallback GetListObjectCallback { get; set; }

        /// <summary>
        /// Adds an exception to the list of exceptions.
        /// </summary>
        /// <param name="error">The exception to add.</param>
        public void AddException(Exception error)
        {
            if (error == null)
                throw new ArgumentNullException(nameof(error));

            if (this._exceptions.Count >= this.MaximumExceptionsCount)
                throw new JsonException("JSO0015: Two many JSON errors detected (" + this._exceptions.Count + ").", error);

            this._exceptions.Add(error);
        }

        internal int FinalStreamingBufferChunkSize => Math.Max(512, this.StreamingBufferChunkSize);

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns>A newly created insance of this class with all values copied.</returns>
        public JsonOptions Clone()
        {
            var clone = new JsonOptions
            {
                AfterWriteObjectCallback = this.AfterWriteObjectCallback,
                ApplyEntryCallback = this.ApplyEntryCallback,
                BeforeWriteObjectCallback = this.BeforeWriteObjectCallback,
                CreateInstanceCallback = this.CreateInstanceCallback,
                DateTimeFormat = this.DateTimeFormat,
                DateTimeOffsetFormat = this.DateTimeOffsetFormat,
                DateTimeStyles = this.DateTimeStyles
            };
            clone._exceptions.AddRange(this._exceptions);
            clone.FormattingTab = this.FormattingTab;
            clone.GetListObjectCallback = this.GetListObjectCallback;
            clone.GuidFormat = this.GuidFormat;
            clone.MapEntryCallback = this.MapEntryCallback;
            clone.MaximumExceptionsCount = this.MaximumExceptionsCount;
            clone.SerializationOptions = this.SerializationOptions;
            clone.StreamingBufferChunkSize = this.StreamingBufferChunkSize;
            clone.ThrowExceptions = this.ThrowExceptions;
            clone.WriteNamedValueObjectCallback = this.WriteNamedValueObjectCallback;
            clone.WriteValueCallback = this.WriteValueCallback;
            return clone;
        }

        /// <summary>
        /// Gets a key that can be used for type cache.
        /// </summary>
        /// <returns>A cache key.</returns>
        public string GetCacheKey()
        {
            return ((int)this.SerializationOptions).ToString();
        }

       
    }


}
