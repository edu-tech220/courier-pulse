using System.Security.Cryptography;

namespace CourierPulse.Api.Services;

/// <summary>Generates short, public-facing tracking codes like "CP-7K3F9Q".</summary>
public static class TrackingCodeGenerator
{
    // Excludes 0/O and 1/I to avoid ambiguity when a code is read aloud or typed by hand.
    private const string Alphabet = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const int CodeLength = 7;

    public static string Generate()
    {
        var chars = new char[CodeLength];
        var randomBytes = RandomNumberGenerator.GetBytes(CodeLength);
        for (var i = 0; i < CodeLength; i++)
        {
            chars[i] = Alphabet[randomBytes[i] % Alphabet.Length];
        }

        return $"CP-{new string(chars)}";
    }
}
