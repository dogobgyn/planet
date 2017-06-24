using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using UnityEngine;
using System.Collections;
using System.Xml;
using System.Xml.Schema;
using System.IO;
using System.Runtime.Serialization;
using System.Globalization;

namespace TwoDee
{
    public static class Extensions
    {
        public static bool IsEmpty(this string name)
        {
            return name == null || name.Trim().Length == 0;
        }
    }
}

public class StringWriterWithEncoding : StringWriter
{
    Encoding encoding;

    public StringWriterWithEncoding(StringBuilder builder, Encoding encoding)
    : base(builder)
    {
        this.encoding = encoding;
    }

    public override Encoding Encoding
    {
        get { return encoding; }
    }
}

[XmlRoot("dictionarypod")]
[Serializable]
public class SerializableDictionaryPod<TKey, TValue> : Dictionary<TKey, TValue>, IXmlSerializable, ICloneable, ISerializable
{
    public SerializableDictionaryPod()
    {

    }

    #region ISerializable Members

    protected SerializableDictionaryPod(SerializationInfo info, StreamingContext context)
    {
        int itemCount = info.GetInt32("itemsCount");
        for (int i = 0; i < itemCount; i++)
        {
            KeyValuePair<TKey, TValue> kvp = (KeyValuePair<TKey, TValue>)info.GetValue(String.Format(CultureInfo.InvariantCulture, "Item{0}", i), typeof(KeyValuePair<TKey, TValue>));

            Add(kvp.Key, kvp.Value);
        }
    }

    void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue("itemsCount", Count);
        int itemIdx = 0; foreach (KeyValuePair<TKey, TValue> kvp in this)
        {
            info.AddValue(String.Format(CultureInfo.InvariantCulture, "Item{0}", itemIdx), kvp, typeof(KeyValuePair<TKey, TValue>));
            itemIdx++;
        }
    }

    #endregion

    #region " WriteXml "

    public void WriteXml(XmlWriter writer)
    {
        // Base types
        string baseKeyType = typeof(TKey).AssemblyQualifiedName;
        string baseValueType = typeof(TValue).AssemblyQualifiedName;
        writer.WriteAttributeString("keyType", baseKeyType);
        writer.WriteAttributeString("valueType", baseValueType);

        foreach (TKey key in this.Keys)
        {
            // Start
            writer.WriteStartElement("item");

            // Key
            Type keyType = key.GetType();
            XmlSerializer keySerializer = GetTypeSerializer(keyType.AssemblyQualifiedName);

            writer.WriteStartElement("key");
            if (keyType != typeof(TKey)) { writer.WriteAttributeString("type", keyType.AssemblyQualifiedName); }
            keySerializer.Serialize(writer, key);
            writer.WriteEndElement();

            // Value
            TValue value = this[key];
            Type valueType = value.GetType();
            XmlSerializer valueSerializer = GetTypeSerializer(valueType.AssemblyQualifiedName);

            writer.WriteStartElement("value");
            if (valueType != typeof(TValue)) { writer.WriteAttributeString("type", valueType.AssemblyQualifiedName); }
            valueSerializer.Serialize(writer, value);
            writer.WriteEndElement();

            // End
            writer.WriteEndElement();
        }
    }

    #endregion

    #region " ReadXml "

    public void ReadXml(XmlReader reader)
    {
        bool wasEmpty = reader.IsEmptyElement;
        reader.Read();

        if (wasEmpty)
        {
            return;
        }

        // Base types
        string baseKeyType = typeof(TKey).AssemblyQualifiedName;
        string baseValueType = typeof(TValue).AssemblyQualifiedName;

        while (reader.NodeType != XmlNodeType.EndElement)
        {
            // Start
            reader.ReadStartElement("item");

            // Key
            XmlSerializer keySerializer = GetTypeSerializer(reader["type"] ?? baseKeyType);
            reader.ReadStartElement("key");
            TKey key = (TKey)keySerializer.Deserialize(reader);
            reader.ReadEndElement();

            // Value
            XmlSerializer valueSerializer = GetTypeSerializer(reader["type"] ?? baseValueType);
            reader.ReadStartElement("value");
            TValue value = (TValue)valueSerializer.Deserialize(reader);
            reader.ReadEndElement();

            // Store
            this.Add(key, value);

            // End
            reader.ReadEndElement();
            reader.MoveToContent();
        }
        reader.ReadEndElement();
    }

    #endregion

    #region " GetSchema "

    public XmlSchema GetSchema()
    {
        return null;
    }

    #endregion

    #region " GetTypeSerializer "

    private static readonly Dictionary<string, XmlSerializer> _serializers = new Dictionary<string, XmlSerializer>();
    private static readonly object _deadbolt = new object();
    private XmlSerializer GetTypeSerializer(string type)
    {
        if (!_serializers.ContainsKey(type))
        {
            lock (_deadbolt)
            {
                if (!_serializers.ContainsKey(type))
                {
                    _serializers.Add(type, new XmlSerializer(Type.GetType(type)));
                }
            }
        }
        return _serializers[type];
    }

    #endregion

    #region ICloneable
    public object Clone()
    {
        SerializableDictionaryPod<TKey, TValue> clone = new SerializableDictionaryPod<TKey, TValue>();

        foreach (KeyValuePair<TKey, TValue> pair in this)
        {
            clone.Add(pair.Key, (TValue)pair.Value);
        }

        return clone;
    }
    public SerializableDictionaryPod<TKey, TValue> CloneTyped()
    {
        return (SerializableDictionaryPod<TKey, TValue>)this.Clone();
    }
    #endregion
}


