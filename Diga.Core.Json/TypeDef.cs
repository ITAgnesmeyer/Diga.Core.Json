using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;

namespace Diga.Core.Json
{
    internal class TypeDef
        {
            private static readonly Dictionary<string, TypeDef> _TypeDefs = new Dictionary<string, TypeDef>();
            private static readonly Dictionary<Type, KeyValueType> _IsKvPe = new Dictionary<Type, KeyValueType>();
            private static readonly object _lock = new object();

            private readonly List<MemberDefinition> _serializationMembers;
            private readonly List<MemberDefinition> _deserializationMembers;
            private readonly Type _type;

            private TypeDef(Type type, JsonOptions options)
            {
                this._type = type;
                IEnumerable<MemberDefinition> members;
                if (options.SerializationOptions.HasFlag(JsonSerializationOptions.UseReflection))
                {
                    members = EnumerateDefinitionsUsingReflection(true, type, options);
                }
                else
                {
                    members = EnumerateDefinitionsUsingTypeDescriptors(true, type, options);
                }

                this._serializationMembers = new List<MemberDefinition>(options.FinalizeSerializationMembers(type, members));

                if (options.SerializationOptions.HasFlag(JsonSerializationOptions.UseReflection))
                {
                    members = EnumerateDefinitionsUsingReflection(false, type, options);
                }
                else
                {
                    members = EnumerateDefinitionsUsingTypeDescriptors(false, type, options);
                }

                this._deserializationMembers = new List<MemberDefinition>(options.FinalizeDeserializationMembers(type, members));
            }

            private MemberDefinition GetDeserializationMember(string key)
            {
                if (key == null)
                    return null;

                foreach (var def in this._deserializationMembers)
                {
                    if (string.Compare(def.WireName, key, StringComparison.OrdinalIgnoreCase) == 0)
                        return def;
                }
                return null;
            }

            public void ApplyEntry(IDictionary dictionary, object target, string key, object value, JsonOptions options)
            {
                var member = GetDeserializationMember(key);
                if (member == null)
                    return;

                member.ApplyEntry(dictionary, target, key, value, options);
            }

            public void WriteValues(TextWriter writer, object component, IDictionary<object, object> objectGraph, JsonOptions options)
            {
                var first = true;
                foreach (var member in this._serializationMembers)
                {
                    var nameChanged = false;
                    var name = member.WireName;
                    var value = member.Accessor.Get(component);
                    if (options.WriteNamedValueObjectCallback != null)
                    {
                        var e = new JsonEventArgs(writer, value, objectGraph, options, name, component)
                        {
                            EventType = JsonEventType.WriteNamedValueObject,
                            First = first
                        };
                        options.WriteNamedValueObjectCallback(e);
                        first = e.First;
                        if (e.Handled)
                            continue;

                        nameChanged = name != e.Name;
                        name = e.Name;
                        value = e.Value;
                    }

                    if (options.SerializationOptions.HasFlag(JsonSerializationOptions.SkipNullPropertyValues))
                    {
                        if (value == null)
                            continue;
                    }

                    if (options.SerializationOptions.HasFlag(JsonSerializationOptions.SkipZeroValueTypes))
                    {
                        if (member.IsZeroValue(value))
                            continue;
                    }

                    if (options.SerializationOptions.HasFlag(JsonSerializationOptions.SkipNullDateTimeValues))
                    {
                        if (member.IsNullDateTimeValue(value))
                            continue;
                    }

                    var skipDefaultValues = options.SerializationOptions.HasFlag(JsonSerializationOptions.SkipDefaultValues);
                    if (skipDefaultValues && member.HasDefaultValue)
                    {
                        if (member.EqualsDefaultValue(value))
                            continue;
                    }

                    if (!first)
                    {
                        writer.Write(',');
                    }
                    else
                    {
                        first = false;
                    }

                    if (nameChanged)
                    {
                        Json.WriteNameValue(writer, name, value, objectGraph, options);
                    }
                    else
                    {
                        if (options.SerializationOptions.HasFlag(JsonSerializationOptions.WriteKeysWithoutQuotes))
                        {
                            writer.Write(member.EscapedWireName);
                        }
                        else
                        {
                            writer.Write('"');
                            writer.Write(member.EscapedWireName);
                            writer.Write('"');
                        }

                        writer.Write(':');
                        Json.WriteValue(writer, value, objectGraph, options);
                    }
                }
            }

