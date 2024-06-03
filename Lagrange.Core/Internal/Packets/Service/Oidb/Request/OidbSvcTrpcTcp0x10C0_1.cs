using ProtoBuf;

namespace Lagrange.Core.Internal.Packets.Service.Oidb.Request;

// ReSharper disable InconsistentNaming

/// <summary>
/// Fetch Friends & Group Notification List
/// </summary>
[ProtoContract]
[OidbSvcTrpcTcp(0x10c0, 1)]
internal class OidbSvcTrpcTcp0x10C0_1
{
    [ProtoMember(1)] public uint Count { get; set; } // 20
    
    [ProtoMember(2)] public uint Field2 { get; set; } // 0
}