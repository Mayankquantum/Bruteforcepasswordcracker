namespace code;

// VALIDATOR. Its only job: check if a candidate matches the target hash.
// It does NOT generate candidates. Fully independent of the generator.
public class PasswordValidator
{
    private readonly PasswordHasher _hasher = new();

    public bool IsMatch(string candidate, string targetHash)
    {
        return _hasher.Hash(candidate) == targetHash;
    }
}