[XmlRoot("dictionary")]
[Serializable]
public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IXmlSerializable, ICloneable, ISerializable
    where TValue : ICloneable
{
    public SerializableDictionary()
    {

    }

    #region ISerializable Members

    protected SerializableDictionary(SerializationInfo info, StreamingContext context)
    {
        int itemCount = info.GetInt32("itemsCount");
        for (int i = 0; i < itemCount; i++)
        {
            KeyValuePair<TKey,TValue> kvp = (KeyValuePair<TKey, TValue>)info.GetValue(String.Format(CultureInfo.InvariantCulture, "Item{0}", i), typeof(KeyValuePair<TKey, TValue>));

            Add(kvp.Key, kvp.Value);
        }
    }

    void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue("itemsCount", Count);
        int itemIdx = 0; foreach (KeyValuePair<TKey, TValue> kvp in this)
        {
            info.AddValue(String.Format(CultureInfo.InvariantCulture, "Item{0}", itemIdx), kvp, typeof(KeyValuePair<TKey, TValue>));
            itemIdx++;
        }
    }

    #endregion

    #region " WriteXml "

    public void WriteXml(XmlWriter writer)
    {
        // Base types
        string baseKeyType = typeof(TKey).AssemblyQualifiedName;
        string baseValueType = typeof(TValue).AssemblyQualifiedName;
        writer.WriteAttributeString("keyType", baseKeyType);
        writer.WriteAttributeString("valueType", baseValueType);

        foreach (TKey key in this.Keys)
        {
            // Start
            writer.WriteStartElement("item");

            // Key
            Type keyType = key.GetType();
            XmlSerializer keySerializer = GetTypeSerializer(keyType.AssemblyQualifiedName);

            writer.WriteStartElement("key");
            if (keyType != typeof(TKey)) { writer.WriteAttributeString("type", keyType.AssemblyQualifiedName); }
            keySerializer.Serialize(writer, key);
            writer.WriteEndElement();

            // Value
            TValue value = this[key];
            Type valueType = value.GetType();
            XmlSerializer valueSerializer = GetTypeSerializer(valueType.AssemblyQualifiedName);

            writer.WriteStartElement("value");
            if (valueType != typeof(TValue)) { writer.WriteAttributeString("type", valueType.AssemblyQualifiedName); }
            valueSerializer.Serialize(writer, value);
            writer.WriteEndElement();

            // End
            writer.WriteEndElement();
        }
    }

    #endregion

    #region " ReadXml "

    public void ReadXml(XmlReader reader)
    {
        bool wasEmpty = reader.IsEmptyElement;
        reader.Read();

        if (wasEmpty)
        {
            return;
        }

        // Base types
        string baseKeyType = typeof(TKey).AssemblyQualifiedName;
        string baseValueType = typeof(TValue).AssemblyQualifiedName;

        while (reader.NodeType != XmlNodeType.EndElement)
        {
            // Start
            reader.ReadStartElement("item");

            // Key
            XmlSerializer keySerializer = GetTypeSerializer(reader["type"] ?? baseKeyType);
            reader.ReadStartElement("key");
            TKey key = (TKey)keySerializer.Deserialize(reader);
            reader.ReadEndElement();

            // Value
            XmlSerializer valueSerializer = GetTypeSerializer(reader["type"] ?? baseValueType);
            reader.ReadStartElement("value");
            TValue value = (TValue)valueSerializer.Deserialize(reader);
            reader.ReadEndElement();

            // Store
            this.Add(key, value);

            // End
            reader.ReadEndElement();
            reader.MoveToContent();
        }
        reader.ReadEndElement();
    }

    #endregion

    #region " GetSchema "

    public XmlSchema GetSchema()
    {
        return null;
    }

    #endregion

    #region " GetTypeSerializer "

    private static readonly Dictionary<string, XmlSerializer> _serializers = new Dictionary<string, XmlSerializer>();
    private static readonly object _deadbolt = new object();
    private XmlSerializer GetTypeSerializer(string type)
    {
        if (!_serializers.ContainsKey(type))
        {
            lock (_deadbolt)
            {
                if (!_serializers.ContainsKey(type))
                {
                    _serializers.Add(type, new XmlSerializer(Type.GetType(type)));
                }
            }
        }
        return _serializers[type];
    }

    #endregion

    #region ICloneable
    public object Clone()
    {
        SerializableDictionary<TKey, TValue> clone = new SerializableDictionary<TKey, TValue>();

        foreach (KeyValuePair<TKey, TValue> pair in this)
        {
            clone.Add(pair.Key, (TValue)pair.Value.Clone());
        }

        return clone;
    }
    public SerializableDictionary<TKey, TValue> CloneTyped()
    {
        return (SerializableDictionary<TKey, TValue>)this.Clone();
    }
    #endregion
}

