using System;
using System.Buffers.Binary;
using System.Numerics;
using Godot;

namespace Game.Temperance.Network;

/// <summary>
/// This is a bitpacker that writes bits in 32-bit words instead of 8-bit bytes because Glenn Fiedler told me
/// not to fart out my bytes like it's 1985 :( "modern machines are optimized to work this way"
/// well maybe my brain isn't optimized to work this way
///
/// Note: A bitpacker is something used to optimize packet size by combining many bits together seamlessly
/// rather than separating each bit of information into their own distinct segments. It's up to you to know
/// how to decode the incoming packets by adding on headers and things like that
/// </summary>
public ref struct BufferWriter(Span<byte> buffer)
{
    /// <summary>
    /// Using a Span requires we use a ref struct instead of a class
    /// Holds the bytes we want to eventually turn into a packet
    /// </summary>
    private readonly Span<byte> _buffer = buffer;
    
    /// <summary>
    /// Where to write the next 32-bit word in the buffer
    /// </summary>
    private int _wordIndex = 0;
    
    /// <summary>
    /// To write 32-bits at a time, we need something capable of holding more than 32-bits
    /// So here we are, a 64-bit temp buffer
    /// </summary>
    private ulong _tempBuffer = 0;

    /// <summary>
    /// We keep track of the actual number of bits we've written to the temp buffer
    /// So that we can add onto partially filled words for proper bitpacking
    /// Once we hit 32, we send the word over to the actual buffer in one go
    /// </summary>
    private int _bitsInTempBuffer = 0;
    
    /// <summary>
    /// A mask is used in binary math to select which bits, starting from the right of
    /// in this case a 32-bit word, that you want to keep.
    /// You take the original 32-bit word, AND it against this mask, and all the bits outside the range become 0s
    /// while the bits in range keep their original value. Then you can shift the original 32-bit word
    /// by the number of bits you took out of it.
    /// Example:
    /// 32-bit word: 1111 0101 1110 0000 1010 0001 0010 0110
    /// 10-bit mask: 0000 0000 0000 0000 0000 0011 1111 1111
    /// word & mask: 0000 0000 0000 0000 0000 0001 0010 0110
    /// word>>=bits: 0000 0000 0011 1101 0111 1000 0010 1000
    /// </summary>
    /// <param name="bits">The number of bits of the 32-bit word we want</param>
    /// <returns></returns>
    public static uint Mask(int bits)
    {
        // The formula below doesn't work if you want all 32 bits, so we cover that case here
        if (bits >= 32)
            return uint.MaxValue;
        
        // The 1u is shifted left by the amount of bits given to put it 1 ahead of all of the bits you want
        // Then when you subtract 1, all the bits flip from a 0 to a 1, giving you the proper mask
        return (1u << bits) - 1;
    }

    /// <summary>
    /// Given a range of valid values for an integer, calculates the bits necessary to hold the information
    /// Important for quantization!
    /// We only need to care about the specific number of bits a value needs rather than the whole 32-bit range
    /// </summary>
    public static int CalculateBits(int min, int max)
    {
        // Bit operations needs an unsigned integer to do the log
        var range = (uint)max - (uint)min;
        if (range == 0)
            return 0;
        
        // Gets us the binary FLOOR, then we add 1 to get bits :)
        // log_2(1) = 0 (2⁰ = 1), can't have a bit of 0 so 0 + 1 = 1
        // log_2(2) = 1 (2¹ = 2), takes 2 bits to write 2 so 1 + 1 = 2
        // log_2(3) = 1.584... (2^¹.⁵⁸⁴... = 3) takes 2 bits to write 3 so 1 + 1 = 2
        // log_2(4) = 2 (2² = 4), takes 3 bits to write 4 so 2 + 1 = 3
        return BitOperations.Log2(range) + 1;
    }

    /// <summary>
    /// Given a value of some 32-bit range and the limit of how many bits to take from it,
    /// we store the content into a temporary buffer until we reach 32 bits, then we write
    /// the content to the main buffer.
    /// If you don't have anything left to write, call <see cref="FinalWrite"/>
    /// </summary>
    /// <param name="content">The potentially 32-bit range of values to add to the buffer</param>
    /// <param name="bits">The number of bits to actually take from the content</param>
    public void StoreOrWrite(uint content, int bits)
    {
        // Nothing to write (why? why would you do this?)
        if (bits == 0)
            return;
        
        // Stop trying to write negative bits to save on bandwidth
        if (bits is < 0 or > 32)
            throw new ArgumentOutOfRangeException(nameof(bits));
        
        // Buffer length is the total amount of bytes it could ever hold
        // Multiplied by 8 is the total amount of bits it could ever hold
        // SO if we compare our word index converted to bits plus the amount of bits we want to potentially write
        // And it exceeds our max length, it leads to an overflow
        if ((_wordIndex - 1) * 32 + _bitsInTempBuffer + bits > _buffer.Length * 8)
            throw new InvalidOperationException("Potential buffer overflow");
        
        // Take up to 32 bits from the content that we want to insert into our temp buffer
        var bitsToInsert = (ulong)content & Mask(bits);
        
        // Shift the bits left of the bits already in the temp buffer so they don't collide
        bitsToInsert <<= _bitsInTempBuffer;
        
        // Insert the bits into the temp buffer by ORing it
        // The area to the left of the bits already in the temp buffer should all be 0s,
        // so this is the same thing as just adding the two values together
        _tempBuffer |= bitsToInsert;
        _bitsInTempBuffer += bits;
        
        // Now that the bits are in the temp buffer,
        // we add it on if we reach 32+ bits
        if (_bitsInTempBuffer < 32)
            return;
        
        // We write it right to left (little endian) because we want to end up reading the packet in order of insertion
        BinaryPrimitives.WriteUInt32LittleEndian(_buffer.Slice(_wordIndex * 4, 4), (uint)_tempBuffer);
        
        // Get rid of the written bits in the temp buffer
        _tempBuffer >>= 32;
        _bitsInTempBuffer -= 32;
        _wordIndex++;
    }

    /// <summary>
    /// If there's nothing left to add via <see cref="StoreOrWrite"/>
    /// write the remaining contents of the temp buffer into the main buffer
    /// ONE BYTE AT A TIME BECAUSE MAYBE I LIKE FARTING OUT MY BYTES LIKE IT'S 1985
    /// </summary>
    /// <param name="byteIndex">How many bytes we can write to the packet</param>
    public void FinalWrite(out int byteIndex)
    {
        // Look at how simple shitting out a byte at a time is
        byteIndex = _wordIndex * 4;
        while (_bitsInTempBuffer > 0)
        {
            _buffer[byteIndex++] = (byte)_tempBuffer;
            _tempBuffer >>= 8;
            _bitsInTempBuffer -= 8;
        }
        
        // If we use the BufferWriter again, we leave the bits at 0 for the next person :)
        _bitsInTempBuffer = 0;
    }

    /// <summary>
    /// Given a bool, writes an unsigned 1 if true, or an unsigned 0 if false
    /// A bool only needs 1 bit!
    /// </summary>
    public void WriteBool(bool value)
    {
        StoreOrWrite(value ? 1u : 0u, 1);
    }

    /// <summary>
    /// Given an int, writes the bits of it to the buffer
    /// The min-max range is used for quantization (optimization of bits used to store the int in the buffer)
    /// so you are HIGHLY encouraged to pass one :))))))))
    /// </summary>
    public void WriteInt(int value, int min = int.MinValue, int max = int.MaxValue)
    {
        if (max <= min)
            throw new InvalidOperationException("Max must be greater than min");
        
        // Yes you still have to respect the range
        if (value < min || value > max)
            throw new ArgumentOutOfRangeException(nameof(value));
        
        // We subtract the min from the value because it lets us support the negative half of numbers in uint
        // uints are more efficient than integers by one bit because we don't need to reserve one bit at all times to
        // indicate whether it's positive or negative
        var content = (uint)(value - min);
        var bits = CalculateBits(min, max);
        StoreOrWrite(content, bits);
    }

    /// <summary>
    /// For floating points we preserve what we think will be an acceptable amount of precision
    /// that won't impact gameplay visually or logically
    ///
    /// In the packet it will be written like it's an int, it just needs to be multiplied by precision to get the decimals again
    /// Treating it like an int saves one bit on the decimal
    /// </summary>
    public void WriteFloat(float value, float min, float max, float precision = 0.01f)
    {
        if (max <= min)
            throw new InvalidOperationException("Max must be greater than min");
        
        if (value < min || value > max)
            throw new ArgumentOutOfRangeException(nameof(value));
        
        // Range is always positive
        var rangeFloat = max - min;
        var rangeIntForPrecision = (int)Mathf.Ceil(rangeFloat / precision);
        var bits = CalculateBits(0, rangeIntForPrecision);
        
        // Normalized value is used to find an acceptable int value representation of the original float number
        // and we subtract the minimum so we don't have to deal with negatives like the WriteInt method
        // Why am I adding 0.5f though? I don't know, it was in the Gaffer article
        var normalized = Math.Clamp((value - min) / rangeFloat, 0f, 1f);
        var content = (uint)Math.Floor(normalized * rangeIntForPrecision + 0.5f);
        StoreOrWrite(content, bits);
    }
}