using Microsoft.Diagnostics.Runtime;

namespace Microsoft.Diagnostics.Tools.Analyze
{
    internal static class ClrTypeExtensions
    {
        public static bool IsDerivedFrom(this ClrType self, string typeName)
        {
            while (self != null)
            {
                if (string.Equals(self.Name, typeName))
                {
                    return true;
                }
                self = self.BaseType;
            }

            return false;
        }
    }
}
