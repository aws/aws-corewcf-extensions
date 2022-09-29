using System.Text;

namespace AWS.CoreWCF.Server.Common;

public static class StringExtensions
{
    private static readonly Encoding DefaultEncoding = Encoding.UTF8;

    /// <summary>
    /// Converts a string to a Stream object using the default encoding
    /// </summary>
    /// <param name="str">String to convert to a stream</param>
    /// <returns>A stream containing the string data encoded with default encoding</returns>
    public static Stream ToStream(this string str)
    {
        return str.ToStream(DefaultEncoding);
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
}