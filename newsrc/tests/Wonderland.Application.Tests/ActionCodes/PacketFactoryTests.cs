using System.Buffers.Binary;
using System.Text;
using FluentAssertions;
using Wonderland.Application.ActionCodes;
using Xunit;

namespace Wonderland.Application.Tests.ActionCodes;

public class PacketFactoryTests
{
    [Fact]
    public void Build_ShouldIncludeWloHeader()
    {
        var packet = PacketFactory.Create(6, 1).Build();

        // First 2 bytes = 0x44F4 header (little-endian)
        var header = BinaryPrimitives.ReadUInt16LittleEndian(packet.AsSpan(0, 2));
        header.Should().Be(0x44F4);
    }

    [Fact]
    public void Build_ShouldIncludeCorrectPayloadLength()
    {
        // Payload: [AC:8][Sub:8] = 2 bytes
        var packet = PacketFactory.Create(6, 1).Build();

        var length = BinaryPrimitives.ReadUInt16LittleEndian(packet.AsSpan(2, 2));
        length.Should().Be(2);
    }

    [Fact]
    public void Build_ShouldWriteActionCodeAndSubAction()
    {
        var packet = PacketFactory.Create(63, 4).Build();

        packet[4].Should().Be(63); // AC
        packet[5].Should().Be(4);  // Sub
    }

    [Fact]
    public void U8_ShouldWriteSingleByte()
    {
        var packet = PacketFactory.Create(0).U8(42).Build();

        // Header(4) + AC(1) + Sub(1) + value(1) = index 6
        packet[6].Should().Be(42);
    }

    [Fact]
    public void U16_ShouldWriteLittleEndian()
    {
        var packet = PacketFactory.Create(0).U16(0x1234).Build();

        var value = BinaryPrimitives.ReadUInt16LittleEndian(packet.AsSpan(6, 2));
        value.Should().Be(0x1234);
    }

    [Fact]
    public void U32_ShouldWriteLittleEndian()
    {
        var packet = PacketFactory.Create(0).U32(0xDEADBEEF).Build();

        var value = BinaryPrimitives.ReadUInt32LittleEndian(packet.AsSpan(6, 4));
        value.Should().Be(0xDEADBEEF);
    }

    [Fact]
    public void NStr_ShouldWriteNullTerminatedString()
    {
        var packet = PacketFactory.Create(0).NStr("Hello").Build();

        var strBytes = packet[6..11];
        Encoding.UTF8.GetString(strBytes).Should().Be("Hello");
        packet[11].Should().Be(0); // null terminator
    }

    [Fact]
    public void PStr_ShouldWriteLengthPrefixedString()
    {
        var packet = PacketFactory.Create(0).PStr("Hi").Build();

        packet[6].Should().Be(2); // length prefix
        Encoding.UTF8.GetString(packet, 7, 2).Should().Be("Hi");
    }

    [Fact]
    public void Str_ShouldWriteFixedLengthPaddedWithZeros()
    {
        var packet = PacketFactory.Create(0).Str("AB", 5).Build();

        packet[6].Should().Be((byte)'A');
        packet[7].Should().Be((byte)'B');
        packet[8].Should().Be(0); // padding
        packet[9].Should().Be(0);
        packet[10].Should().Be(0);
    }

    [Fact]
    public void Pad_ShouldWriteZeroBytes()
    {
        var packet = PacketFactory.Create(0).Pad(3).Build();

        packet[6].Should().Be(0);
        packet[7].Should().Be(0);
        packet[8].Should().Be(0);
    }

    [Fact]
    public void Build_ComplexPacket_ShouldMaintainByteOrder()
    {
        // Simulate AC6 movement broadcast: [6][1][charID:32][dir:8][x:16][y:16]
        var packet = PacketFactory.Create(6, 1)
            .U32(12345)
            .U8(3)
            .U16(100)
            .U16(200)
            .Build();

        // Total: header(4) + AC(1) + Sub(1) + charId(4) + dir(1) + x(2) + y(2) = 15
        packet.Length.Should().Be(15);

        var payloadLength = BinaryPrimitives.ReadUInt16LittleEndian(packet.AsSpan(2, 2));
        payloadLength.Should().Be(11); // 1+1+4+1+2+2

        packet[4].Should().Be(6);  // AC
        packet[5].Should().Be(1);  // Sub
        BinaryPrimitives.ReadUInt32LittleEndian(packet.AsSpan(6, 4)).Should().Be(12345u);
        packet[10].Should().Be(3); // direction
        BinaryPrimitives.ReadUInt16LittleEndian(packet.AsSpan(11, 2)).Should().Be(100);
        BinaryPrimitives.ReadUInt16LittleEndian(packet.AsSpan(13, 2)).Should().Be(200);
    }

    [Fact]
    public void BuildPayload_ShouldExcludeHeader()
    {
        var payload = PacketFactory.Create(2, 5).U32(99).BuildPayload();

        // No header — just AC(1) + Sub(1) + value(4) = 6
        payload.Length.Should().Be(6);
        payload[0].Should().Be(2);
        payload[1].Should().Be(5);
    }
}
