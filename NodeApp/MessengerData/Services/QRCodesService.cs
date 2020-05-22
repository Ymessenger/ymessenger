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
using Microsoft.EntityFrameworkCore;
using NodeApp.Converters;
using NodeApp.ExceptionClasses;
using NodeApp.Interfaces;
using NodeApp.MessengerData.Entities;
using ObjectsLibrary;
using ObjectsLibrary.ViewModels;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace NodeApp.MessengerData.Services
{
    public class QRCodesService : IQRCodesService
    {
        private readonly IDbContextFactory<MessengerDbContext> contextFactory;
        public QRCodesService(IDbContextFactory<MessengerDbContext> contextFactory)
        {
            this.contextFactory = contextFactory;
        }
        public async Task<QRCodeContent> CreateQRCodeAsync(long userId, long nodeId)
        {
            var sequence = RandomExtensions.NextBytes(192);
            string sequenceStr = Convert.ToBase64String(sequence);           

            QRCode qrCode = new QRCode
            {
                UserId = userId,
                SequenceHash = GetSequenceHashSha512(sequenceStr)
            };
            using (MessengerDbContext context = contextFactory.Create())
            {
                bool isUserExists = await context.Users.AnyAsync(user => user.Id == userId).ConfigureAwait(false);
                if (!isUserExists)
                {
                    throw new ObjectDoesNotExistsException($"User with Id: {userId} does not found.");
                }

                await context.QRCodes.AddAsync(qrCode).ConfigureAwait(false);
                await context.SaveChangesAsync().ConfigureAwait(false);
                return new QRCodeContent(qrCode.Id, qrCode.UserId, nodeId, sequenceStr);
            }
        }

        public async Task<TokenVm> CreateTokenByQRCodeAsync(QRCodeContent qRCodeContent,
                                                                   string deviceTokenId = null,
                                                                   string osName = null,
                                                                   string deviceName = null,
                                                                   string appName = null)
        {
            byte[] sequenceHash = GetSequenceHashSha512(qRCodeContent.Sequence);
            using (MessengerDbContext context = contextFactory.Create())
            {
                var qrCode = await context.QRCodes
                .FirstOrDefaultAsync(qr => qr.UserId == qRCodeContent.UserId && qr.SequenceHash.SequenceEqual(sequenceHash))
                .ConfigureAwait(false);
                if (qrCode == null)
                {
                    throw new ObjectDoesNotExistsException("QR-code with the data was not found.");
                }

                Token token = new Token
                {
                    AccessToken = RandomExtensions.NextString(64),
                    RefreshToken = RandomExtensions.NextString(64),
                    AccessTokenExpirationTime = DateTime.UtcNow.AddHours(TokensService.ACCESS_LIFETIME_HOUR).ToUnixTime(),
                    RefreshTokenExpirationTime = DateTime.UtcNow.AddHours(TokensService.REFRESH_LIFETIME_HOUR).ToUnixTime(),
                    AppName = appName,
                    DeviceName = deviceName,
                    OSName = osName,
                    DeviceTokenId = deviceTokenId,
                    UserId = qRCodeContent.UserId
                };
                await context.Tokens.AddAsync(token).ConfigureAwait(false);
                context.QRCodes.Remove(qrCode);
                await context.SaveChangesAsync().ConfigureAwait(false);
                return TokenConverter.GetTokenVm(token);
            }
        }
        private byte[] GetSequenceHashSha512(string sequence)
        {
            if (sequence == null)
            {
                throw new ArgumentNullException(nameof(sequence));
            }

            byte[] sequenceBytes = Convert.FromBase64String(sequence);
            using (SHA512 sha = SHA512.Create())
            {
                return sha.ComputeHash(sequenceBytes);
            }
        }
    }
}