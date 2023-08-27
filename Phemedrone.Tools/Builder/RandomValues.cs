using System;
using System.Linq;

namespace Phemedrone.Tools.Builder
{
    public static class RandomValues
    {
        private const string Consonants = "bcdfghjklmnprstvxz";
        private const string Vowels = "aeiouy";

        private static readonly Random Rnd = new Random();
        
        public static string RandomString(int length)
        {
            var useReverse = Rnd.Next(0, 2) == 1;

            var ch = useReverse ? Vowels[Rnd.Next(Vowels.Length)] : Consonants[Rnd.Next(Consonants.Length)];
            var result = ch.ToString().ToUpper();

            for (var i = 0; i < length; i++)
            {
                var vowel = Vowels[Rnd.Next(Vowels.Length)];
                var consonant = Consonants[Rnd.Next(Consonants.Length)];
                    
                if (useReverse)
                {
                    (vowel, consonant) = (consonant, vowel);
                }
                    
                result += vowel;
                result += consonant;
            }

            return result;
        }
        
        public static string GenerateKey()
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, 16).Select(x => x[Rnd.Next(x.Length)]).ToArray());
        }
    }
}