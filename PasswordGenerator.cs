using System;

namespace code;

public class PasswordGenerator
{
    private readonly Random _rng = new();

    public string Generate()
    {
        // length: 4 or 5. Next(4, 6) returns 4 or 5 (6 is exclusive).
        int length = _rng.Next(4, 6);

        char[] chars = new char[length];
        for (int i = 0; i < length; i++)
        {
            int index = _rng.Next(Charset.Length);
            chars[i] = Charset.Characters[index];
        }
        return new string(chars);
    }
}
