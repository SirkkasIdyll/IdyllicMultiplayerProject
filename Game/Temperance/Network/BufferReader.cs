using System;
using System.Buffers.Binary;

namespace Game.Temperance.Network;

/// <summary>
/// Intended to read from a bitpacked packet up to 32-bits at a time
/// </summary>
/// <param name="buffer">The data of the packet</param>
public ref struct BufferReader(ReadOnlySpan<byte> buffer)
{
    /// <summary>
    /// Using a Span requires we use a ref struct instead of a class
    /// Holds the bytes we want to read values from
    /// </summary>
    private readonly ReadOnlySpan<byte> _buffer = buffer;
    
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
                var bitsToInsert = (ulong)BinaryPrimitives.ReadUInt32LittleEndian(_buffer.Slice(_wordIndex * 4, 4));
                
                // Shift the bits so they don't collide with what we already have loaded in the temp buffer
                bitsToInsert <<= _bitsInTempBuffer;
                
                // Insert the bits into the temp buffer by ORing it
                // The area to the left of the bits already in the temp buffer should all be 0s,
                // so this is the same thing as just adding the two values together
                _tempBuffer |= bitsToInsert;
                _bitsInTempBuffer += 32;
                _wordIndex += 1;
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
}