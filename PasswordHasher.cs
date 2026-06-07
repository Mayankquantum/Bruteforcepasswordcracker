using System.Security.Cryptography;
using System.Text;

namespace code;

public class PasswordHasher
{
    private const string Salt = "VU_OOP_2026_STATIC_SALT";

    public string Hash(string input)
    {
        string salted = Salt + input;
        byte[] bytes = Encoding.UTF8.GetBytes(salted);
        byte[] hash = SHA256.HashData(bytes);

        var sb = new StringBuilder();
        foreach (byte b in hash)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}
