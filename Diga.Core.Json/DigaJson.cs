using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;

namespace Diga.Core.Json
{
    /// <summary>
    /// A utility class to serialize and deserialize JSON.
    /// </summary>
    public static class DigaJson
    {
        private const string _null = "null";
        private const string _true = "true";
        private const string _false = "false";
        private const string _zeroArg = "{0}";
        private const string _dateStartJs = "new Date(";
        private const string _dateEndJs = ")";
        private const string _dateStart = @"""\/Date(";
        private const string _dateStart2 = @"/Date(";
        private const string _dateEnd = @")\/""";
        private const string _dateEnd2 = @")/";
        private const string _roundTripFormat = "R";
        private const string _enumFormat = "D";
        private const string _x4Format = "{0:X4}";
        private const string _d2Format = "D2";
        private const string _scriptIgnore = "ScriptIgnore";
        private const string _serializationTypeToken = "__type";

        private static readonly string[] _dateFormatsUtc = { "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'", "yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'", "yyyy'-'MM'-'dd'T'HH':'mm'Z'", "yyyyMMdd'T'HH':'mm':'ss'Z'" };
        private static readonly DateTime _minDateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly long _minDateTimeTicks = _minDateTime.Ticks;
        private static readonly FormatterConverter _defaultFormatterConverter = new FormatterConverter();

        /// <summary>
        /// Serializes the specified object. Supports anonymous and dynamic types.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        /// <param name="options">Options to use for serialization.</param>
        /// <returns>
        /// A JSON representation of the serialized object.
        /// </returns>
        public static string Serialize(object value, DigaJsonOptions options = null)
        {
            using (var writer = new StringWriter())
            {
                Serialize(writer, value, options);
                return writer.ToString();
            }
        }

        /// <summary>
        /// Serializes the specified object to the specified TextWriter. Supports anonymous and dynamic types.
        /// </summary>
        /// <param name="writer">The output writer. May not be null.</param>
        /// <param name="value">The object to serialize.</param>
        /// <param name="options">Options to use for serialization.</param>
        public static void Serialize(TextWriter writer, object value, DigaJsonOptions options = null)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            options = options ?? new DigaJsonOptions();
            var jsonp = options.JsonPCallback.Nullify();
            if (jsonp != null)
            {
                writer.Write(options.JsonPCallback);
                writer.Write('(');
            }

            WriteValue(writer, value, new Dictionary<object, object>(ReferenceComparer._current), options);
            if (jsonp != null)
            {
                writer.Write(')');
                writer.Write(';');
            }
        }

        /// <summary>
        /// Deserializes an object from the specified text.
        /// </summary>
        /// <param name="text">The text text.</param>
        /// <param name="targetType">The required target type.</param>
        /// <param name="options">Options to use for deserialization.</param>
        /// <returns>
        /// An instance of an object representing the input data.
        /// </returns>
        private static object Deserialize(string text, Type targetType = null, DigaJsonOptions options = null)
        {
            if (text == null)
            {
                if (targetType == null)
                    return null;

                if (!targetType.IsValueType)
                    return null;

                return CreateInstance(null, targetType, 0, options, null);
            }

            using (var reader = new StringReader(text))
            {
                return Deserialize(reader, targetType, options);

            }
            
        }

        /// <summary>
        /// Deserializes an object from the specified TextReader.
        /// </summary>
        /// <typeparam name="T">The target type.</typeparam>
        /// <param name="reader">The input reader. May not be null.</param>
        /// <param name="options">Options to use for deserialization.</param>
        /// <returns>
        /// An instance of an object representing the input data.
        /// </returns>
        public static T Deserialize<T>(TextReader reader, DigaJsonOptions options = null)
        {
            return (T)Deserialize(reader, typeof(T), options);
        }

        /// <summary>
        /// Deserializes an object from the specified TextReader.
        /// </summary>
        /// <typeparam name="T">The target type.</typeparam>
        /// <param name="text">The text to deserialize.</param>
        /// <param name="options">Options to use for deserialization.</param>
        /// <returns>
        /// An instance of an object representing the input data.
        /// </returns>
        public static T Deserialize<T>(string text, DigaJsonOptions options = null)
        {
            return (T)Deserialize(text, typeof(T), options);
        }

        /// <summary>
        /// Deserializes an object from the specified TextReader.
        /// </summary>
        /// <param name="reader">The input reader. May not be null.</param>
        /// <param name="targetType">The required target type.</param>
        /// <param name="options">Options to use for deserialization.</param>
        /// <returns>
        /// An instance of an object representing the input data.
        /// </returns>
        private static object Deserialize(TextReader reader, Type targetType = null, DigaJsonOptions options = null)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            options = options ?? new DigaJsonOptions();
            if (targetType == null || targetType == typeof(object))
                return ReadValue(reader, options);

            var value = ReadValue(reader, options);
            if (value == null)
            {
                if (targetType.IsValueType)
                    return CreateInstance(null, targetType, 0, options, null);

                return null;
            }

