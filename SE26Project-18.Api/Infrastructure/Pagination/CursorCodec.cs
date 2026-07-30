using System.Buffers.Binary;
using Microsoft.AspNetCore.WebUtilities;
using SE26Project_18.Api.Exceptions;

namespace SE26Project_18.Api.Infrastructure.Pagination;

internal static class CursorCodec
{
    private const byte Version = 1;

    private const int CursorLength = 19;

    public static string Encode(byte purpose, DateTime? timestamp, long id)
    {
        Span<byte> payload = stackalloc byte[CursorLength];
        payload[0] = Version;
        payload[1] = purpose;
        payload[2] = timestamp.HasValue ? (byte)1 : (byte)0;
        BinaryPrimitives.WriteInt64BigEndian(payload[3..11], timestamp?.Ticks ?? 0);
        BinaryPrimitives.WriteInt64BigEndian(payload[11..19], id);
        return WebEncoders.Base64UrlEncode(payload);
    }

    public static (DateTime? Timestamp, long Id) Decode(string cursor, byte purpose)
    {
        try
        {
            var payload = WebEncoders.Base64UrlDecode(cursor);
            if (
                payload.Length != CursorLength
                || payload[0] != Version
                || payload[1] != purpose
                || payload[2] > 1
            )
            {
                throw new FormatException();
            }

            var ticks = BinaryPrimitives.ReadInt64BigEndian(payload.AsSpan(3, 8));
            var id = BinaryPrimitives.ReadInt64BigEndian(payload.AsSpan(11, 8));
            if (id <= 0 || (payload[2] == 0 && ticks != 0))
            {
                throw new FormatException();
            }

            var timestamp = payload[2] == 1 ? new DateTime(ticks) : (DateTime?)null;
            return (timestamp, id);
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException)
        {
            throw new ValidationException("The pagination cursor is invalid.");
        }
    }
}
