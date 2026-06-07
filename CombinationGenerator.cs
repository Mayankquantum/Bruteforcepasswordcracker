using System;
using System.Collections.Generic;

namespace code;

// Brute-force GENERATOR. Its only job: produce candidate strings.
// It does NOT hash and does NOT compare — that independence is the requirement.
// It also does NOT know the real password length. The caller drives the length.
public class CombinationGenerator
{
    // Yields every possible string of exactly `length` characters,
    // built from the shared Charset. Lazy (yield) so we don't hold
    // millions of strings in memory at once.
    public IEnumerable<string> Generate(int length)
    {
        // odometer: an array of indices into Charset, all starting at 0
        int[] indices = new int[length];

        while (true)
        {
            // build the current candidate from the index array
            char[] candidate = new char[length];
            for (int i = 0; i < length; i++)
                candidate[i] = Charset.Characters[indices[i]];
            yield return new string(candidate);

            // increment the odometer: rightmost wheel first, carry left
            int pos = length - 1;
            while (pos >= 0)
            {
                indices[pos]++;
                if (indices[pos] < Charset.Length)
                    break;              // no carry needed, done
                indices[pos] = 0;       // wheel rolled over
                pos--;                  // carry to the wheel on the left
            }
            if (pos < 0)
                yield break;            // all wheels rolled over → exhausted this length
        }
    }
}
