using Microsoft.IdentityModel.Tokens;

namespace Fhi.ClientCredentialsKeypairs;

public static class JsonWebKeyExtensions
{
    /// <summary>
    /// Gets only the public part of a JsonWebKey
    /// https://en.wikipedia.org/wiki/RSA_%28cryptosystem%29
    /// "The public key consists of the modulus n and the public (or encryption) exponent e. The private key consists of the private (or decryption) exponent d, which must be kept secret. p, q, and λ(n) must also be kept secret because they can be used to calculate d. In fact, they can all be discarded after d has been computed.[16]"
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public static JsonWebKey GetPublicKey(this JsonWebKey key)
    {
        return new JsonWebKey()
        {
            Alg = key.Alg, // Alogrithm
            N = key.N, // modulus N
            E = key.E, // exponent E
            Kty = key.Kty, // Key type
        };
    }
}
