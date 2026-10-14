using System;
using System.Buffers.Binary;
using Godot;

namespace Game.Temperance.Network;

/// <summary>
/// Intended to read from a bitpacked packet up to 32-bits at a time
/// </summary>
/// <param name="buffer">The data of the packet</param>
public ref struct BufferReader(byte[] buffer)
{
    /// <summary>
    /// Using a Span requires we use a ref struct instead of a class
    /// Holds the bytes we want to read values from
    /// </summary>
    private readonly ReadOnlySpan<byte> _buffer = new(buffer, 0,  buffer.Length);
    
    /// <summary>
    /// Where to read the next 32-bit word in the buffer
    /// </summary>
    private int _wordIndex = 0;
    
    /// <summary>
    /// Because some of the content we may want to read will be 32-bits long, we need a place to store more than 32-bits
    /// Hence this temp buffer to store upcoming words in
    /// </summary>
    private ulong _tempBuffer = 0;
    
    /// <summary>
    /// Tracks how many bits of data are currently in the temp buffer
    /// Useful because there may be segments that are just all 0s and whatnot, that's still data!
    /// </summary>
    private int _bitsInTempBuffer = 0;

    /// <summary>
    /// We keep track of the actual number of bits we've read from the buffer
    /// So that we can read bits from any position of a byte
    /// </summary>
    private int _bitsReadFromBuffer = 0;

    /// <summary>
    /// Given up to a potential amount of 32 bits,
    /// returns the content in the buffer matching that amount of bits
    /// 
    /// 32 bits is an arbitary limit of something we might want to read at any given time
    /// but it's a good limit to incentivize using smaller quantizations of values :)
    /// 
    /// </summary>
    /// <param name="bits">The amount of bits we want to read from the buffer</param>
    /// <param name="content">The content taken out of the buffer</param>
    public void Read(int bits, out uint content)
    {
        content = 0u;

        // You didn't even want to read anything? Why?
        if (bits == 0)
            return;
        
        // Stop trying to read negative bits, they don't exist!
        if (bits is < 0 or > 32)
            throw new ArgumentOutOfRangeException(nameof(bits));
        
        // We all outta bits
        if (bits > _buffer.Length * 8 - _bitsReadFromBuffer)
            throw new InvalidOperationException("Potential buffer underflow");
        
        // Load the requested amount of bits into the TempBuffer
        var byteIndex = _wordIndex * 4;
        while (_bitsInTempBuffer < bits)
        {
            // If there are enough bits to load one entire 32-bit word, just load the entire 32-bit word
            if (byteIndex * 8 + 32 <= _buffer.Length * 8)
            {
                // We're loading an entire 32-bit word for the sake of speed since it's equivalent to 4 loops loading one byte at a time
                // We wrote them in Little Endian originally, so we read them as Little Endian as well
                var bitsToInsert = (ulong)BinaryPrimitives.ReadUInt32LittleEndian(_buffer.Slice(byteIndex, 4));
                
                // Shift the bits so they don't collide with what we already have loaded in the temp buffer
                bitsToInsert <<= _bitsInTempBuffer;
                
                // Insert the bits into the temp buffer by ORing it
                // The area to the left of the bits already in the temp buffer should all be 0s,
                // so this is the same thing as just adding the two values together
                _tempBuffer |= bitsToInsert;
                _bitsInTempBuffer += 32;
                _wordIndex += 1;
                byteIndex += 4;
            }
            // We don't have enough bits to load an entire 32-bit word, so we have to do it one byte at a time :(
            // This only happens up to 3 times at the very end of the buffer, so it's not that bad
            else
            {
                // Loads 8 bits then does the whole move insert shebang as the above
                var bitsToInsert = (ulong)_buffer[byteIndex++];
                bitsToInsert <<= _bitsInTempBuffer;
                _tempBuffer |= bitsToInsert;
                _bitsInTempBuffer += 8;
            }
        }

        // Selects just the bits that are asked for from the temp buffer for the output content
        content = (uint)_tempBuffer & BufferWriter.Mask(bits);
        
        // Shifts out the bits that were read
        _tempBuffer >>= bits;
        _bitsInTempBuffer -= bits;
        _bitsReadFromBuffer += bits;
    }

    /// <summary>
    /// Reads a single bit from the buffer and interprets it as a boolean
    /// </summary>
    public bool ReadBool()
    {
        Read(1, out var content);
        return content != 0;
    }

    /// <summary>
    /// Given a range, returns the integer read from the amount of bits it would take to cover that range
    /// </summary>
    public int ReadInt(int min = int.MinValue, int max = int.MaxValue)
    {
        if (max <= min)
            throw new InvalidOperationException("Max must be greater than min");
        
        var bits = BufferWriter.CalculateBits(min, max);
        Read(bits, out var content);
        
        // We re-add the minimum here because we subtracted the minimum when we first wrote the packet
        // as a quirk of converting from int to uint back to int
        return (int)content + min;
    }

    /// <summary>
    /// Honestly, following the math to undo it kind of hurts my brain
    /// </summary>
    public float ReadFloat(float min, float max, float precision)
    {
        if (max <= min)
            throw new InvalidOperationException("Max must be greater than min");
        
        // Range is always positive
        var rangeFloat = max - min;
        var rangeIntForPrecision = (int)MathF.Ceiling(rangeFloat / precision);
        var bits = BufferWriter.CalculateBits(0, rangeIntForPrecision);
        Read(bits, out var content);
        
        // We're just undoing all the shenanigans we did to write it in the first place
        return min + content / (float)rangeIntForPrecision * rangeFloat;
    }
    
    public Vector2 ReadVector2(float min, float max, float precision)
    {
        var x = ReadFloat(min, max, precision);
        var y = ReadFloat(min, max, precision);
        return new Vector2(x, y);
    }
    
    public Vector2I ReadVector2I(int min = int.MinValue, int max = int.MaxValue)
    {
        var x = ReadInt(min, max);
        var y = ReadInt(min, max);
        return new Vector2I(x, y);
    }
    
    public Rect2 ReadRect2(float min, float max, float precision)
    {
        var position = ReadVector2(min, max, precision);
        var size = ReadVector2(min, max, precision);
        return new Rect2(position, size);
    }
    
    public Rect2I ReadRect2I(int min = int.MinValue, int max = int.MaxValue)
    {
        var position = ReadVector2I(min, max);
        var size = ReadVector2I(min, max);
        return new Rect2I(position, size);
    }
    
    public Vector3 ReadVector3(float min, float max, float precision)
    {
        var x = ReadFloat(min, max, precision);
        var y = ReadFloat(min, max, precision);
        var z = ReadFloat(min, max, precision);
        return new Vector3(x, y, z);
    }
    
    public Vector3I ReadVector3I(int min = int.MinValue, int max = int.MaxValue)
    {
        var x = ReadInt(min, max);
        var y = ReadInt(min, max);
        var z = ReadInt(min, max);
        return new Vector3I(x, y, z);
    }
    
    public Basis ReadBasis(float min, float max, float precision)
    {
        var x = ReadVector3(min, max, precision);
        var y = ReadVector3(min, max, precision);
        var z = ReadVector3(min, max, precision);
        return new Basis(x, y, z);
    }
    
    public Transform3D ReadTransform3D(float min, float max, float precision)
    {
        var basis = ReadBasis(min, max, precision);
        var origin = ReadVector3(min, max, precision);
        return new Transform3D(basis, origin);
    }
    
    public Vector4 ReadVector4(float min, float max, float precision)
    {
        var w = ReadFloat(min, max, precision);
        var x = ReadFloat(min, max, precision);
        var y = ReadFloat(min, max, precision);
        var z = ReadFloat(min, max, precision);
        return new Vector4(x, y, z, w);
    }
    
    public Vector4I ReadVector4I(int min = int.MinValue, int max = int.MaxValue)
    {
        var w = ReadInt(min, max);
        var x = ReadInt(min, max);
        var y = ReadInt(min, max);
        var z = ReadInt(min, max);
        return new  Vector4I(x, y, z, w);
    }
    
    public Plane ReadPlane(float min, float max, float precision)
    {
        var d = ReadFloat(min, max, precision);
        var normal = ReadVector3(min, max, precision);
        return new Plane(normal, d);
    }
    
    public Aabb ReadAabb(float min, float max, float precision)
    {
        var position = ReadVector3(min, max, precision);
        var size = ReadVector3(min, max, precision);
        return new Aabb(position, size);
    }

    /// <summary>
    /// If you want to know the voodoo we're doing just read the Write equivalent of this
    ///
    /// This will read a NORMALIZED quaternion since that's useful for rotation!
    /// </summary>
    /// <returns></returns>
    public Quaternion ReadQuaternion()
    {
        var bound = 0.707107f;
        
        // We left out the largest value, and we're going to reconstruct it from the three smaller values
        var largestIndex = ReadInt(0, 3);
        var content = new Quaternion();
        
        // Reverse pythagorean energy except with three squares to add
        var sumOfSquares = 0f;
        for (var i = 0; i < 4; i++)
        {
            if (i == largestIndex)
                continue;

            content[i] = ReadFloat(-bound, bound, 0.001f);
            sumOfSquares += content[i] * content[i];
        }

        // And here we are
        content[largestIndex] = MathF.Sqrt(MathF.Max(0f, 1f - sumOfSquares));
        return content;
    }
}