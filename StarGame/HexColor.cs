using System;
using System.Globalization;
using Raylib_cs;

namespace StarflightGame;

/// <summary>
/// Parses CSS-style <c>#RRGGBB</c> hex strings to Raylib <see cref="Color"/> with alpha 255.
/// </summary>
public static class HexColor
{
    public static Color ToRaylibColor(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            throw new ArgumentException("Hex color cannot be null or empty.", nameof(hex));
        }

        ReadOnlySpan<char> s = hex.AsSpan().Trim();
        if (s.Length > 0 && s[0] == '#')
        {
            s = s[1..];
        }

        if (s.Length != 6)
        {
            throw new ArgumentException($"Expected #RRGGBB (6 hex digits), got: {hex}", nameof(hex));
        }

        byte r = ParseByte(s.Slice(0, 2));
        byte g = ParseByte(s.Slice(2, 2));
        byte b = ParseByte(s.Slice(4, 2));
        return new Color(r, g, b, (byte)255);
    }

    private static byte ParseByte(ReadOnlySpan<char> twoHexDigits)
    {
        if (!byte.TryParse(twoHexDigits, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte value))
        {
            throw new ArgumentException($"Invalid hex byte: {twoHexDigits.ToString()}");
        }

        return value;
    }
}