public static class ExtensionMethods
{
    public static Stream GenerateStream(this string s)
    {
        MemoryStream stream = new MemoryStream();
        StreamWriter writer = new StreamWriter(stream);
        writer.Write(s);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }

    public static int Clamp(this int i, int min, int max)
    {
        if (i < min) i = min;
        if (i > max) i = max;
        return i;
    }
    public static bool TryParseVector3(this string rString, out Vector3 vec)
    {
        vec = Vector3.zero;

        // Cannot possibly be valid
        if (rString.Length < 6) return false;

        string[] temp = rString.Substring(1, rString.Length - 2).Split(',');
        float x, y, z;
        bool result = float.TryParse(temp[0], out x);
        result &= float.TryParse(temp[1], out y);
        result &= float.TryParse(temp[2], out z);
        Vector3 rValue = new Vector3(x, y, z);
        vec = rValue;

        return result;
    }

    public static IEnumerable<Transform> GetChildrenNamed(this Transform target, string name)
    {
        var result = new List<Transform>();
        var children = target.GetChildren();
        foreach(var child in children)
        {
            if(child.name == name)
            {
                result.Add(child);
            }
        }

        return result;
    }

    public static IEnumerable<Transform> GetChildren(this Transform target)
	{
		List<Transform> children = new List<Transform>();
		for (int i = 0; i < target.childCount; ++i)
		{
			children.Add (target.GetChild(i));
		}
		return children;
	}

    public static IEnumerable<Transform> GetChildrenRecursive(this Transform target)
    {
        List<Transform> children = new List<Transform>();
        for (int i = 0; i < target.childCount; ++i)
        {
            children.Add(target.GetChild(i));

            foreach(var child in GetChildrenRecursive(target.GetChild(i)))
            {
                children.Add(child);
            }
        }

        return children;
    }

    public static IEnumerable<Transform> GetSelfAndChildrenRecursive(this GameObject gameObject)
    {
        yield return gameObject.transform;
        foreach (var child in GetChildrenRecursive(gameObject.transform)) yield return child;
    }


    public static Transform Search(this Transform target, string name)
	{
		if (target.name == name) return target;
		for (int i = 0; i < target.childCount; ++i)
		{
			var result = Search(target.GetChild(i), name);
			if (result != null) return result;
		}
		return null;
	}

    public static void GoAway(this MonoBehaviour behavior)
    {
        behavior.transform.position = new Vector3(-999.0f, -999.0f, 0.0f);
    }

    public class ConstraintList
    {
        public ConstraintList(GameObject g)
        {
            this.g = g;
            var rb = g.GetComponent<Rigidbody>();
            var rbc = rb.constraints;
            x = (0 != (rbc & RigidbodyConstraints.FreezePositionX));
            y = (0 != (rbc & RigidbodyConstraints.FreezePositionY));
            z = (0 != (rbc & RigidbodyConstraints.FreezePositionZ));
        }

        public void Set()
        {
            var rb = g.GetComponent<Rigidbody>();
            rb.constraints = (rb.constraints & 
                ~(RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezePositionZ)) |
                (x ? RigidbodyConstraints.FreezePositionX : 0) |
                (y ? RigidbodyConstraints.FreezePositionY : 0) |
                (z ? RigidbodyConstraints.FreezePositionZ : 0);
        }

        public void UnlockXY()
        {
            x = y = false;
            Set();
        }

        public void FlipXY()
        {
            x = !x;
            y = !y;
            Set();
        }

        public bool x;
        public bool y;
        public bool z;
        private GameObject g;
    };
    public static ConstraintList GetConstraints(this GameObject g)
    {
        return new ConstraintList(g);
    }

    public static TValue GetValueOrCreate<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key) where TValue : class, new()
    {
        TValue val;
        if (!dict.TryGetValue(key, out val))
        {
            val = new TValue();
            dict[key] = val;
        }
        return val;
    }

    public static void Swap<T>(this List<T> list, int a, int b)
    {
        var temp = list[a];
        list[a] = list[b];
        list[b] = temp;
    }
}