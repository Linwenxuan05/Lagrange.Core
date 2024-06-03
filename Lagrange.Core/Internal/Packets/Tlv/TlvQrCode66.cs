using Lagrange.Core.Common;
using Lagrange.Core.Utility.Binary;
using Lagrange.Core.Utility.Binary.Tlv;
using Lagrange.Core.Utility.Binary.Tlv.Attributes;

namespace Lagrange.Core.Internal.Packets.Tlv;

[TlvQrCode(0x066)]
internal class TlvQrCode66 : TlvBody
{
    public TlvQrCode66(BotAppInfo appInfo) => PtOsVersion = appInfo.PtOsVersion;

    [BinaryProperty] public int PtOsVersion { get; }
}