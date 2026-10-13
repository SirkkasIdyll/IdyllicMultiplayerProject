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
    [ThrowsException(typeof(InvalidOperationException))]
    public void BufferOverflowTest()
    {
        var buffer = new byte[4];
        var bufferWriter = new BufferWriter(buffer);
        bufferWriter.WriteInt(int.MaxValue);
        bufferWriter.WriteInt(int.MaxValue);
    }

    [TestCase]
    [ThrowsException(typeof(InvalidOperationException))]
    public void BufferUnderflowTest()
    {
        var buffer = new byte[4];
        var bufferWriter = new BufferWriter(buffer);
        bufferWriter.WriteInt(int.MaxValue);
        
        var bufferReader = new BufferReader(buffer);
        AssertInt(bufferReader.ReadInt()).IsEqual(int.MaxValue);
        bufferReader.ReadInt(0, 1);
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
    
    [TestCase]
    public void FloatReadAndWriteTest()
    {
        var buffer = new byte[6];
        var bufferWriter = new BufferWriter(buffer);
        // It should take two bytes this time to handle this range at this precision level
        var min = -5.25f;
        var max = 2.68f;
        var precision = 0.01f;
        var range = max - min;
        var randomValue1 = Random.Shared.NextSingle() * range + min;
        var randomValue2 = Random.Shared.NextSingle() * range + min;
        var randomValue3 = Random.Shared.NextSingle() * range + min;
        bufferWriter.WriteFloat(randomValue1, min, max, precision);
        bufferWriter.WriteFloat(randomValue2, min, max, precision);
        bufferWriter.WriteFloat(randomValue3, min, max, precision);
        bufferWriter.FinalWrite(out _);
        
        var bufferReader = new BufferReader(buffer);
        // If we are +- half the precision range, we are all good
        AssertFloat(bufferReader.ReadFloat(min, max, precision)).IsEqualApprox(randomValue1, precision / 2);
        AssertFloat(bufferReader.ReadFloat(min, max, precision)).IsEqualApprox(randomValue2, precision / 2);
        AssertFloat(bufferReader.ReadFloat(min, max, precision)).IsEqualApprox(randomValue3, precision / 2);
    }
}