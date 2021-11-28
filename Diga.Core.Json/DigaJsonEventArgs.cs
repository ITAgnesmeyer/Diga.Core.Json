

using System;
using System.Collections.Generic;
using System.IO;

namespace Diga.Core.Json
{
    /// <summary>
    /// Provides data for a JSON event.
    /// </summary>
    public sealed class DigaJsonEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DigaJsonEventArgs"/> class.
        /// </summary>
        /// <param name="writer">The writer currently in use.</param>
        /// <param name="value">The value on the stack.</param>
        /// <param name="objectGraph">The current serialization object graph.</param>
        /// <param name="options">The options currently in use.</param>
        /// <param name="name">The field or property name.</param>
        /// <param name="component">The component holding the value.</param>
        public DigaJsonEventArgs(TextWriter writer, object value, IDictionary<object, object> objectGraph, DigaJsonOptions options, string name = null, object component = null)
        {
            this.Options = options;
            this.Writer = writer;
            this.ObjectGraph = objectGraph;
            this.Value = value;
            this.Name = name;
            this.Component = component;
        }

        /// <summary>
        /// Gets the options currently in use.
        /// </summary>
        /// <value>The options.</value>
        public DigaJsonOptions Options { get; }

        /// <summary>
        /// Gets the writer currently in use.
        /// </summary>
        /// <value>The writer.</value>
        public TextWriter Writer { get; }

        /// <summary>
        /// Gets the current serialization object graph.
        /// </summary>
        /// <value>The object graph.</value>
        public IDictionary<object, object> ObjectGraph { get; }

        /// <summary>
        /// Gets the component holding the value. May be null.
        /// </summary>
        /// <value>The component.</value>
        public object Component { get; }

        /// <summary>
        /// Gets or sets the type of the event.
        /// </summary>
        /// <value>
        /// The type of the event.
        /// </value>
        public DigaJsonEventType EventType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="DigaJsonEventArgs"/> is handled.
        /// An handled object can be skipped, not written to the stream. If the object is written, First must be set to false, otherwise it must not be changed.
        /// </summary>
        /// <value><c>true</c> if handled; otherwise, <c>false</c>.</value>
        public bool Handled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the object being handled is first in the list.
        /// If the object is handled and written to the stream, this must be set to false after the stream is written.
        /// If the object is skipped, it must not be changed.
        /// </summary>
        /// <value><c>true</c> if this is the first object; otherwise, <c>false</c>.</value>
        public bool First { get; set; }

        /// <summary>
        /// Gets or sets the value on the stack.
        /// </summary>
        /// <value>The value.</value>
        public object Value { get; set; }

        /// <summary>
        /// Gets or sets the name on the stack. The Name can be a property or field name when serializing objects. May be null.
        /// </summary>
        /// <value>The value.</value>
        public string Name { get; set; }
    }



}
