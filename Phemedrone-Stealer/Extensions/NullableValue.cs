using System;

namespace Phemedrone.Extensions
{
    public class NullableValue
    {
        // to minimize runtime errors i used this method that returns default value
        // for generic type when an error occurs
        public static T Call<T>(Func<T> method)
        {
            try
            {
                return method();
            }
            catch
            {
                return default;
            }
        }
    }
}