namespace AppSettingsLint.Core;

internal sealed class SourceCoordinates(byte[] source)
{
    private int offset;
    private int line = 1;
    private int column = 1;

    // The reader requests token starts in source order. Count Unicode scalars,
    // not UTF-8 bytes; tabs count as one column and LF ends a line (also in CRLF).
    public SourceLocation At(long tokenOffset)
    {
        var target = checked((int)tokenOffset);
        while (offset < target)
        {
            var value = source[offset++];
            if (value == (byte)'\n') { line++; column = 1; }
            else if ((value & 0xC0) != 0x80) column++;
        }
        return new(line, column);
    }
}
