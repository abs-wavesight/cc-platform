using ProtoBuf;

namespace Models;

[ProtoContract]
public class Model
{
    [ProtoMember(1)]
    public int Id { get; set; }
    [ProtoMember(2)]
    public string Data { get; set; }
}