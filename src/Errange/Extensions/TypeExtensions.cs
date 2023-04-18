namespace Errange.Extensions;

internal static class TypeExtensions
{
    /// <summary>
    ///     Copied from
    ///     <see href="https://docs.microsoft.com/en-us/archive/blogs/cloudpfe/base32-encoding-and-decoding-in-c" />.
    /// </summary>
    /// <param name="type"><see cref="Type.GUID" /> is used to generate id.</param>
    /// <param name="maxLength"></param>
    /// <returns></returns>
    public static string ToBase32Id(this Type type, int maxLength = 8)
    {
        string ToBase32String(IReadOnlyCollection<byte> bytes)
        {
            const string base32AllowedCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            const char paddingChar = '0';
            string bits = bytes
                .Select(@byte => Convert
                    .ToString(@byte, toBase: 2)
                    .PadLeft(totalWidth: 8, paddingChar))
                .Aggregate((first, second) => first + second)
                .PadRight((int)(Math.Ceiling(bytes.Count * 8 / 5d) * 5), paddingChar);
            return Enumerable
                .Range(start: default, bits.Length / 5)
                .Select(index => base32AllowedCharacters.Substring(Convert.ToInt32(bits.Substring(index * 5, length: 5), fromBase: 2), length: 1))
                .Aggregate((first, second) => first + second);
        }

        return ToBase32String(type.GUID.ToByteArray()).Substring(startIndex: default, maxLength);
    }
}