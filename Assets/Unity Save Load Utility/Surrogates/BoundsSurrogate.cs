using System.Runtime.Serialization;
using UnityEngine;

sealed class BoundsSurrogate : ISerializationSurrogate
{

    // Method called to serialize
    public void GetObjectData(System.Object obj,
        SerializationInfo info, StreamingContext context)
    {

        Bounds b = (Bounds)obj;
        info.AddValue("cx", b.center.x);
        info.AddValue("cy", b.center.y);
        info.AddValue("cz", b.center.z);
        info.AddValue("sx", b.size.x);
        info.AddValue("sy", b.size.y);
        info.AddValue("sz", b.size.z);
    }

    // Method called to deserialize
    public System.Object SetObjectData(System.Object obj,
        SerializationInfo info, StreamingContext context,
        ISurrogateSelector selector)
    {

        Bounds b = (Bounds)obj;
        var center = new Vector3(
        (float)info.GetValue("cx", typeof(float)),
        (float)info.GetValue("cy", typeof(float)),
        (float)info.GetValue("cz", typeof(float)));
        b.center = center;
        var size = new Vector3(
        (float)info.GetValue("sx", typeof(float)),
        (float)info.GetValue("sy", typeof(float)),
        (float)info.GetValue("sz", typeof(float)));
        b.size = size;
        obj = b;

        return obj;
    }
}