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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NodeApp.Extensions;
using NodeApp.Helpers;
using NodeApp.HttpServer.Models;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services.Users;
using ObjectsLibrary.Interfaces;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.HttpServer.Controllers.Api
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]
    public class QRCodesController : ControllerBase
    {
        private readonly IQRCodesService _qrCodesService;
        private readonly ILoadUsersService _loadUsersService;
        private readonly IFileStorage _fileStorage;
        public QRCodesController(IQRCodesService qrCodesService, ILoadUsersService loadUsersService, IFileStorage fileStorage)
        {
            _qrCodesService = qrCodesService;
            _loadUsersService = loadUsersService;
            _fileStorage = fileStorage;
        }
        [HttpPost]
        public async Task<IActionResult> SendToEmail([FromForm] SendQRModel model)
        {
            Dictionary<string, string> errors = new Dictionary<string, string>();
            if (string.IsNullOrWhiteSpace(model.Email))
            {
                var user = await _loadUsersService.GetUserAsync(model.UserId).ConfigureAwait(false);
                if (user == null)
                {
                    errors.Add("userId", $"The user with the specified identifier was not found. ({nameof(model.UserId)} : {model.UserId})");
                }
                else
                {
                    string userEmail = user.Emails?.FirstOrDefault();
                    if (string.IsNullOrWhiteSpace(userEmail))
                    {
                        errors.Add("email", $"The email address is not specified in the request parameters and the user does not have an email address. ({nameof(model.UserId)} : {model.UserId})");
                    }
                    model.Email = userEmail;
                }
            }
            else
            {
                if (!ValidationHelper.IsEmailValid(model.Email))
                {
                    errors.Add("email", $"The specified value is not a valid email address. ({nameof(model.Email)} : {model.Email})");
                }
            }
            if (errors.Any())
            {
                return new JsonResult(errors);
            }           
            if (!string.IsNullOrWhiteSpace(model.UploadFileId))
            {
                await EmailHandler.SendEmailWithFileAsync(model.Email, await _fileStorage.GetStreamAsync(model.UploadFileId), model.UploadFileId, "QR-Code");
                await _fileStorage.RemoveAsync(model.UploadFileId);
            }
            return Ok();            
        }
        [HttpGet]
        public async Task<IActionResult> GetQRCode([FromQuery]long userId)
        {
            Dictionary<string, string> errors = new Dictionary<string, string>();            
            var user = await _loadUsersService.GetUserAsync(userId).ConfigureAwait(false);
            if(user == null)
            {
                errors.Add("userId", $"The user with the specified identifier was not found. ({nameof(userId)} : {userId})");                     
            }                
            if (errors.Any())
            {
                return new JsonResult(errors);
            }
            var qrCodeAuthData = await _qrCodesService.CreateQRCodeAsync(userId, NodeSettings.Configs.Node.Id).ConfigureAwait(false);
            QRCodeGenerator qrCodeGenerator = new QRCodeGenerator();            
            QRCodeData qrCodeData = qrCodeGenerator.CreateQrCode(qrCodeAuthData.ToJson(), QRCodeGenerator.ECCLevel.Q);   
            QRCode qRCode = new QRCode(qrCodeData);            
            Bitmap bitmap = new Bitmap("wwwroot/lock-black-256.png");                         
            var imageData = qRCode.GetGraphic(10, System.Drawing.Color.Black, System.Drawing.Color.White, bitmap);            
            string imageTempName = $"{Guid.NewGuid().ToString()}.png";            
            var newImage = new Bitmap(imageData.Width, imageData.Height + 100);           
            Graphics graphics = Graphics.FromImage(newImage);
            SolidBrush fillBrush = new SolidBrush(System.Drawing.Color.White);
            graphics.FillRectangle(fillBrush, 0,0, newImage.Width, newImage.Height);
            graphics.DrawImage(imageData, 0, 100, new Rectangle(0, 0, imageData.Width, imageData.Height), GraphicsUnit.Pixel);           
            Font drawFont = new Font("Arial", 60);
            SolidBrush drawBrush = new SolidBrush(System.Drawing.Color.Black);                
            float x = newImage.Width / 2 - 200;
            float y = 50;
            graphics.DrawString("Sign in QR", drawFont, drawBrush, x, y);
            using (Stream stream = System.IO.File.OpenWrite(imageTempName))
            {
                newImage.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            }           
            byte[] content = await System.IO.File.ReadAllBytesAsync(imageTempName);
            using (Stream stream = System.IO.File.OpenRead(imageTempName))
            {
                await _fileStorage.UploadAsync(stream, imageTempName);
            }
            System.IO.File.Delete(imageTempName);            
            return File(content, "image/png", imageTempName);
        }
    }
}