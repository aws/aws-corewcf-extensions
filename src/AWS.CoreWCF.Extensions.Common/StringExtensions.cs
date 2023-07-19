using System.IO.Hashing;
using System.Text;

namespace AWS.CoreWCF.Extensions.Common;

public static class StringExtensions
{
    private static readonly Encoding _defaultEncoding = Encoding.UTF8;

    /// <summary>
    /// Converts a string to a Stream object using the default encoding
    /// </summary>
    /// <param name="str">String to convert to a stream</param>
    /// <returns>A stream containing the string data encoded with default encoding</returns>
    public static Stream ToStream(this string str)
    {
        return str.ToStream(_defaultEncoding);
    }

    /// <summary>
    /// Converts a string to a Stream object using a specified encoding
    /// </summary>
    /// <param name="str">String to convert to a stream</param>
    /// <param name="encoding">Encoding to apply to the string data</param>
    /// <returns>A stream containing the string data encoded with specified encoding</returns>
    public static Stream ToStream(this string str, Encoding encoding)
    {
        return new MemoryStream(encoding.GetBytes(str));
    }

    /// <summary>
    /// Hashes a string using CRC32
    /// </summary>
    /// <param name="str">String to hash</param>
    /// <returns>The hashed value encoded as a hex string</returns>
    public static string ToCrc32Hash(this string str)
    {
        var strBytes = Encoding.UTF8.GetBytes(str);
        var strHash = Crc32.Hash(strBytes);
        return BitConverter.ToString(strHash);
    }
}