            public override string ToString()
            {
                return this._type.AssemblyQualifiedName;
            }

            private static string GetKey(Type type, JsonOptions options)
            {
                return type.AssemblyQualifiedName + '\0' + options.GetCacheKey();
            }

            private static TypeDef UnlockedGet(Type type, JsonOptions options)
            {
                var key = GetKey(type, options);
                if (!_TypeDefs.TryGetValue(key, out var ta))
                {
                    ta = new TypeDef(type, options);
                    _TypeDefs.Add(key, ta);
                }
                return ta;
            }

            public static void Lock<T>(Action<T> action, T state)
            {
                lock (_lock)
                {
                    action(state);
                }
            }

            public static bool RemoveDeserializationMember(Type type, JsonOptions options, MemberDefinition member)
            {
                lock (_lock)
                {
                    var ta = UnlockedGet(type, options);
                    return ta._deserializationMembers.Remove(member);
                }
            }

            public static bool RemoveSerializationMember(Type type, JsonOptions options, MemberDefinition member)
            {
                lock (_lock)
                {
                    var ta = UnlockedGet(type, options);
                    return ta._serializationMembers.Remove(member);
                }
            }

            public static void AddDeserializationMember(Type type, JsonOptions options, MemberDefinition member)
            {
                lock (_lock)
                {
                    var ta = UnlockedGet(type, options);
                    ta._deserializationMembers.Add(member);
                }
            }

            public static void AddSerializationMember(Type type, JsonOptions options, MemberDefinition member)
            {
                lock (_lock)
                {
                    var ta = UnlockedGet(type, options);
                    ta._serializationMembers.Add(member);
                }
            }

            public static MemberDefinition[] GetDeserializationMembers(Type type, JsonOptions options)
            {
                lock (_lock)
                {
                    var ta = UnlockedGet(type, options);
                    return ta._deserializationMembers.ToArray();
                }
            }

            public static MemberDefinition[] GetSerializationMembers(Type type, JsonOptions options)
            {
                lock (_lock)
                {
                    var ta = UnlockedGet(type, options);
                    return ta._serializationMembers.ToArray();
                }
            }

            public static TypeDef Get(Type type, JsonOptions options)
            {
                lock (_lock)
                {
                    return UnlockedGet(type, options);
                }
            }

            public static bool IsKeyValuePairEnumerable(Type type, out Type keyType, out Type valueType)
            {
                lock (_lock)
                {
                    if (!_IsKvPe.TryGetValue(type, out var kv))
                    {
                        kv = new KeyValueType();
                        Json.InternalIsKeyValuePairEnumerable(type, out kv.KeyType, out kv.ValueType);
                        _IsKvPe.Add(type, kv);
                    }

                    keyType = kv.KeyType;
                    valueType = kv.ValueType;
                    return kv.KeyType != null;
                }
            }

