using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace BetaSharp.Util;

/// <summary>
/// Provides methods for encoding and decoding strings using the Modified UTF-8 encoding format.
/// </summary>
/// <remarks>
/// Modified UTF-8 is a variation of the standard UTF-8 encoding used in NBT.
/// This encoding differs from standard UTF-8 in that it uses two-byte sequences
/// to encode the null character (U+0000) instead of the single byte used in standard UTF-8.
/// </remarks>
internal static class ModifiedUtf8
{
    /// <summary>
    /// Encodes a given string into a byte array using a modified UTF-8 encoding.
    /// </summary>
    /// <param name="str">The string to be encoded into a modified UTF-8 byte array.</param>
    /// <returns>A byte array containing the modified UTF-8 encoded representation of the input string.</returns>
    /// <exception cref="FormatException">Thrown when the encoded string length exceeds the maximum allowable byte size.</exception>
    public static byte[] GetBytes(string str)
    {
        if (string.IsNullOrEmpty(str))
            return [];

        int byteCount = GetByteCount(str);

        switch (byteCount)
        {
            case > ushort.MaxValue:
                throw new FormatException($"Encoded string length ({byteCount} bytes) exceeds the NBT maximum of {ushort.MaxValue} bytes.");
            case 0:
                return [];
        }

        byte[] bytes = new byte[byteCount];
        GetBytesInternal(str, bytes);
        return bytes;
    }

    /// <summary>
    /// Decodes a byte array encoded in the Modified UTF-8 format into its string representation.
    /// </summary>
    /// <param name="bytes">The byte span containing the Modified UTF-8 encoded data to decode.</param>
    /// <returns>A string that represents the decoded value from the Modified UTF-8 byte array.</returns>
    /// <exception cref="FormatException">Thrown when the input byte array contains invalid Modified UTF-8 data.</exception>
    public static string GetString(ReadOnlySpan<byte> bytes)
    {
        if (TryGetString(bytes, out string? result))
        {
            return result;
        }
        throw new FormatException("Input data contained invalid Modified UTF-8 bytes.");
    }

    /// <summary>
    /// Tries to decode a sequence of bytes encoded using the Modified UTF-8 format into a string.
    /// </summary>
    /// <param name="bytes">The span of bytes to decode using the Modified UTF-8 format.</param>
    /// <param name="value">When this method returns, contains the decoded string if the operation was successful, or null if it failed.</param>
    /// <returns>True if the byte sequence was successfully decoded into a string; otherwise, false.</returns>
    public static bool TryGetString(ReadOnlySpan<byte> bytes, [NotNullWhen(true)] out string? value)
    {
        if (bytes.IsEmpty)
        {
            value = string.Empty;
            return true;
        }

        if (!TryGetCharCount(bytes, out int charCount))
        {
            value = null;
            return false;
        }

        if (charCount == 0)
        {
            value = string.Empty;
            return true;
        }

        value = string.Create(charCount, bytes, (chars, stateBytes) =>
        {
            GetStringInternal(stateBytes, chars);
        });

        return true;
    }

    /// <summary>
    /// Gets the byte count for the given input string.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetByteCount(ReadOnlySpan<char> str)
    {
        ref char start = ref MemoryMarshal.GetReference(str);
        nint length = str.Length;
        nint i = 0;
        nint byteCount = 0;

        if (Vector256.IsHardwareAccelerated && length >= Vector256<ushort>.Count)
        {
            var asciiMask = Vector256.Create((ushort)0xFF80);
            for (; i <= length - Vector256<ushort>.Count; i += Vector256<ushort>.Count)
            {
                Vector256<ushort> v = Vector256.LoadUnsafe(ref Unsafe.As<char, ushort>(ref start), (nuint)i);
                if ((v & asciiMask) != Vector256<ushort>.Zero)
                    goto Scalar;
            }
            byteCount = i;
        }

    Scalar:
        for (; i < length; i++)
        {
            char c = Unsafe.Add(ref start, i);
            byteCount += c == 0 ? 2 : c <= 0x7F ? 1 : c <= 0x7FF ? 2 : 3;
        }
        return (int)byteCount;
    }

    /// <summary>
    /// Internal implementation of GetBytes that writes to a pre-allocated span.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void GetBytesInternal(ReadOnlySpan<char> str, Span<byte> bytes)
    {
        int position = 0;

        foreach (char c in str)
        {
            if (c == 0)
            {
                bytes[position++] = 0xC0;
                bytes[position++] = 0x80;
            }
            else switch (c)
                {
                    case >= '\u0001' and <= '\u007F':
                        bytes[position++] = (byte)c;
                        break;
                    case <= '\u07FF':
                        bytes[position++] = (byte)(0xC0 | ((c >> 6) & 0x1F));
                        bytes[position++] = (byte)(0x80 | (c & 0x3F));
                        break;
                    default:
                        bytes[position++] = (byte)(0xE0 | ((c >> 12) & 0x0F));
                        bytes[position++] = (byte)(0x80 | ((c >> 6) & 0x3F));
                        bytes[position++] = (byte)(0x80 | (c & 0x3F));
                        break;
                }
        }
    }

    /// <summary>
    /// Tries to get the character count for a Modified UTF-8 byte span.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryGetCharCount(ReadOnlySpan<byte> bytes, out int charCount)
    {
        charCount = 0;
        int index = 0;

        while (index < bytes.Length)
        {
            byte firstByte = bytes[index];

            if ((firstByte & 0x80) == 0)
            {
                if (firstByte == 0)
                {
                    return false;
                }
                index++;
            }
            else if ((firstByte & 0xE0) == 0xC0)
            {
                if (index + 1 >= bytes.Length) return false;
                byte secondByte = bytes[index + 1];
                if ((secondByte & 0xC0) != 0x80) return false;

                int val = ((firstByte & 0x1F) << 6) | (secondByte & 0x3F);
                if (val < 0x80 && val != 0) return false;

                index += 2;
            }
            else if ((firstByte & 0xF0) == 0xE0)
            {
                if (index + 2 >= bytes.Length) return false;
                byte secondByte = bytes[index + 1];
                byte thirdByte = bytes[index + 2];
                if ((secondByte & 0xC0) != 0x80 || (thirdByte & 0xC0) != 0x80) return false;

                int val = ((firstByte & 0x0F) << 12) | ((secondByte & 0x3F) << 6) | (thirdByte & 0x3F);
                if (val < 0x800) return false;

                index += 3;
            }
            else
            {
                return false;
            }
            charCount++;
        }
        return true;
    }

    /// <summary>
    /// Internal implementation of GetString that decodes bytes into a character span.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void GetStringInternal(ReadOnlySpan<byte> bytes, Span<char> destination)
    {
        int byteIndex = 0;
        int charIndex = 0;

        while (byteIndex < bytes.Length)
        {
            byte firstByte = bytes[byteIndex];
            char c;

            if ((firstByte & 0x80) == 0)
            {
                c = (char)firstByte;
                byteIndex++;
            }
            else if ((firstByte & 0xE0) == 0xC0)
            {
                byte secondByte = bytes[byteIndex + 1];
                c = (char)(((firstByte & 0x1F) << 6) | (secondByte & 0x3F));
                byteIndex += 2;
            }
            else
            {
                byte secondByte = bytes[byteIndex + 1];
                byte thirdByte = bytes[byteIndex + 2];
                c = (char)(((firstByte & 0x0F) << 12) | ((secondByte & 0x3F) << 6) | (thirdByte & 0x3F));
                byteIndex += 3;
            }
            destination[charIndex++] = c;
        }
    }
}
