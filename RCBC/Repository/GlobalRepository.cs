using RCBC.Interface;

namespace RCBC.Repository
{
    public class GlobalRepository : IGlobalRepository
    {
        public bool IsStrongPassword(string password)
        {
            // Define criteria for a strong password
            int minLength = 8; // Minimum length
            int minDigitCount = 1; // Minimum number of digits
            int minUpperCount = 1; // Minimum number of uppercase letters
            int minLowerCount = 1; // Minimum number of lowercase letters
            int minSpecialCount = 1; // Minimum number of special characters
            string specialCharacters = @"!@#$%^&*()_+[]{}|;:,.<>?";

            // Check if the password meets the criteria
            if (password.Length < minLength) return false;
            if (password.Count(char.IsDigit) < minDigitCount) return false;
            if (password.Count(char.IsUpper) < minUpperCount) return false;
            if (password.Count(char.IsLower) < minLowerCount) return false;
            if (password.Count(c => specialCharacters.Contains(c)) < minSpecialCount) return false;

            // You can add more complex checks as needed, such as checking for common passwords, dictionary words, or patterns.

            return true;
        }
    }
}
