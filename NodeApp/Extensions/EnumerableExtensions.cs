/** 
  *    This file is part of Y messenger.
  *
  *    Y messenger is free software: you can redistribute it and/or modify
  *    it under the terms of the GNU Affero Public License as published by
  *    the Free Software Foundation, either version 3 of the License, or
  *    (at your option) any later version.
  *
  *    Y messenger is distributed in the hope that it will be useful,
  *    but WITHOUT ANY WARRANTY; without even the implied warranty of
  *    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  *    GNU Affero Public License for more details.
  *
  *    You should have received a copy of the GNU Affero Public License
  *    along with Y messenger.  If not, see <https://www.gnu.org/licenses/>.
  */
using System;
using System.Collections.Generic;
using System.Linq;

namespace NodeApp.Extensions
{
    public static class EnumerableExtensions
    {
        public static string AsString<T>(this IEnumerable<T> collection, string separator, Func<T, string> toStringFunc = null)
        {
            string result = string.Empty;
            if (collection != null && collection.Any())
            {
                foreach (T value in collection)
                {
                    if (toStringFunc != null)
                    {
                        result += $"{toStringFunc.Invoke(value)}";
                    }
                    else
                    {
                        result += $"{value.ToString()}";
                    }

                    if (!collection.LastOrDefault().Equals(value))
                    {
                        result += separator;
                    }
                }
            }

            return result;
        }
        public static string AsString<T>(this IEnumerable<T> collection, string separator)
        {
            string result = string.Empty;
            if (collection != null && collection.Any())
            {
                foreach (T value in collection)
                {
                    result += $"{value.ToString()}";
                    if (!collection.LastOrDefault().Equals(value))
                    {
                        result += separator;
                    }
                }
            }

            return result;
        }
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> collection)
        {
            return collection == null || !collection.Any();
        }
    }
}