            return ChangeType(null, value, targetType, options);
        }

        /// <summary>
        /// Deserializes data from the specified text and populates a specified object instance.
        /// </summary>
        /// <param name="text">The text to deserialize.</param>
        /// <param name="target">The object instance to populate.</param>
        /// <param name="options">Options to use for deserialization.</param>
        public static void DeserializeToTarget(string text, object target, DigaJsonOptions options = null)
        {
            if (text == null)
                return;

            using (var reader = new StringReader(text))
            {
                DeserializeToTarget(reader, target, options);
            }
            
        }

        /// <summary>
        /// Deserializes data from the specified TextReader and populates a specified object instance.
        /// </summary>
        /// <param name="reader">The input reader. May not be null.</param>
        /// <param name="target">The object instance to populate.</param>
        /// <param name="options">Options to use for deserialization.</param>
        public static void DeserializeToTarget(TextReader reader, object target, DigaJsonOptions options = null)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            if (target == null)
                throw new ArgumentNullException(nameof(target));

            var value = ReadValue(reader, options);
            Apply(value, target, options);
        }

        /// <summary>
        /// Applies the content of an array or dictionary to a target object.
        /// </summary>
        /// <param name="input">The input object.</param>
        /// <param name="target">The target object.</param>
        /// <param name="options">Options to use.</param>
        public static void Apply(object input, object target, DigaJsonOptions options = null)
        {
            options = options ?? new DigaJsonOptions();
            if (target is Array array && !array.IsReadOnly)
            {
                Apply(input as IEnumerable, array, options);
                return;
            }

            if (input is IDictionary dic)
            {
                Apply(dic, target, options);
                return;
            }

            if (target == null) return;
            var lo = GetListObject(target.GetType(), options, target, input, null, null);
            if (lo == null) return;
            lo.List = target;
            ApplyToListTarget(target, input as IEnumerable, lo, options);
        }

        internal static object CreateInstance(object target, Type type, int elementsCount, DigaJsonOptions options, object value)
        {
            try
            {
                if (options.CreateInstanceCallback != null)
                {
                    var og = new Dictionary<object, object>
                    {
                        ["elementsCount"] = elementsCount,
                        ["value"] = value
                    };

                    var e = new DigaJsonEventArgs(null, type, og, options, null, target)
                    {
                        EventType = DigaJsonEventType.CreateInstance
                    };
                    options.CreateInstanceCallback(e);
                    if (e.Handled)
                        return e.Value;
                }

                if (type.IsArray)
                {
                    var elementType = type.GetElementType();
                    if (elementType != null)
                        return Array.CreateInstance(elementType, elementsCount);
                }
                return Activator.CreateInstance(type);
            }
            catch (Exception e)
            {
                HandleException(new DigaJsonException("JSO0001: JSON error detected. Cannot create an instance of the '" + type.Name + "' type.", e), options);
                return null;
            }
        }

        internal static DigaJsonListObject GetListObject(Type type, DigaJsonOptions options, object target, object value, IDictionary dictionary, string key)
        {
            if (options.GetListObjectCallback != null)
            {
                var og = new Dictionary<object, object>
                {
                    ["dictionary"] = dictionary,
                    ["type"] = type
                };

                var e = new DigaJsonEventArgs(null, value, og, options, key, target)
                {
                    EventType = DigaJsonEventType.GetListObject
                };
                options.GetListObjectCallback(e);
                if (e.Handled)
                {
                    og.TryGetValue("type", out var outType);
                    return outType as DigaJsonListObject;
                }
            }

            if (type == typeof(byte[]))
                return null;

            if (typeof(IList).IsAssignableFrom(type))
                return new IListObject(); // also handles arrays

            if (type != null && type.IsGenericType)
            {
                if (type.GetGenericTypeDefinition() == typeof(ICollection<>))
                    return Activator.CreateInstance(typeof(ICollectionTObject<>).MakeGenericType(type.GetGenericArguments()[0])) as DigaJsonListObject;
            }

            if (type != null)
                foreach (var iface in type.GetInterfaces())
                {
                    if (!iface.IsGenericType)
                        continue;

                    if (iface.GetGenericTypeDefinition() == typeof(ICollection<>))
                        return (DigaJsonListObject)Activator.CreateInstance(
                            typeof(ICollectionTObject<>).MakeGenericType(iface.GetGenericArguments()[0]));
                }

            return null;
        }



        internal static void ApplyToListTarget(object target, IEnumerable input, DigaJsonListObject list, DigaJsonOptions options)
        {
            if (list.List == null)
                return;

            if (list.Context != null)
            {
                list.Context["action"] = "init";
                list.Context["target"] = target;
                list.Context["input"] = input;
                list.Context["options"] = options;
            }

            if (input != null)
            {
                var array = list.List as Array;
                var max = 0;
                var i = 0;
                if (array != null)
                {
                    i = array.GetLowerBound(0);
                    max = array.GetUpperBound(0);
                }

                var itemType = GetItemType(list.List.GetType());
                foreach (var value in input)
                {
                    if (array != null)
                    {
                        if (i - 1 == max)
                            break;

                        array.SetValue(ChangeType(target, value, itemType, options), i++);
                    }
                    else
                    {
                        var cvalue = ChangeType(target, value, itemType, options);
                        if (list.Context != null)
                        {
                            list.Context["action"] = "add";
                            list.Context["itemType"] = itemType;
                            list.Context["value"] = value;
                            list.Context["cvalue"] = cvalue;

                            if (!list.Context.TryGetValue("cvalue", out var newcvalue))
                                continue;

                            cvalue = newcvalue;
                        }

                        list.Add(cvalue, options);
                    }
                }
            }
            else
            {
                if (list.Context != null)
                {
                    list.Context["action"] = "clear";
                }
                list.Clear();
            }

            if (list.Context != null)
            {
                list.Context.Clear();
            }
        }

        internal static void Apply(IEnumerable input, Array target, DigaJsonOptions options)
        {
            if (target == null || target.Rank != 1)
                return;

            var elementType = target.GetType().GetElementType();
            var i = 0;
            if (input != null)
            {
                foreach (var value in input)
                {
                    target.SetValue(ChangeType(target, value, elementType, options), i++);
                }
            }
            else
            {
                Array.Clear(target, 0, target.Length);
            }
        }

        internal static bool AreValuesEqual(object o1, object o2)
        {
            if (ReferenceEquals(o1, o2))
                return true;
            var o1IsNull = (o1 == null);
            return !o1IsNull && o1.Equals(o2);
        }

        internal static bool TryGetObjectDefaultValue(Attribute att, out object value)
        {
            if (att is DigaJsonAttribute jsa && jsa.HasDefaultValue)
            {
                value = jsa.DefaultValue;
                return true;
            }

            if (att is DefaultValueAttribute dva)
            {
                value = dva.Value;
                return true;
            }

            value = null;
            return false;
        }

        internal static string GetObjectName(Attribute att)
        {
            if (att is DigaJsonAttribute jsa && !string.IsNullOrEmpty(jsa.Name))
                return jsa.Name;

            if (att is XmlAttributeAttribute xaa && !string.IsNullOrEmpty(xaa.AttributeName))
                return xaa.AttributeName;

            if (att is XmlElementAttribute xea && !string.IsNullOrEmpty(xea.ElementName))
                return xea.ElementName;

            return null;
        }

        internal static bool TryGetObjectDefaultValue(MemberInfo mi, out object value)
        {
            var atts = mi.GetCustomAttributes(true);
            foreach (var att in atts.Cast<Attribute>())
            {
                if (TryGetObjectDefaultValue(att, out value))
                    return true;
            }
            value = null;
            return false;
        }

        internal static string GetObjectName(MemberInfo mi, string defaultName)
        {
            var atts = mi.GetCustomAttributes(true);
            foreach (var att in atts.Cast<Attribute>())
            {
                var name = GetObjectName(att);
                if (name != null)
                    return name;
            }
            return defaultName;
        }

        internal static bool TryGetObjectDefaultValue(PropertyDescriptor pd, out object value)
        {
            foreach (var att in pd.Attributes.Cast<Attribute>())
            {
                if (TryGetObjectDefaultValue(att, out value))
                    return true;
            }

            value = null;
            return false;
        }

        internal static string GetObjectName(PropertyDescriptor pd, string defaultName)
        {
            foreach (var att in pd.Attributes.Cast<Attribute>())
            {
                var name = GetObjectName(att);
                if (name != null)
                    return name;
            }
            return defaultName;
        }

        internal static bool HasScriptIgnore(PropertyDescriptor pd)
        {
            foreach (var att in pd.Attributes)
            {
                if (att.GetType().Name.StartsWith(_scriptIgnore))
                    return true;
            }
            return false;
        }

        internal static bool HasScriptIgnore(MemberInfo mi)
        {
            var atts = mi.GetCustomAttributes(true);

            if (atts.Length == 0)
                return false;

            foreach (var obj in atts)
            {

                if (!(obj is Attribute att))
                    continue;

                if (att.GetType().Name.StartsWith(_scriptIgnore))
                    return true;
            }
            return false;
        }

        private static void Apply(IDictionary dictionary, object target, DigaJsonOptions options)
        {
            if (dictionary == null || target == null)
                return;

            if (target is IDictionary dicTarget)
            {
                var itemType = GetItemType(dicTarget.GetType());
                foreach (DictionaryEntry entry in dictionary)
                {
                    if (itemType == typeof(object))
                    {
                        dicTarget[entry.Key] = entry.Value;
                    }
                    else
                    {
                        dicTarget[entry.Key] = ChangeType(target, entry.Value, itemType, options);
                    }
                }
                return;
            }

            var def = TypeDef.Get(target.GetType(), options);

            foreach (DictionaryEntry entry in dictionary)
            {
                var entryKey = string.Format(CultureInfo.InvariantCulture, "{0}", entry.Key);
                var entryValue = entry.Value;
                if (options.MapEntryCallback != null)
                {
                    var og = new Dictionary<object, object>
                    {
                        ["dictionary"] = dictionary
                    };

                    var e = new DigaJsonEventArgs(null, entryValue, og, options, entryKey, target)
                    {
                        EventType = DigaJsonEventType.MapEntry
                    };
                    options.MapEntryCallback(e);
                    if (e.Handled)
                        continue;

                    entryKey = e.Name;
                    entryValue = e.Value;
                }

                def.ApplyEntry(dictionary, target, entryKey, entryValue, options);
            }
        }

        internal static DigaJsonAttribute GetJsonAttribute(MemberInfo pi)
        {
            var attributes = pi.GetCustomAttributes(true);
            if (attributes.Length == 0)
                return null;

            foreach (var obj in attributes)
            {

                if (!(obj is Attribute att))
                    continue;

                if (att is DigaJsonAttribute jAttributes)
                    return jAttributes;

            }
            return null;
        }

        /// <summary>
        /// Gets the type of elements in a collection type.
        /// </summary>
        /// <param name="collectionType">The collection type.</param>
        /// <returns>The element type or typeof(object) if it was not determined.</returns>
        private static Type GetItemType(Type collectionType)
        {
            if (collectionType == null)
                throw new ArgumentNullException(nameof(collectionType));

            foreach (var iface in collectionType.GetInterfaces())
            {
                if (!iface.IsGenericType)
                    continue;

                if (iface.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                    return iface.GetGenericArguments()[1];

                if (iface.GetGenericTypeDefinition() == typeof(IList<>))
                    return iface.GetGenericArguments()[0];

                if (iface.GetGenericTypeDefinition() == typeof(ICollection<>))
                    return iface.GetGenericArguments()[0];

                if (iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    return iface.GetGenericArguments()[0];
            }
            return typeof(object);
        }

        /// <summary>
        /// Returns a System.Object with a specified type and whose value is equivalent to a specified input object.
        /// If an error occurs, a computed default value of the target type will be returned.
        /// </summary>
        /// <param name="value">The input object. May be null.</param>
        /// <param name="conversionType">The target type. May not be null.</param>
        /// <param name="options">The options to use.</param>
        /// <returns>
        /// An object of the target type whose value is equivalent to input value.
        /// </returns>
        internal static object ChangeType(object value, Type conversionType, DigaJsonOptions options)
        {
            return ChangeType(null, value, conversionType, options);
        }

        /// <summary>
        /// Returns a System.Object with a specified type and whose value is equivalent to a specified input object.
        /// If an error occurs, a computed default value of the target type will be returned.
        /// </summary>
        /// <param name="target">The target. May be null.</param>
        /// <param name="value">The input object. May be null.</param>
        /// <param name="conversionType">The target type. May not be null.</param>
        /// <param name="options">The options to use.</param>
        /// <returns>
        /// An object of the target type whose value is equivalent to input value.
        /// </returns>
        public static object ChangeType(object target, object value, Type conversionType, DigaJsonOptions options = null)
        {
            if (conversionType == null)
                throw new ArgumentNullException(nameof(conversionType));

            if (conversionType == typeof(object))
                return value;

            options = options ?? new DigaJsonOptions();
            if (!(value is string))
            {
                if (conversionType.IsArray)
                {
                    if (value is IEnumerable en)
                    {
                        var elementType = conversionType.GetElementType();
                        var list = new List<object>();
                        foreach (var obj in en)
                        {
                            list.Add(ChangeType(target, obj, elementType, options));
                        }

                        if (elementType != null)
                        {
                            var array = Array.CreateInstance(elementType, list.Count);
                            Array.Copy(list.ToArray(), array, list.Count);
                            return array;
                        }
                    }
                }

                var lo = GetListObject(conversionType, options, target, value, null, null);
                if (lo != null)
                {
                    if (value is IEnumerable en)
                    {
                        lo.List = CreateInstance(target, conversionType, en is ICollection coll ? coll.Count : 0, options, value);
                        ApplyToListTarget(target, en, lo, options);
                        return lo.List;
                    }
                }
            }

            if (value is IDictionary dic)
            {
                var instance = CreateInstance(target, conversionType, 0, options, value);
                if (instance != null)
                {
                    Apply(dic, instance, options);
                }
                return instance;
            }

            if (conversionType == typeof(byte[]) && value is string str)
            {
                if (options.SerializationOptions.HasFlag(DigaJsonSerializationOptions.ByteArrayAsBase64))
                {
                    try
                    {
                        return Convert.FromBase64String(str);
                    }
                    catch (Exception e)
                    {
                        HandleException(new DigaJsonException("JSO0013: JSON deserialization error with a base64 array as string.", e), options);
                        return null;
                    }
                }
            }

            if (conversionType == typeof(DateTime))
            {
                if (value is DateTime)
                    return value;

                var svalue = string.Format(CultureInfo.InvariantCulture, "{0}", value);
                if (!string.IsNullOrEmpty(svalue))
                {
                    if (TryParseDateTime(svalue, options.DateTimeStyles, out var dt))
                        return dt;
                }
            }

            if (conversionType == typeof(TimeSpan))
            {
                var svalue = string.Format(CultureInfo.InvariantCulture, "{0}", value);
                if (!string.IsNullOrEmpty(svalue))
                {
                    if (long.TryParse(svalue, out var ticks))
                        return new TimeSpan(ticks);
                }
            }

            return Conversions.ChangeType(value, conversionType, null, null);
        }

        private static object[] ReadArray(TextReader reader, DigaJsonOptions options)
        {
            if (!ReadWhitespaces(reader))
                return null;

            reader.Read();
            var list = new List<object>();
            do
            {
                var value = ReadValue(reader, options, true, out var arrayEnd);
                if (!Convert.IsDBNull(value))
                {
                    list.Add(value);
                }
                if (arrayEnd)
                    return list.ToArray();

                if (reader.Peek() < 0)
                {
                    HandleException(GetExpectedCharacterException(GetPosition(reader), ']'), options);
                    return list.ToArray();
                }

            }
            while (true);
        }

        private static DigaJsonException GetExpectedCharacterException(long? pos, char c)
        {
            if (pos < 0)
                return new DigaJsonException("JSO0002: JSON deserialization error detected. Expecting '" + c + "' character.");

            return new DigaJsonException("JSO0003: JSON deserialization error detected at position " + pos + ". Expecting '" + c + "' character.");
        }

        private static DigaJsonException GetUnexpectedCharacterException(long? pos, char c)
        {
            if (pos < 0)
                return new DigaJsonException("JSO0004: JSON deserialization error detected. Unexpected '" + c + "' character.");

            return new DigaJsonException("JSO0005: JSON deserialization error detected at position " + pos + ". Unexpected '" + c + "' character.");
        }

        private static DigaJsonException GetExpectedHexaCharacterException(long? pos)
        {
            if (pos < 0)
                return new DigaJsonException("JSO0006: JSON deserialization error detected. Expecting hexadecimal character.");

            return new DigaJsonException("JSO0007: JSON deserialization error detected at position " + pos + ". Expecting hexadecimal character.");
        }

        private static DigaJsonException GetTypeException(long? pos, string typeName, Exception inner)
        {
            if (pos < 0)
                return new DigaJsonException("JSO0010: JSON deserialization error detected for '" + typeName + "' type.", inner);

            return new DigaJsonException("JSO0011: JSON deserialization error detected for '" + typeName + "' type at position " + pos + ".", inner);
        }

        private static DigaJsonException GetEofException(char c)
        {
            return new DigaJsonException("JSO0012: JSON deserialization error detected at end of text. Expecting '" + c + "' character.");
        }

        private static long? GetPosition(TextReader reader)
        {
            if (reader == null)
                return -1;

            if (reader is StreamReader sr)
            {
                try
                {
                    return sr.BaseStream.Position;
                }
                catch
                {
                    return -1;
                }
            }

            if (reader is StringReader str)
            {
                var fi = typeof(StringReader).GetField("_pos", BindingFlags.Instance | BindingFlags.NonPublic);
                if (fi != null)
                {
                    object v = fi.GetValue(str);


                    return (int?)v;
                }
            }
            return -1;
        }

        private static Dictionary<string, object> ReadDictionary(TextReader reader, DigaJsonOptions options)
        {
            if (!ReadWhitespaces(reader))
                return null;

            reader.Read();
            var dictionary = new Dictionary<string, object>();
            do
            {
                var i = reader.Peek();
                if (i < 0)
                {
                    HandleException(GetEofException('}'), options);
                    return dictionary;
                }

                var c = (char)reader.Read();
                switch (c)
                {
                    case '}':
                        return dictionary;

                    case '"':
                        var text = ReadString(reader, options);
                        if (!ReadWhitespaces(reader))
                        {
                            HandleException(GetExpectedCharacterException(GetPosition(reader), ':'), options);
                            return dictionary;
                        }

                        c = (char)reader.Peek();
                        if (c != ':')
                        {
                            HandleException(GetExpectedCharacterException(GetPosition(reader), ':'), options);
                            return dictionary;
                        }

                        reader.Read();
                        dictionary[text] = ReadValue(reader, options);
                        break;

                    case ',':
                        break;

                    case '\r':
                    case '\n':
                    case '\t':
                    case ' ':
                        break;

                    default:
                        HandleException(GetUnexpectedCharacterException(GetPosition(reader), c), options);
                        return dictionary;
                }
            }
            while (true);
        }

        private static string ReadString(TextReader reader, DigaJsonOptions options)
        {
            var sb = new StringBuilder();
            do
            {
                var i = reader.Peek();
                if (i < 0)
                {
                    HandleException(GetEofException('"'), options);
                    return null;
                }

                var c = (char)reader.Read();
                if (c == '"')
                    break;

                if (c == '\\')
                {
                    i = reader.Peek();
                    if (i < 0)
                    {
                        HandleException(GetEofException('"'), options);
                        return null;
                    }

                    var next = (char)reader.Read();
                    switch (next)
                    {
                        case 'b':
                            sb.Append('\b');
                            break;

                        case 't':
                            sb.Append('\t');
                            break;

                        case 'n':
                            sb.Append('\n');
                            break;

                        case 'f':
                            sb.Append('\f');
                            break;

                        case 'r':
                            sb.Append('\r');
                            break;

                        case '/':
                        case '\\':
                        case '"':
                            sb.Append(next);
                            break;

                        case 'u': // unicode
                            var us = ReadX4(reader, options);
                            sb.Append((char)us);
                            break;

                        default:
                            sb.Append(c);
                            sb.Append(next);
                            break;
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            while (true);
            return sb.ToString();
        }

        private static ISerializable ReadSerializable(TextReader reader, DigaJsonOptions options, string typeName, Dictionary<string, object> values)
        {
            Type type;
            try
            {
                type = Type.GetType(typeName, true);
            }
            catch (Exception e)
            {
                HandleException(GetTypeException(GetPosition(reader), typeName, e), options);
                return null;
            }

            var ctor = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(SerializationInfo), typeof(StreamingContext) }, null);
            var info = new SerializationInfo(type, _defaultFormatterConverter);

            foreach (var kvp in values)
            {
                info.AddValue(kvp.Key, kvp.Value);
            }

            var ctx = new StreamingContext(StreamingContextStates.Remoting, null);
            try
            {
                if (ctor != null)
                    return (ISerializable)ctor.Invoke(new object[] { info, ctx });
                return null;
            }
            catch (Exception e)
            {
                HandleException(GetTypeException(GetPosition(reader), typeName, e), options);
                return null;
            }
        }

        private static object ReadValue(TextReader reader, DigaJsonOptions options)
        {
            return ReadValue(reader, options, false, out _);
        }

        private static object ReadValue(TextReader reader, DigaJsonOptions options, bool arrayMode, out bool arrayEnd)
        {
            arrayEnd = false;
            // 1st chance type is determined by format
            int i;
            do
            {
                i = reader.Peek();
                if (i < 0)
                    return null;

                if (i == 10 || i == 13 || i == 9 || i == 32)
                {
                    reader.Read();
                }
                else
                    break;
            }
            while (true);

            var c = (char)i;
            if (c == '"')
            {
                reader.Read();
                var s = ReadString(reader, options);
                if (options.SerializationOptions.HasFlag(DigaJsonSerializationOptions.AutoParseDateTime))
                {
                    if (TryParseDateTime(s, options.DateTimeStyles, out var dt))
                        return dt;
                }
                return s;
            }

            if (c == '{')
            {
                var dic = ReadDictionary(reader, options);
                if (options.SerializationOptions.HasFlag(DigaJsonSerializationOptions.UseISerializable))
                {
                    if (dic.TryGetValue(_serializationTypeToken, out var o))
                    {
                        var typeName = string.Format(CultureInfo.InvariantCulture, "{0}", o);
                        if (!string.IsNullOrEmpty(typeName))
                        {
                            dic.Remove(_serializationTypeToken);
                            return ReadSerializable(reader, options, typeName, dic);
                        }
                    }
                }
                return dic;
            }

            if (c == '[')
                return ReadArray(reader, options);

            if (c == 'n')
                return ReadNew(reader, options, out arrayEnd);

            // handles the null/true/false cases
            if (char.IsLetterOrDigit(c) || c == '.' || c == '-' || c == '+')
                return ReadNumberOrLiteral(reader, options, out arrayEnd);

            if (arrayMode && c == ']')
            {
                reader.Read();
                arrayEnd = true;
                return DBNull.Value; // marks array end
            }

            if (arrayMode && c == ',')
            {
                reader.Read();
                return DBNull.Value; // marks array end
            }

            HandleException(GetUnexpectedCharacterException(GetPosition(reader), c), options);
            return null;
        }

        private static object ReadNew(TextReader reader, DigaJsonOptions options, out bool arrayEnd)
        {
            arrayEnd = false;
            var sb = new StringBuilder();
            do
            {
                var i = reader.Peek();
                if (i < 0)
                    break;

                if ((char)i == '}')
                    break;

                var c = (char)reader.Read();
                if (c == ',')
                    break;

                if (c == ']')
                {
                    arrayEnd = true;
                    break;
                }

                sb.Append(c);
            }
            while (true);

            var text = sb.ToString();
            if (string.Compare(_null, text.Trim(), StringComparison.OrdinalIgnoreCase) == 0)
                return null;

            if (text.StartsWith(_dateStartJs) && text.EndsWith(_dateEndJs))
            {
                string t = text.Substring(_dateStartJs.Length, text.Length - _dateStartJs.Length - _dateEndJs.Length);
                if (long.TryParse(t, out var l))
                    return new DateTime(l * 10000 + _minDateTimeTicks, DateTimeKind.Utc);
            }

            HandleException(GetUnexpectedCharacterException(GetPosition(reader), text[0]), options);
            return null;
        }

        private static object ReadNumberOrLiteral(TextReader reader, DigaJsonOptions options, out bool arrayEnd)
        {
            arrayEnd = false;
            var sb = new StringBuilder();
            do
            {
                var i = reader.Peek();
                if (i < 0)
                    break;

                if ((char)i == '}')
                    break;

                var c = (char)reader.Read();
                if (char.IsWhiteSpace(c) || c == ',')
                    break;

                if (c == ']')
                {
                    arrayEnd = true;
                    break;
                }

                sb.Append(c);
            }
            while (true);

            var text = sb.ToString();
            if (string.Compare(_null, text, StringComparison.OrdinalIgnoreCase) == 0)
                return null;

            if (string.Compare(_true, text, StringComparison.OrdinalIgnoreCase) == 0)
                return true;

            if (string.Compare(_false, text, StringComparison.OrdinalIgnoreCase) == 0)
                return false;

            if (text.LastIndexOf("e", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                    return d;
            }
            else
            {

                if (text.Contains('.'))
                {
                    if (decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var de))
                        return de;
                }
                else
                {
                    if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
                        return i;

                    if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l))
                        return l;

                    if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var de))
                        return de;
                }
            }

            HandleException(GetUnexpectedCharacterException(GetPosition(reader), text[0]), options);
            return null;
        }

        /// <summary>
        /// Converts the JSON string representation of a date time to its DateTime equivalent.
        /// </summary>
        /// <param name="text">The input text.</param>
        /// <returns>A DateTime value if the text was converted successfully; otherwise, null.</returns>
        public static DateTime? TryParseDateTime(string text)
        {
            if (!TryParseDateTime(text, out var dt))
                return null;

            return dt;
        }

        /// <summary>
        /// Converts the JSON string representation of a date time to its DateTime equivalent.
        /// </summary>
        /// <param name="text">The input text.</param>
        /// <param name="styles">The styles to use.</param>
        /// <returns>A DateTime value if the text was converted successfully; otherwise, null.</returns>
        public static DateTime? TryParseDateTime(string text, DateTimeStyles styles)
        {
            if (!TryParseDateTime(text, styles, out var dt))
                return null;

            return dt;
        }

        /// <summary>
        /// Converts the JSON string representation of a date time to its DateTime equivalent.
        /// </summary>
        /// <param name="text">The input text.</param>
        /// <param name="dt">When this method returns, contains the DateTime equivalent.</param>
        /// <returns>true if the text was converted successfully; otherwise, false.</returns>
        private static bool TryParseDateTime(string text, out DateTime dt)
        {
            return TryParseDateTime(text, DigaJsonOptions._defaultDateTimeStyles, out dt);
        }

        /// <summary>
        /// Converts the JSON string representation of a date time to its DateTime equivalent.
        /// </summary>
        /// <param name="text">The input text.</param>
        /// <param name="styles">The styles to use.</param>
        /// <param name="dt">When this method returns, contains the DateTime equivalent.</param>
        /// <returns>
        /// true if the text was converted successfully; otherwise, false.
        /// </returns>
        private static bool TryParseDateTime(string text, DateTimeStyles styles, out DateTime dt)
        {
            dt = DateTime.MinValue;
            if (text == null)
                return false;

            if (text.Length > 2)
            {
                if (text[0] == '"' && text[text.Length - 1] == '"')
                {
                    using (var reader = new StringReader(text))
                    {
                        reader.Read(); // skip "
                        var options = new DigaJsonOptions
                        {
                            ThrowExceptions = false
                        };
                        text = ReadString(reader, options);
                    }
                }
            }

            if (text.EndsWith("Z", StringComparison.OrdinalIgnoreCase))
            {
                if (DateTime.TryParseExact(text, _dateFormatsUtc, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out dt))
                    return true;
            }

            var offsetHours = 0;
            var offsetMinutes = 0;
            var kind = DateTimeKind.Utc;
            const int len = 19;

            // s format length is 19, as in '2012-02-21T17:07:14'
            // so we can do quick checks
            // this portion of code is needed because we assume UTC and the default DateTime parse behavior is not that (even with AssumeUniversal)
            if (text.Length >= len &&
                text[4] == '-' &&
                text[7] == '-' &&
                (text[10] == 'T' || text[10] == 't') &&
                text[13] == ':' &&
                text[16] == ':')
            {
                if (DateTime.TryParseExact(text, "o", null, DateTimeStyles.AssumeUniversal, out dt))
                    return true;

                var tz = text.Substring(len).IndexOfAny(new[] { '+', '-' });
                var text2 = text;
                if (tz >= 0)
                {
                    tz += len;
                    var offset = text.Substring(tz + 1).Trim();
                    if (int.TryParse(offset, out int i))
                    {
                        kind = DateTimeKind.Local;
                        offsetHours = i / 100;
                        offsetMinutes = i % 100;
                        if (text[tz] == '-')
                        {
                            offsetHours = -offsetHours;
                            offsetMinutes = -offsetMinutes;
                        }
                        text2 = text.Substring(0, tz);
                    }
                }

                if (tz >= 0)
                {
                    if (DateTime.TryParseExact(text2, "s", null, DateTimeStyles.AssumeLocal, out dt))
                    {
                        if (offsetHours != 0)
                        {
                            dt = dt.AddHours(offsetHours);
                        }

                        if (offsetMinutes != 0)
                        {
                            dt = dt.AddMinutes(offsetMinutes);
                        }
                        return true;
                    }
                }
                else
                {
                    if (DateTime.TryParseExact(text, "s", null, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out dt))
                        return true;
                }
            }

            // 01234567890123456
            // 20150525T15:50:00
            if (text.Length == 17)
            {
                if ((text[8] == 'T' || text[8] == 't') && text[11] == ':' && text[14] == ':')
                {
                    _ = int.TryParse(text.Substring(0, 4), out var year);
                    _ = int.TryParse(text.Substring(4, 2), out var month);
                    _ = int.TryParse(text.Substring(6, 2), out var day);
                    _ = int.TryParse(text.Substring(9, 2), out var hour);
                    _ = int.TryParse(text.Substring(12, 2), out var minute);
                    _ = int.TryParse(text.Substring(15, 2), out var second);
                    if (month > 0 && month < 13 &&
                        day > 0 && day < 32 &&
                        year >= 0 &&
                        hour >= 0 && hour < 24 &&
                        minute >= 0 && minute < 60 &&
                        second >= 0 && second < 60)
                    {
                        try
                        {
                            dt = new DateTime(year, month, day, hour, minute, second);
                            return true;
                        }
                        catch
                        {
                            // do nothing
                        }
                    }
                }
            }

            // read this http://weblogs.asp.net/bleroy/archive/2008/01/18/dates-and-json.aspx
            string ticks = null;
            if (text.StartsWith(_dateStartJs) && text.EndsWith(_dateEndJs))
            {
                ticks = text.Substring(_dateStartJs.Length, text.Length - _dateStartJs.Length - _dateEndJs.Length).Trim();
            }
            else if (text.StartsWith(_dateStart2, StringComparison.OrdinalIgnoreCase) && text.EndsWith(_dateEnd2, StringComparison.OrdinalIgnoreCase))
            {
                ticks = text.Substring(_dateStart2.Length, text.Length - _dateEnd2.Length - _dateStart2.Length).Trim();
            }

            if (!string.IsNullOrEmpty(ticks))
            {
                var startIndex = (ticks[0] == '-') || (ticks[0] == '+') ? 1 : 0;
                var pos = ticks.IndexOfAny(new[] { '+', '-' }, startIndex);
                if (pos >= 0)
                {
                    var neg = ticks[pos] == '-';
                    var offset = ticks.Substring(pos + 1).Trim();
                    ticks = ticks.Substring(0, pos).Trim();
                    if (int.TryParse(offset, out var i))
                    {
                        kind = DateTimeKind.Local;
                        offsetHours = i / 100;
                        offsetMinutes = i % 100;
                        if (neg)
                        {
                            offsetHours = -offsetHours;
                            offsetMinutes = -offsetMinutes;
                        }
                    }
                }

                if (long.TryParse(ticks, NumberStyles.Number, CultureInfo.InvariantCulture, out var l))
                {
                    dt = new DateTime((l * 10000) + _minDateTimeTicks, kind);
                    if (offsetHours != 0)
                    {
                        dt = dt.AddHours(offsetHours);
                    }

                    if (offsetMinutes != 0)
                    {
                        dt = dt.AddMinutes(offsetMinutes);
                    }
                    return true;
                }
            }

            // don't parse pure timespan style XX:YY:ZZ
            if (text.Length == 8 && text[2] == ':' && text[5] == ':')
            {
                dt = DateTime.MinValue;
                return false;
            }

            return DateTime.TryParse(text, null, styles, out dt);
        }

        internal static void HandleException(Exception ex, DigaJsonOptions options)
        {
            if (options != null && !options.ThrowExceptions)
            {
                options.AddException(ex);
                return;
            }
            throw ex;
        }

        private static byte GetHexValue(TextReader reader, char c, DigaJsonOptions options)
        {
            c = char.ToLower(c);
            if (c < '0')
            {
                HandleException(GetExpectedHexaCharacterException(GetPosition(reader)), options);
                return 0;
            }

            if (c <= '9')
                return (byte)(c - '0');

            if (c < 'a')
            {
                HandleException(GetExpectedHexaCharacterException(GetPosition(reader)), options);
                return 0;
            }

            if (c <= 'f')
                return (byte)(c - 'a' + 10);

            HandleException(GetExpectedHexaCharacterException(GetPosition(reader)), options);
            return 0;
        }

        private static ushort ReadX4(TextReader reader, DigaJsonOptions options)
        {
            var u = 0;
            for (var i = 0; i < 4; i++)
            {
                u *= 16;
                if (reader.Peek() < 0)
                {
                    HandleException(new DigaJsonException("JSO0008: JSON deserialization error detected at end of stream. Expecting hexadecimal character."), options);
                    return 0;
                }

                u += GetHexValue(reader, (char)reader.Read(), options);
            }
            return (ushort)u;
        }

        private static bool ReadWhitespaces(TextReader reader)
        {
            return ReadWhile(reader, char.IsWhiteSpace);
        }

        private static bool ReadWhile(TextReader reader, Predicate<char> cont)
        {
            do
            {
                var i = reader.Peek();
                if (i < 0)
                    return false;

                if (!cont((char)i))
                    return true;

                reader.Read();
            }
            while (true);
        }

       


        /// <summary>
        /// Writes a value to a JSON writer.
        /// </summary>
        /// <param name="writer">The writer. May not be null.</param>
        /// <param name="value">The value to writer.</param>
        /// <param name="objectGraph">A graph of objects to track cyclic serialization.</param>
        /// <param name="options">The options to use.</param>
        public static void WriteValue(TextWriter writer, object value, IDictionary<object, object> objectGraph, DigaJsonOptions options = null)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            objectGraph = objectGraph ?? new Dictionary<object, object>();
            options = options ?? new DigaJsonOptions();
            if (options.WriteValueCallback != null)
            {
                var e = new DigaJsonEventArgs(writer, value, objectGraph, options)
                {
                    EventType = DigaJsonEventType.WriteValue
                };
                options.WriteValueCallback(e);
                if (e.Handled)
                    return;
            }

            if (value == null || Convert.IsDBNull(value))
            {
                writer.Write(_null);
                return;
            }

            if (value is string s)
            {
                WriteString(writer, s);
                return;
            }

            if (value is bool b)
            {
                writer.Write(b ? _true : _false);
                return;
            }

            if (value is float f)
            {
                if (float.IsInfinity(f) || float.IsNaN(f))
                {
                    writer.Write(_null);
                    return;
                }

                writer.Write(f.ToString(_roundTripFormat, CultureInfo.InvariantCulture));
                return;
            }

            if (value is double d)
            {
                if (double.IsInfinity(d) || double.IsNaN(d))
                {
                    writer.Write(_null);
                    return;
                }

                writer.Write(d.ToString(_roundTripFormat, CultureInfo.InvariantCulture));
                return;
            }

            if (value is char c)
            {
                if (c == '\0')
                {
                    writer.Write(_null);
                    return;
                }
                WriteString(writer, c.ToString());
                return;
            }

            if (value is Enum @enum)
            {
                if (options.SerializationOptions.HasFlag(DigaJsonSerializationOptions.EnumAsText))
                {
                    WriteString(writer, value.ToString());
                }
                else
                {
                    writer.Write(@enum.ToString(_enumFormat));
                }
                return;
            }

            if (value is TimeSpan ts)
            {
                if (options.SerializationOptions.HasFlag(DigaJsonSerializationOptions.TimeSpanAsText))
                {
                    WriteString(writer, ts.ToString("g", CultureInfo.InvariantCulture));
                }
                else
                {
                    writer.Write(ts.Ticks);
                }
                return;
            }

            if (value is DateTimeOffset dto)
            {
                if (options.SerializationOptions.HasFlag(DigaJsonSerializationOptions.DateFormatJs))
                {
                    writer.Write(_dateStartJs);
                    writer.Write((dto.ToUniversalTime().Ticks - _minDateTimeTicks) / 10000);
                    writer.Write(_dateEndJs);
                }
                else if (options.SerializationOptions.HasFlag(DigaJsonSerializationOptions.DateTimeOffsetFormatCustom) && !string.IsNullOrEmpty(options.DateTimeOffsetFormat))
                {
                    WriteString(writer, dto.ToUniversalTime().ToString(options.DateTimeOffsetFormat));
                }
                else if (options.SerializationOptions.HasFlag(DigaJsonSerializationOptions.DateFormatIso8601))
                {
                    WriteString(writer, dto.ToUniversalTime().ToString("s"));
                }
                else if (options.SerializationOptions.HasFlag(DigaJsonSerializationOptions.DateFormatRoundtripUtc))
                {
                    WriteString(writer, dto.ToUniversalTime().ToString("o"));
                }
                else
                {
                    writer.Write(_dateStart);
                    writer.Write((dto.ToUniversalTime().Ticks - _minDateTimeTicks) / 10000);
                    writer.Write(_dateEnd);
                }
                return;
            }
            // read this http://weblogs.asp.net/bleroy/archive/2008/01/18/dates-and-json.aspx

            if (value is DateTime dt)
            {
                if (options.SerializationOptions.HasFlag(DigaJsonSerializationOptions.DateFormatJs))
                {
                    writer.Write(_dateStartJs);
                    writer.Write((dt.ToUniversalTime().Ticks - _minDateTimeTicks) / 10000);
                    writer.Write(_dateEndJs);
                }
                else if (options.SerializationOptions.HasFlag(DigaJsonSerializationOptions.DateFormatCustom) && !string.IsNullOrEmpty(options.DateTimeFormat))
                {
                    WriteString(writer, dt.ToUniversalTime().ToString(options.DateTimeFormat));
                }
                else if (options.SerializationOptions.HasFlag(DigaJsonSerializationOptions.DateFormatIso8601))
                {
                    writer.Write('"');
                    writer.Write(EscapeString(dt.ToUniversalTime().ToString("s")), options);
                    AppendTimeZoneUtcOffset(writer, dt);
                    writer.Write('"');
                }
                else if (options.SerializationOptions.HasFlag(DigaJsonSerializationOptions.DateFormatRoundtripUtc))
                {
                    WriteString(writer, dt.ToUniversalTime().ToString("o"));
                }
                else
                {
                    writer.Write(_dateStart);
                    writer.Write((dt.ToUniversalTime().Ticks - _minDateTimeTicks) / 10000);
                    AppendTimeZoneUtcOffset(writer, dt);
                    writer.Write(_dateEnd);
                }
                return;
            }

            if (value is int || value is uint || value is short || value is ushort ||
                value is long || value is ulong || value is byte || value is sbyte ||
                value is decimal)
            {
                writer.Write(string.Format(CultureInfo.InvariantCulture, _zeroArg, value));
                return;
            }

            if (value is Guid guid)
            {
                if (options.GuidFormat != null)
                {
                    WriteUnescapedString(writer, guid.ToString(options.GuidFormat));
                }
                else
                {
                    WriteUnescapedString(writer, guid.ToString());
                }
                return;
            }

            var uri = value as Uri;
            if (uri != null)
            {
                WriteString(writer, uri.GetComponents(UriComponents.SerializationInfoString, UriFormat.UriEscaped));
                return;
            }

            if (value is Array array)
            {
                WriteArray(writer, array, objectGraph, options);
                return;
            }

            if (objectGraph.ContainsKey(value))
            {
                if (options.SerializationOptions.HasFlag(DigaJsonSerializationOptions.ContinueOnCycle))
                {
                    writer.Write(_null);
                    return;
                }

                HandleException(new DigaJsonException("JSO0009: Cyclic JSON serialization detected."), options);
                return;
            }

            objectGraph.Add(value, null);

            if (value is IDictionary dictionary)
            {
                WriteDictionary(writer, dictionary, objectGraph, options);
                return;
            }

            // ExpandoObject falls here
            if (TypeDef.IsKeyValuePairEnumerable(value.GetType(), out var _, out var _))
            {
                WriteDictionary(writer, new KeyValueTypeDictionary(value), objectGraph, options);
                return;
            }

            if (value is IEnumerable enumerable)
            {
                WriteEnumerable(writer, enumerable, objectGraph, options);
                return;
            }

            if (options.SerializationOptions.HasFlag(DigaJsonSerializationOptions.StreamsAsBase64))
            {
                if (value is Stream stream)
                {
                    WriteBase64Stream(writer, stream, objectGraph, options);
                    return;
                }
            }

            WriteObject(writer, value, objectGraph, options);
        }

        /// <summary>
        /// Writes a stream to a JSON writer.
        /// </summary>
        /// <param name="writer">The writer. May not be null.</param>
        /// <param name="stream">The stream. May not be null.</param>
        /// <param name="objectGraph">The object graph.</param>
        /// <param name="options">The options to use.</param>
        /// <returns>The number of written bytes.</returns>
        private static long WriteBase64Stream(TextWriter writer, Stream stream, IDictionary<object, object> objectGraph, DigaJsonOptions options = null)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            objectGraph =objectGraph ?? new Dictionary<object, object>();
            options = options ?? new DigaJsonOptions();
            var total = 0L;

            if (writer is StreamWriter sw)
            {
                sw.Flush();
                return WriteBase64Stream(stream, sw.BaseStream, options);
            }

            if (writer is IndentedTextWriter itw)
            {
                itw.Flush();
                return WriteBase64Stream(itw.InnerWriter, stream, objectGraph, options);
            }

            using (var ms = new MemoryStream())
            {
                var bytes = new byte[options.FinalStreamingBufferChunkSize];
                do
                {
                    var read = stream.Read(bytes, 0, bytes.Length);
                    if (read == 0)
                        break;

                    ms.Write(bytes, 0, read);
                    total += read;
                }
                while (true);
                writer.Write('"');
                writer.Write(Convert.ToBase64String(ms.ToArray()));
                writer.Write('"');

            }

            return total;
        }

        private static long WriteBase64Stream(Stream inputStream, Stream outputStream, DigaJsonOptions options)
        {
            outputStream.WriteByte((byte)'"');
            // don't dispose this stream or it will dispose the outputStream as well
            var b64 = new CryptoStream(outputStream, new ToBase64Transform(), CryptoStreamMode.Write);
            var total = 0L;
            var bytes = new byte[options.FinalStreamingBufferChunkSize];
            do
            {
                var read = inputStream.Read(bytes, 0, bytes.Length);
                if (read == 0)
                    break;

                b64.Write(bytes, 0, read);
                total += read;
            }
            while (true);

            b64.FlushFinalBlock();
            b64.Flush();
            outputStream.WriteByte((byte)'"');
            return total;
        }

        internal static bool InternalIsKeyValuePairEnumerable(Type type, out Type keyType, out Type valueType)
        {
            keyType = null;
            valueType = null;
            foreach (var t in type.GetInterfaces())
            {
                if (t.IsGenericType)
                {
                    if (typeof(IEnumerable<>).IsAssignableFrom(t.GetGenericTypeDefinition()))
                    {
                        var args = t.GetGenericArguments();
                        if (args.Length == 1)
                        {
                            var kvp = args[0];
                            if (kvp.IsGenericType && typeof(KeyValuePair<,>).IsAssignableFrom(kvp.GetGenericTypeDefinition()))
                            {
                                var kvpArgs = kvp.GetGenericArguments();
                                if (kvpArgs.Length == 2)
                                {
                                    keyType = kvpArgs[0];
                                    valueType = kvpArgs[1];
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        private static void AppendTimeZoneUtcOffset(TextWriter writer, DateTime dt)
        {
            if (dt.Kind != DateTimeKind.Utc)
            {
                var offset = TimeZoneInfo.Local.GetUtcOffset(dt);
                writer.Write(offset.Ticks >= 0 ? '+' : '-');
                writer.Write(Math.Abs(offset.Hours).ToString(_d2Format));
                writer.Write(Math.Abs(offset.Minutes).ToString(_d2Format));
            }
        }

        /// <summary>
        /// Writes an enumerable to a JSON writer.
        /// </summary>
        /// <param name="writer">The writer. May not be null.</param>
        /// <param name="array">The array. May not be null.</param>
        /// <param name="objectGraph">The object graph.</param>
        /// <param name="options">The options to use.</param>
        private static void WriteArray(TextWriter writer, Array array, IDictionary<object, object> objectGraph, DigaJsonOptions options = null)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            if (array == null)
                throw new ArgumentNullException(nameof(array));

            objectGraph =objectGraph ?? new Dictionary<object, object>();
            options = options ?? new DigaJsonOptions();
            if (options.SerializationOptions.HasFlag(DigaJsonSerializationOptions.ByteArrayAsBase64))
            {
                if (array is byte[] bytes)
                {
                    using (var ms = new MemoryStream(bytes))
                    {
                        ms.Position = 0;
                        WriteBase64Stream(writer, ms, objectGraph, options);

                    }
                    return;
                }
            }


            WriteArray(writer, array, objectGraph, options, Array.Empty<int>());

        }

        private static void WriteArray(TextWriter writer, Array array, IDictionary<object, object> objectGraph, DigaJsonOptions options, int[] indices)
        {
            var newIndices = new int[indices.Length + 1];
            for (var i = 0; i < indices.Length; i++)
            {
                newIndices[i] = indices[i];
            }

            writer.Write('[');
            for (var i = 0; i < array.GetLength(indices.Length); i++)
            {
                if (i > 0)
                {
                    writer.Write(',');
                }
                newIndices[indices.Length] = i;

                if (array.Rank == newIndices.Length)
                {
                    WriteValue(writer, array.GetValue(newIndices), objectGraph, options);
                }
                else
                {
                    WriteArray(writer, array, objectGraph, options, newIndices);
                }
            }
            writer.Write(']');
        }

        /// <summary>
        /// Writes an enumerable to a JSON writer.
        /// </summary>
        /// <param name="writer">The writer. May not be null.</param>
        /// <param name="enumerable">The enumerable. May not be null.</param>
        /// <param name="objectGraph">The object graph.</param>
        /// <param name="options">The options to use.</param>
        private static void WriteEnumerable(TextWriter writer, IEnumerable enumerable, IDictionary<object, object> objectGraph, DigaJsonOptions options = null)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            if (enumerable == null)
                throw new ArgumentNullException(nameof(enumerable));

            objectGraph =objectGraph ?? new Dictionary<object, object>();
            options = options ?? new DigaJsonOptions();
            writer.Write('[');
            var first = true;
            foreach (var value in enumerable)
            {
                if (!first)
                {
                    writer.Write(',');
                }
                else
                {
                    first = false;
                }
                WriteValue(writer, value, objectGraph, options);
            }
            writer.Write(']');
        }

        /// <summary>
        /// Writes a dictionary to a JSON writer.
        /// </summary>
        /// <param name="writer">The writer. May not be null.</param>
        /// <param name="dictionary">The dictionary. May not be null.</param>
        /// <param name="objectGraph">The object graph.</param>
        /// <param name="options">The options to use.</param>
        private static void WriteDictionary(TextWriter writer, IDictionary dictionary, IDictionary<object, object> objectGraph, DigaJsonOptions options = null)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary));

            objectGraph =objectGraph ?? new Dictionary<object, object>();
            options = options ?? new DigaJsonOptions();
            writer.Write('{');
            var first = true;
            foreach (DictionaryEntry entry in dictionary)
            {
                var entryKey = string.Format(CultureInfo.InvariantCulture, "{0}", entry.Key);
                if (!first)
                {
                    writer.Write(',');
                }
                else
                {
                    first = false;
                }

                if (options.SerializationOptions.HasFlag(DigaJsonSerializationOptions.WriteKeysWithoutQuotes))
                {
                    writer.Write(EscapeString(entryKey));
                }
                else
                {
                    WriteString(writer, entryKey);
                }

                writer.Write(':');
                WriteValue(writer, entry.Value, objectGraph, options);
            }
            writer.Write('}');
        }

        private static void WriteSerializable(TextWriter writer, ISerializable serializable, IDictionary<object, object> objectGraph, DigaJsonOptions options)
        {
            var info = new SerializationInfo(serializable.GetType(), _defaultFormatterConverter);
            var ctx = new StreamingContext(StreamingContextStates.Remoting, null);
            serializable.GetObjectData(info, ctx);
            info.AddValue(_serializationTypeToken, serializable.GetType().AssemblyQualifiedName);

            var first = true;
            foreach (var entry in info)
            {
                if (!first)
                {
                    writer.Write(',');
                }
                else
                {
                    first = false;
                }

                if (options.SerializationOptions.HasFlag(DigaJsonSerializationOptions.WriteKeysWithoutQuotes))
                {
                    writer.Write(EscapeString(entry.Name));
                }
                else
                {
                    WriteString(writer, entry.Name);
                }

                writer.Write(':');
                WriteValue(writer, entry.Value, objectGraph, options);
            }
        }

        private static bool ForceSerializable(object obj)
        {
            return obj is Exception;
        }

        /// <summary>
        /// Writes an object to the JSON writer.
        /// </summary>
        /// <param name="writer">The writer. May not be null.</param>
        /// <param name="value">The object to serialize. May not be null.</param>
        /// <param name="objectGraph">The object graph.</param>
        /// <param name="options">The options to use.</param>
        private static void WriteObject(TextWriter writer, object value, IDictionary<object, object> objectGraph, DigaJsonOptions options = null)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            if (value == null)
                throw new ArgumentNullException(nameof(value));

            objectGraph =objectGraph ?? new Dictionary<object, object>();
            options = options ?? new DigaJsonOptions();

            ISerializable serializable = null;
            var useISerializable = options.SerializationOptions.HasFlag(DigaJsonSerializationOptions.UseISerializable) || ForceSerializable(value);
            if (useISerializable)
            {
                serializable = value as ISerializable;
            }

            writer.Write('{');

            if (options.BeforeWriteObjectCallback != null)
            {
                var e = new DigaJsonEventArgs(writer, value, objectGraph, options)
                {
                    EventType = DigaJsonEventType.BeforeWriteObject
                };
                options.BeforeWriteObjectCallback(e);
                if (e.Handled)
                    return;
            }

            var type = value.GetType();
            if (serializable != null)
            {
                WriteSerializable(writer, serializable, objectGraph, options);
            }
            else
            {
                var def = TypeDef.Get(type, options);
                def.WriteValues(writer, value, objectGraph, options);
            }

            if (options.AfterWriteObjectCallback != null)
            {
                var e = new DigaJsonEventArgs(writer, value, objectGraph, options)
                {
                    EventType = DigaJsonEventType.AfterWriteObject
                };
                options.AfterWriteObjectCallback(e);
            }

            writer.Write('}');
        }

        /// <summary>
        /// Determines whether the specified value is a value type and is equal to zero.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>true if the specified value is a value type and is equal to zero; false otherwise.</returns>
        internal static bool IsZeroValueType(object value)
        {
            if (value == null)
                return false;

            var type = value.GetType();
            if (!type.IsValueType)
                return false;

            return value.Equals(Activator.CreateInstance(type));
        }

        /// <summary>
        /// Writes a name/value pair to a JSON writer.
        /// </summary>
        /// <param name="writer">The input writer. May not be null.</param>
        /// <param name="name">The name. null values will be converted to empty values.</param>
        /// <param name="value">The value.</param>
        /// <param name="objectGraph">The object graph.</param>
        /// <param name="options">The options to use.</param>
        internal static void WriteNameValue(TextWriter writer, string name, object value, IDictionary<object, object> objectGraph, DigaJsonOptions options = null)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            name =name ?? string.Empty;
            options = options ?? new DigaJsonOptions();
            if (options.SerializationOptions.HasFlag(DigaJsonSerializationOptions.WriteKeysWithoutQuotes))
            {
                writer.Write(EscapeString(name));
            }
            else
            {
                WriteString(writer, name);
            }

            writer.Write(':');
            WriteValue(writer, value, objectGraph, options);
        }

        /// <summary>
        /// Writes a string to a JSON writer.
        /// </summary>
        /// <param name="writer">The input writer. May not be null.</param>
        /// <param name="text">The text.</param>
        private static void WriteString(TextWriter writer, string text)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            if (text == null)
            {
                writer.Write(_null);
                return;
            }

            writer.Write('"');
            writer.Write(EscapeString(text));
            writer.Write('"');
        }

        /// <summary>
        /// Writes a string to a JSON writer.
        /// </summary>
        /// <param name="writer">The input writer. May not be null.</param>
        /// <param name="text">The text.</param>
        internal static void WriteUnescapedString(TextWriter writer, string text)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            if (text == null)
            {
                writer.Write(_null);
                return;
            }

            writer.Write('"');
            writer.Write(text);
            writer.Write('"');
        }

        private static void AppendCharAsUnicode(StringBuilder sb, char c)
        {
            sb.Append('\\');
            sb.Append('u');
            sb.AppendFormat(CultureInfo.InvariantCulture, _x4Format, (ushort)c);
        }

        /// <summary>
        /// Serializes an object with format. Note this is more for debugging purposes as it's not designed to be fast.
        /// </summary>
        /// <param name="value">The JSON object. May be null.</param>
        /// <param name="options">The options to use. May be null.</param>
        /// <returns>A string containing the formatted object.</returns>
        public static string SerializeFormatted(object value, DigaJsonOptions options = null)
        {
            using (var sw = new StringWriter())
            {
                SerializeFormatted(sw, value, options);
                return sw.ToString();

            }
        }

        /// <summary>
        /// Serializes an object with format. Note this is more for debugging purposes as it's not designed to be fast.
        /// </summary>
        /// <param name="writer">The output writer. May not be null.</param>
        /// <param name="value">The JSON object. May be null.</param>
        /// <param name="options">The options to use. May be null.</param>
        public static void SerializeFormatted(TextWriter writer, object value, DigaJsonOptions options = null)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            options = options ?? new DigaJsonOptions();
            var serialized = Serialize(value, options);
            var deserialized = Deserialize(serialized, typeof(object), options);
            WriteFormatted(writer, deserialized, options);
        }

        /// <summary>
        /// Writes a JSON deserialized object formatted.
        /// </summary>
        /// <param name="jsonObject">The JSON object. May be null.</param>
        /// <param name="options">The options to use. May be null.</param>
        /// <returns>A string containing the formatted object.</returns>
        public static string WriteFormatted(object jsonObject, DigaJsonOptions options = null)
        {
            using (var sw = new StringWriter())
            {
                WriteFormatted(sw, jsonObject, options);
                return sw.ToString();
            }
        }

        /// <summary>
        /// Writes a JSON deserialized object formatted.
        /// </summary>
        /// <param name="writer">The output writer. May not be null.</param>
        /// <param name="jsonObject">The JSON object. May be null.</param>
        /// <param name="options">The options to use. May be null.</param>
        public static void WriteFormatted(TextWriter writer, object jsonObject, DigaJsonOptions options = null)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            options = options ?? new DigaJsonOptions();
            var itw = new IndentedTextWriter(writer, options.FormattingTab);
            WriteFormatted(itw, jsonObject, options);
        }

        private static void WriteFormatted(IndentedTextWriter writer, object jsonObject, DigaJsonOptions options)
        {
            if (jsonObject is DictionaryEntry entry)
            {
                var entryKey = string.Format(CultureInfo.InvariantCulture, "{0}", entry.Key);
                if (options.SerializationOptions.HasFlag(DigaJsonSerializationOptions.WriteKeysWithoutQuotes))
                {
                    writer.Write(entryKey);
                    writer.Write(": ");
                }
                else
                {
                    writer.Write('"');
                    writer.Write(entryKey);
                    writer.Write("\": ");
                }

                writer.Indent++;
                WriteFormatted(writer, entry.Value, options);
                writer.Indent--;
                return;
            }

            if (jsonObject is IDictionary dictionary)
            {
                writer.WriteLine('{');
                var first = true;
                writer.Indent++;
                foreach (DictionaryEntry entry2 in dictionary)
                {
                    if (!first)
                    {
                        writer.WriteLine(',');
                    }
                    else
                    {
                        first = false;
                    }

                    WriteFormatted(writer, entry2, options);
                }

                writer.Indent--;
                writer.WriteLine();
                writer.Write('}');
                return;
            }

            if (jsonObject is string s)
            {
                WriteString(writer, s);
                return;
            }

            if (jsonObject is IEnumerable enumerable)
            {
                writer.WriteLine('[');
                var first = true;
                writer.Indent++;
                foreach (var obj in enumerable)
                {
                    if (!first)
                    {
                        writer.WriteLine(',');
                    }
                    else
                    {
                        first = false;
                    }

                    WriteFormatted(writer, obj, options);
                }

                writer.Indent--;
                writer.WriteLine();
                writer.Write(']');
                return;
            }

            WriteValue(writer, jsonObject, null, options);
        }

        /// <summary>
        /// Escapes a string using JSON representation.
        /// </summary>
        /// <param name="value">The string to escape.</param>
        /// <returns>A JSON-escaped string.</returns>
        public static string EscapeString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            StringBuilder builder = null;
            var startIndex = 0;
            var count = 0;
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (c == '\r' ||
                    c == '\t' ||
                    c == '"' ||
                    c == '\'' ||
                    c == '<' ||
                    c == '>' ||
                    c == '\\' ||
                    c == '\n' ||
                    c == '\b' ||
                    c == '\f' ||
                    c < ' ')
                {
                    if (builder == null)
                    {
                        builder = new StringBuilder(value.Length + 5);
                    }

                    if (count > 0)
                    {
                        builder.Append(value, startIndex, count);
                    }
                    startIndex = i + 1;
                    count = 0;
                }

                switch (c)
                {
                    case '<':
                    case '>':
                    case '\'':
                        AppendCharAsUnicode(builder, c);
                        continue;

                    case '\\':
                        if (builder != null) builder.Append(@"\\");
                        continue;

                    case '\b':
                        if (builder != null) builder.Append(@"\b");
                        continue;

                    case '\t':
                        if (builder != null) builder.Append(@"\t");
                        continue;

                    case '\n':
                        if (builder != null) builder.Append(@"\n");
                        continue;

                    case '\f':
                        if (builder != null) builder.Append(@"\f");
                        continue;

                    case '\r':
                        if (builder != null) builder.Append(@"\r");
                        continue;

                    case '"':
                        if (builder != null) builder.Append("\\\"");
                        continue;
                }

                if (c < ' ')
                {
                    AppendCharAsUnicode(builder, c);
                }
                else
                {
                    count++;
                }
            }

            if (builder == null)
                return value;

            if (count > 0)
            {
                builder.Append(value, startIndex, count);
            }
            return builder.ToString();
        }

        internal static T GetAttribute<T>(this PropertyDescriptor descriptor) where T : Attribute
        {
            return descriptor.Attributes.GetAttribute<T>();
        }

        private static T GetAttribute<T>(this AttributeCollection attributes) where T : Attribute
        {
            foreach (var att in attributes)
            {

                if (att.GetType().IsAssignableFrom(typeof(T)))
                    return (T)att;
            }
            return null;
        }

        internal static bool EqualsIgnoreCase(this string str, string text, bool trim = false)
        {
            if (trim)
            {
                str = str.Nullify();
                text = text.Nullify();
            }

            if (str == null)
                return text == null;

            if (text == null)
                return false;

            if (str.Length != text.Length)
                return false;

            return string.Compare(str, text, StringComparison.OrdinalIgnoreCase) == 0;
        }

        public static string Nullify(this string str)
        {
            if (str == null)
                return null;

            if (string.IsNullOrWhiteSpace(str))
                return null;

            var t = str.Trim();
            return t.Length == 0 ? null : t;
        }

       
        
        
        

      
    }
}