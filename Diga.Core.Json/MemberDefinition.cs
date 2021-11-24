using System;
using System.Collections;
using System.Collections.Generic;

namespace Diga.Core.Json
{
    /// <summary>
    /// Defines a type's member.
    /// </summary>
    public sealed class MemberDefinition
    {
        private string _name;
        private string _wireName;
        private string _escapedWireName;
        private IMemberAccessor _accessor;
        private Type _type;

        /// <summary>
        /// Gets or sets the member name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name
        {
            get => this._name;
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentException(null, nameof(value));

                this._name = value;
            }
        }

        /// <summary>
        /// Gets or sets the name used for serialization and deserialiation.
        /// </summary>
        /// <value>
        /// The name used during serialization and deserialization.
        /// </value>
        public string WireName
        {
            get => this._wireName;
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentException(null, nameof(value));

                this._wireName = value;
            }
        }

        /// <summary>
        /// Gets or sets the escaped name used during serialization and deserialiation.
        /// </summary>
        /// <value>
        /// The escaped name used during serialization and deserialiation.
        /// </value>
        public string EscapedWireName
        {
            get => this._escapedWireName;
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentException(null, nameof(value));

                this._escapedWireName = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance has default value.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance has default value; otherwise, <c>false</c>.
        /// </value>
        public bool HasDefaultValue { get; set; }

        /// <summary>
        /// Gets or sets the default value.
        /// </summary>
        /// <value>
        /// The default value.
        /// </value>
        public object DefaultValue { get; set; }

        /// <summary>
        /// Gets or sets the accessor.
        /// </summary>
        /// <value>
        /// The accessor.
        /// </value>
        public IMemberAccessor Accessor
        {
            get => this._accessor;
            set
            {
                this._accessor = value ?? throw new ArgumentNullException(nameof(value));
            }
        }

        /// <summary>
        /// Gets or sets the member type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        public Type Type
        {
            get => this._type;
            set
            {
                this._type = value ?? throw new ArgumentNullException(nameof(value));
            }
        }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return this.Name;
        }

        /// <summary>
        /// Gets or creates a member instance.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="elementsCount">The elements count.</param>
        /// <param name="options">The options.</param>
        /// <returns>A new or existing instance.</returns>
        public object GetOrCreateInstance(object target, int elementsCount, JsonOptions options = null)
        {
            object targetValue;
            if (options != null && options.SerializationOptions.HasFlag(JsonSerializationOptions.ContinueOnValueError))
            {
                try
                {
                    targetValue = this.Accessor.Get(target);
                }
                catch
                {
                    return null;
                }
            }
            else
            {
                targetValue = this.Accessor.Get(target);
            }

            // sufficient array?
            if (targetValue == null || targetValue is Array array && array.GetLength(0) < elementsCount)
            {
                if (this.Type.IsInterface)
                    return null;

                targetValue = Json.CreateInstance(target, this.Type, elementsCount, options, targetValue);
                if (targetValue != null)
                {
                    this.Accessor.Set(target, targetValue);
                }
            }
            return targetValue;
        }

        /// <summary>
        /// Applies the dictionary entry to this member.
        /// </summary>
        /// <param name="dictionary">The input dictionary.</param>
        /// <param name="target">The target object.</param>
        /// <param name="key">The entry key.</param>
        /// <param name="value">The entry value.</param>
        /// <param name="options">The options.</param>
        public void ApplyEntry(IDictionary dictionary, object target, string key, object value, JsonOptions options = null)
        {
            if (options?.ApplyEntryCallback != null)
            {
                var og = new Dictionary<object, object>
                {
                    ["dictionary"] = dictionary,
                    ["member"] = this
                };

                var e = new JsonEventArgs(null, value, og, options, key, target)
                {
                    EventType = JsonEventType.ApplyEntry
                };
                options.ApplyEntryCallback(e);
                if (e.Handled)
                    return;

                value = e.Value;
            }

            if (value is IDictionary dic)
            {
                var targetValue = GetOrCreateInstance(target, dic.Count, options);
                Json.Apply(dic, targetValue, options);
                return;

            }

            var lo = Json.GetListObject(this.Type, options, target, value, dictionary, key);
            if (lo != null)
            {
                if (value is IEnumerable enumerable)
                {
                    lo.List = GetOrCreateInstance(target, enumerable is ICollection coll ? coll.Count : 0, options);
                    Json.ApplyToListTarget(target, enumerable, lo, options);
                    return;
                }
            }


            var cvalue = Json.ChangeType(target, value, this.Type, options);
            this.Accessor.Set(target, cvalue);
        }

        /// <summary>
        /// Determines whether the specified value is equal to the zero value for its type.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>true if the specified value is equal to the zero value.</returns>
        public bool IsNullDateTimeValue(object value)
        {
            return value == null || DateTime.MinValue.Equals(value);
        }

        /// <summary>
        /// Determines whether the specified value is equal to the zero value for its type.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>true if the specified value is equal to the zero value.</returns>
        public bool IsZeroValue(object value)
        {
            if (value == null)
                return false;

            var type = value.GetType();
            if (type != this.Type)
                return false;

            return Json.IsZeroValueType(value);
        }

        /// <summary>
        /// Determines if a value equals the default value.
        /// </summary>
        /// <param name="value">The value to compare.</param>
        /// <returns>true if both values are equal; false otherwise.</returns>
        public bool EqualsDefaultValue(object value)
        {
            return Json.AreValuesEqual(this.DefaultValue, value);
        }

        /// <summary>
        /// Removes a deserialization member.
        /// </summary>
        /// <param name="type">The type. May not be null.</param>
        /// <param name="options">The options. May be null.</param>
        /// <param name="member">The member. May not be null.</param>
        /// <returns>true if item is successfully removed; otherwise, false.</returns>
        public static bool RemoveDeserializationMember(Type type, JsonOptions options, MemberDefinition member)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (member == null)
                throw new ArgumentNullException(nameof(member));

            options = options ?? new JsonOptions();
            return TypeDef.RemoveDeserializationMember(type, options, member);
        }

        /// <summary>
        /// Removes a serialization member.
        /// </summary>
        /// <param name="type">The type. May not be null.</param>
        /// <param name="options">The options. May be null.</param>
        /// <param name="member">The member. May not be null.</param>
        /// <returns>true if item is successfully removed; otherwise, false.</returns>
        public static bool RemoveSerializationMember(Type type, JsonOptions options, MemberDefinition member)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (member == null)
                throw new ArgumentNullException(nameof(member));

            options = options ?? new JsonOptions();
            return TypeDef.RemoveSerializationMember(type, options, member);
        }

        /// <summary>
        /// Adds a deserialization member.
        /// </summary>
        /// <param name="type">The type. May not be null.</param>
        /// <param name="options">The options. May be null.</param>
        /// <param name="member">The member. May not be null.</param>
        /// <returns>true if item is successfully added; otherwise, false.</returns>
        public static void AddDeserializationMember(Type type, JsonOptions options, MemberDefinition member)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (member == null)
                throw new ArgumentNullException(nameof(member));

            options = options ?? new JsonOptions();
            TypeDef.AddDeserializationMember(type, options, member);
        }

        /// <summary>
        /// Adds a serialization member.
        /// </summary>
        /// <param name="type">The type. May not be null.</param>
        /// <param name="options">The options. May be null.</param>
        /// <param name="member">The member. May not be null.</param>
        /// <returns>true if item is successfully added; otherwise, false.</returns>
        public static void AddSerializationMember(Type type, JsonOptions options, MemberDefinition member)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (member == null)
                throw new ArgumentNullException(nameof(member));

            options = options ?? new JsonOptions();
            TypeDef.AddSerializationMember(type, options, member);
        }

        /// <summary>
        /// Gets the serialization members for a given type.
        /// </summary>
        /// <param name="type">The type. May not be null.</param>
        /// <param name="options">The options. May be null.</param>
        /// <returns>A list of serialization members.</returns>
        public static MemberDefinition[] GetSerializationMembers(Type type, JsonOptions options = null)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            options = options ?? new JsonOptions();
            return TypeDef.GetSerializationMembers(type, options);
        }

        /// <summary>
        /// Gets the deserialization members for a given type.
        /// </summary>
        /// <param name="type">The type. May not be null.</param>
        /// <param name="options">The options. May be null.</param>
        /// <returns>A list of deserialization members.</returns>
        public static MemberDefinition[] GetDeserializationMembers(Type type, JsonOptions options = null)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            options = options ?? new JsonOptions();
            return TypeDef.GetDeserializationMembers(type, options);
        }

        /// <summary>
        /// Run a specified action, using the member definition lock.
        /// </summary>
        /// <typeparam name="T">The action input type.</typeparam>
        /// <param name="action">The action. May not be null.</param>
        /// <param name="state">The state. May be null.</param>
        public static void UsingLock<T>(Action<T> action, T state)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            TypeDef.Lock(action, state);
        }
    }
}