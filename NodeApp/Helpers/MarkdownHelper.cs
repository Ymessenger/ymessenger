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
using System.Text.RegularExpressions;

namespace NodeApp.Helpers
{
    public static class MarkdownHelper
    {
        public static bool ContainsMarkdownUserCalling(string text, long id)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }
            string expression = @"\w*\[UserCalling\]\(userId=(?<userId>\d+)\)\w*";
            Regex regex = new Regex(expression);
            MatchCollection matches = regex.Matches(text);
            foreach (Match match in matches)
            {
                if (long.TryParse(match.Groups["userId"].Value, out long userId))
                {
                    if (userId == id)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
