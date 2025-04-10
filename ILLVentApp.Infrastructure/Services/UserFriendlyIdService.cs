using System;
using System.Linq;
using System.Text;
using ILLVentApp.Domain.Interfaces;

namespace ILLVentApp.Infrastructure.Services
{
    public class UserFriendlyIdService : IUserFriendlyIdService
    {
        private const string Prefix = "USR-";
        private const int CodeLength = 4;
        private const int ChecksumLength = 4;
        private static readonly Random Random = new Random();
        private const string ValidChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Excluded I, O, 0, 1 for clarity

        public string GenerateUserId()
        {
            // Generate random alphanumeric code
            var code = new string(Enumerable.Repeat(ValidChars, CodeLength)
                .Select(s => s[Random.Next(s.Length)]).ToArray());

            // Generate checksum
            var checksum = GenerateChecksum(code);

            return $"{Prefix}{code}-{checksum}";
        }

        public bool ValidateUserId(string userId)
        {
            if (string.IsNullOrEmpty(userId) || !userId.StartsWith(Prefix))
                return false;

            var parts = userId.Split('-');
            if (parts.Length != 3)
                return false;

            var code = parts[1];
            var checksum = parts[2];

            if (code.Length != CodeLength || checksum.Length != ChecksumLength)
                return false;

            // Validate checksum
            return checksum == GenerateChecksum(code);
        }

        private string GenerateChecksum(string code)
        {
            // Simple checksum calculation
            int sum = code.Sum(c => c);
            return (sum % 10000).ToString("D4");
        }
    }
} 