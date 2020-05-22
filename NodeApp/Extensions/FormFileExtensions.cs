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
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using System;

namespace NodeApp.Extensions
{
    public static class FormFileExtensions
    {
        public static bool TryGetImage(this IFormFile formFile, out Image<Rgba32> image, out IImageFormat imageFormat)
        {
            try
            {
                image = Image.Load<Rgba32>(Configuration.Default, formFile.OpenReadStream(), out var format);
                imageFormat = format;
                return true;

            }
            catch (Exception)
            {
                image = null;
                imageFormat = null;
                return false;
            }
            finally
            {
                formFile.OpenReadStream().Position = 0;
            }
        }
    }
}
