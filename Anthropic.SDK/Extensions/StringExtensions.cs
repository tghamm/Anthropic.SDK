using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anthropic.SDK.Extensions
{
    internal static class StringExtensions
    {
        public static string ToSnakeCase(string @string)
            => string.IsNullOrEmpty(@string)
                ? @string
                : string.Concat(
                    @string.Select((x, i) => i > 0 && char.IsUpper(x)
                        ? $"_{x}"
                        : x.ToString())).ToLower();
    }
}
