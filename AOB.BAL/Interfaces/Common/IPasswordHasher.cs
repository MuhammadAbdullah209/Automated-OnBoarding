namespace AOB.BAL.Interfaces.Common
{
    /// <summary>
    /// Hashes and verifies passwords. Abstracted so the algorithm can change without touching services.
    /// </summary>
    public interface IPasswordHasher
    {
        /// <summary>Returns a salted hash suitable for storing in the database.</summary>
        string Hash(string password);

        /// <summary>Checks a plain-text password against a stored hash.</summary>
        bool Verify(string password, string storedHash);

        /// <summary>True when the stored hash was made with weaker settings than <see cref="Hash"/> uses now.</summary>
        bool NeedsRehash(string storedHash);

        /// <summary>Does the work of a verification without a stored hash, so an unknown account takes as long as a known one.</summary>
        void SpendVerifyTime(string password);
    }
}