            private static IEnumerable<MemberDefinition> EnumerateDefinitionsUsingReflection(bool serialization, Type type, JsonOptions options)
            {
                foreach (var info in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (options.SerializationOptions.HasFlag(JsonSerializationOptions.UseJsonAttribute))
                    {
                        var ja = Json.GetJsonAttribute(info);
                        if (ja != null)
                        {
                            if (serialization && ja.IgnoreWhenSerializing)
                                continue;

                            if (!serialization && ja.IgnoreWhenDeserializing)
                                continue;
                        }
                    }

                    if (options.SerializationOptions.HasFlag(JsonSerializationOptions.UseXmlIgnore))
                    {
                        if (info.IsDefined(typeof(XmlIgnoreAttribute), true))
                            continue;
                    }

                    if (options.SerializationOptions.HasFlag(JsonSerializationOptions.UseScriptIgnore))
                    {
                        if (Json.HasScriptIgnore(info))
                            continue;
                    }

                    if (serialization)
                    {
                        if (!info.CanRead)
                            continue;

                        var getMethod = info.GetGetMethod();
                        if (getMethod == null || getMethod.GetParameters().Length > 0)
                            continue;
                    }
                    // else we don't test the set method, as some properties can still be deserialized (collections)

                    var name = Json.GetObjectName(info, info.Name);

                    var ma = new MemberDefinition
                    {
                        Type = info.PropertyType,
                        Name = info.Name
                    };
                    if (serialization)
                    {
                        ma.WireName = name;
                        ma.EscapedWireName = Json.EscapeString(name);
                    }
                    else
                    {
                        ma.WireName = name;
                    }

                    ma.HasDefaultValue = Json.TryGetObjectDefaultValue(info, out var defaultValue);
                    ma.DefaultValue = defaultValue;
                    if (info.DeclaringType != null)
                        ma.Accessor = (IMemberAccessor)Activator.CreateInstance(
                            typeof(PropertyInfoAccessor<,>).MakeGenericType(info.DeclaringType, info.PropertyType),
                            info);
                    yield return ma;
                }

                if (options.SerializationOptions.HasFlag(JsonSerializationOptions.SerializeFields))
                {
                    foreach (var info in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                    {
                        if (options.SerializationOptions.HasFlag(JsonSerializationOptions.UseJsonAttribute))
                        {
                            var ja = Json.GetJsonAttribute(info);
                            if (ja != null)
                            {
                                if (serialization && ja.IgnoreWhenSerializing)
                                    continue;

                                if (!serialization && ja.IgnoreWhenDeserializing)
                                    continue;
                            }
                        }

                        if (options.SerializationOptions.HasFlag(JsonSerializationOptions.UseXmlIgnore))
                        {
                            if (info.IsDefined(typeof(XmlIgnoreAttribute), true))
                                continue;
                        }

                        if (options.SerializationOptions.HasFlag(JsonSerializationOptions.UseScriptIgnore))
                        {
                            if (Json.HasScriptIgnore(info))
                                continue;
                        }

                        var name = Json.GetObjectName(info, info.Name);

                        var ma = new MemberDefinition
                        {
                            Type = info.FieldType,
                            Name = info.Name
                        };
                        if (serialization)
                        {
                            ma.WireName = name;
                            ma.EscapedWireName = Json.EscapeString(name);
                        }
                        else
                        {
                            ma.WireName = name;
                        }

                        ma.HasDefaultValue = Json.TryGetObjectDefaultValue(info, out var defaultValue);
                        ma.DefaultValue = defaultValue;
                        ma.Accessor = (IMemberAccessor)Activator.CreateInstance(typeof(FieldInfoAccessor), info);
                        yield return ma;
                    }
                }
            }

            private static IEnumerable<MemberDefinition> EnumerateDefinitionsUsingTypeDescriptors(bool serialization, Type type, JsonOptions options)
            {
                foreach (var descriptor in TypeDescriptor.GetProperties(type).Cast<PropertyDescriptor>())
                {
                    if (options.SerializationOptions.HasFlag(JsonSerializationOptions.UseJsonAttribute))
                    {
                        var ja = descriptor.GetAttribute<JsonAttribute>();
                        if (ja != null)
                        {
                            if (serialization && ja.IgnoreWhenSerializing)
                                continue;

                            if (!serialization && ja.IgnoreWhenDeserializing)
                                continue;
                        }
                    }

                    if (options.SerializationOptions.HasFlag(JsonSerializationOptions.UseXmlIgnore))
                    {
                        if (descriptor.GetAttribute<XmlIgnoreAttribute>() != null)
                            continue;
                    }

                    if (options.SerializationOptions.HasFlag(JsonSerializationOptions.UseScriptIgnore))
                    {
                        if (Json.HasScriptIgnore(descriptor))
                            continue;
                    }

                    if (options.SerializationOptions.HasFlag(JsonSerializationOptions.SkipGetOnly) && descriptor.IsReadOnly)
                        continue;

                    var name = Json.GetObjectName(descriptor, descriptor.Name);

                    var ma = new MemberDefinition
                    {
                        Type = descriptor.PropertyType,
                        Name = descriptor.Name
                    };
                    if (serialization)
                    {
                        ma.WireName = name;
                        ma.EscapedWireName = Json.EscapeString(name);
                    }
                    else
                    {
                        ma.WireName = name;
                    }

                    ma.HasDefaultValue = Json.TryGetObjectDefaultValue(descriptor, out var defaultValue);
                    ma.DefaultValue = defaultValue;
                    ma.Accessor = (IMemberAccessor)Activator.CreateInstance(typeof(PropertyDescriptorAccessor), descriptor);
                    yield return ma;
                }
            }
        }
}