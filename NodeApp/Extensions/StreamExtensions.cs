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
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NodeApp.Extensions
{
    public static class StreamExtensions
    {
        public static byte[] ReadAllBytes(this Stream stream)
        {
            int readedCount = 0;
            int bufferLength = 1024;
            byte[] buffer = new byte[bufferLength];
            List<byte> result = new List<byte>();
            using (BinaryReader reader = new BinaryReader(stream))
            {
                while ((readedCount = reader.Read(buffer, 0, bufferLength)) > 0)
                {
                    result.AddRange(buffer.Take(readedCount));
                }
            }
            return result.ToArray();
        }
    }
}
