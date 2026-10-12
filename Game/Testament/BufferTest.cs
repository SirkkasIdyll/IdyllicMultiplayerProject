using ENet;
using Game.Temperance.Network;

namespace Game.Testament;

using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class BufferTest
{
    [TestCase]
    public void BoolReadAndWriteTest()
    {
        var buffer = new byte[5];
        var bufferWriter = new BufferWriter(buffer);
        // 1111 0011
        bufferWriter.WriteBool(true);
        bufferWriter.WriteBool(true);
        bufferWriter.WriteBool(false);
        bufferWriter.WriteBool(false);
        bufferWriter.WriteBool(true);
        bufferWriter.WriteBool(true);
        bufferWriter.WriteBool(true);
        bufferWriter.WriteBool(true);
        
        // 0101 0000
        bufferWriter.WriteBool(false);
        bufferWriter.WriteBool(false);
        bufferWriter.WriteBool(false);
        bufferWriter.WriteBool(false);
        bufferWriter.WriteBool(true);
        bufferWriter.WriteBool(false);
        bufferWriter.WriteBool(true);
        bufferWriter.WriteBool(false);
        
        // 0010 0010
        bufferWriter.WriteBool(false);
        bufferWriter.WriteBool(true);
        bufferWriter.WriteBool(false);
        bufferWriter.WriteBool(false);
        bufferWriter.WriteBool(false);
        bufferWriter.WriteBool(true);
        bufferWriter.WriteBool(false);
        bufferWriter.WriteBool(false);
        
        // 0101 0000
        bufferWriter.WriteBool(false);
        bufferWriter.WriteBool(false);
        bufferWriter.WriteBool(false);
        bufferWriter.WriteBool(false);
        bufferWriter.WriteBool(true);
        bufferWriter.WriteBool(false);
        bufferWriter.WriteBool(true);
        bufferWriter.WriteBool(false);
        
        // 0000 0101
        bufferWriter.WriteBool(true);
        bufferWriter.WriteBool(false);
        bufferWriter.WriteBool(true);
        bufferWriter.WriteBool(false);
        bufferWriter.WriteBool(false);
        bufferWriter.WriteBool(false);
        bufferWriter.WriteBool(false);
        bufferWriter.WriteBool(false);

        bufferWriter.FinalWrite(out _);

        var bufferReader = new BufferReader(buffer);
        AssertBool(bufferReader.ReadBool()).IsTrue();
        AssertBool(bufferReader.ReadBool()).IsTrue();
        AssertBool(bufferReader.ReadBool()).IsFalse();
        AssertBool(bufferReader.ReadBool()).IsFalse();
        AssertBool(bufferReader.ReadBool()).IsTrue();
        AssertBool(bufferReader.ReadBool()).IsTrue();
        AssertBool(bufferReader.ReadBool()).IsTrue();
        AssertBool(bufferReader.ReadBool()).IsTrue();
        
        AssertBool(bufferReader.ReadBool()).IsFalse();
        AssertBool(bufferReader.ReadBool()).IsFalse();
        AssertBool(bufferReader.ReadBool()).IsFalse();
        AssertBool(bufferReader.ReadBool()).IsFalse();
        AssertBool(bufferReader.ReadBool()).IsTrue();
        AssertBool(bufferReader.ReadBool()).IsFalse();
        AssertBool(bufferReader.ReadBool()).IsTrue();
        AssertBool(bufferReader.ReadBool()).IsFalse();
        
        AssertBool(bufferReader.ReadBool()).IsFalse();
        AssertBool(bufferReader.ReadBool()).IsTrue();
        AssertBool(bufferReader.ReadBool()).IsFalse();
        AssertBool(bufferReader.ReadBool()).IsFalse();
        AssertBool(bufferReader.ReadBool()).IsFalse();
        AssertBool(bufferReader.ReadBool()).IsTrue();
        AssertBool(bufferReader.ReadBool()).IsFalse();
        AssertBool(bufferReader.ReadBool()).IsFalse();
        
        AssertBool(bufferReader.ReadBool()).IsFalse();
        AssertBool(bufferReader.ReadBool()).IsFalse();
        AssertBool(bufferReader.ReadBool()).IsFalse();
        AssertBool(bufferReader.ReadBool()).IsFalse();
        AssertBool(bufferReader.ReadBool()).IsTrue();
        AssertBool(bufferReader.ReadBool()).IsFalse();
        AssertBool(bufferReader.ReadBool()).IsTrue();
        AssertBool(bufferReader.ReadBool()).IsFalse();
        
        AssertBool(bufferReader.ReadBool()).IsTrue();
        AssertBool(bufferReader.ReadBool()).IsFalse();
        AssertBool(bufferReader.ReadBool()).IsTrue();
        AssertBool(bufferReader.ReadBool()).IsFalse();
        AssertBool(bufferReader.ReadBool()).IsFalse();
        AssertBool(bufferReader.ReadBool()).IsFalse();
        AssertBool(bufferReader.ReadBool()).IsFalse();
        AssertBool(bufferReader.ReadBool()).IsFalse();

    }
}