using System;
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

    [TestCase]
    public void IntReadAndWriteTest()
    {
        var buffer = new byte[5];
        var bufferWriter = new BufferWriter(buffer);
        // Take up an entire 32-bit word by using the max value with the absolute minimum and maximum range
        bufferWriter.WriteInt(int.MaxValue);

        // 1111 1111 = 255 so we whatever the number, we should only need 8 bits for this :)
        var min = 0;
        var max = 255;
        var randomValue = Random.Shared.Next(min, max);
        bufferWriter.WriteInt(randomValue, min, max);
        
        // I forgot to do the FinalWrite so this test errored a few times, I feel like this is going to be a recurring trend
        bufferWriter.FinalWrite(out _); 
        
        var bufferReader = new BufferReader(buffer);
        AssertInt(bufferReader.ReadInt()).IsEqual(int.MaxValue);
        AssertInt(bufferReader.ReadInt(min, max)).IsEqual(randomValue);
    }
}