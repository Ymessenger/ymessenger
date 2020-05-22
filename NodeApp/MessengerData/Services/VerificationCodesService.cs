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
using NodeApp.CacheStorageClasses;
using NodeApp.Interfaces.Services;
using NodeApp.Objects;
using ObjectsLibrary;
using System.Threading.Tasks;

namespace NodeApp.MessengerData.Services
{
    public class VerificationCodesService : IVerificationCodesService
    {
        private readonly ICacheRepository<VerificationCodeInfo> cacheRepository;
        private const string VERIFICATIONCODE_KEY_FORMAT = "VERIFICATION_CODE:{0}";

        public VerificationCodesService(ICacheRepository<VerificationCodeInfo> cacheRepository)
        {
            this.cacheRepository = cacheRepository;
        }

        public async Task<VerificationCodeInfo> GetUserVerificationCodeAsync(string id, long? userId = null)
        {
            return await cacheRepository.GetObject(string.Format(VERIFICATIONCODE_KEY_FORMAT, userId?.ToString() ?? id)).ConfigureAwait(false);
        }

        public async Task<bool> CanRequestSmsCodeAsync(long currentTime, string id, long? userId = null)
        {
            VerificationCodeInfo vCodeInfo = await GetUserVerificationCodeAsync(id, userId).ConfigureAwait(false);
            if (vCodeInfo == null)
            {
                return true;
            }

            if (currentTime > vCodeInfo.GenerationTimeSeconds + vCodeInfo.WaitingSeconds)
            {
                return true;
            }
            return false;
        }

        public async Task<bool> IsVerificationCodeValidAsync(string id, long? userId, short verificationCode)
        { 
            VerificationCodeInfo vCodeInfo = await GetUserVerificationCodeAsync(id, userId).ConfigureAwait(false);
            if (vCodeInfo != null && vCodeInfo.VCode == verificationCode)
            {
                await cacheRepository.Remove(string.Format(VERIFICATIONCODE_KEY_FORMAT, userId?.ToString() ?? id)).ConfigureAwait(false);
                return true;
            }
            return false;
        }

        public async Task<short> CreateVerificationCodeAsync(long currentTime, string id, long? userId = null)
        {
            VerificationCodeInfo existingCode = await GetUserVerificationCodeAsync(userId?.ToString() ?? id).ConfigureAwait(false);
            VerificationCodeInfo vCodeInfo;
            if (existingCode == null)
            {
                short verificationCode;
                verificationCode = (short)RandomExtensions.NextInt32(1000, 9999);
                vCodeInfo = new VerificationCodeInfo(verificationCode, currentTime, id, 30, userId);
            }
            else
            {
                short extendedTime = (existingCode.WaitingSeconds * 2) < short.MaxValue
                    ? (short)(existingCode.WaitingSeconds * 2)
                    : existingCode.WaitingSeconds;
                vCodeInfo = new VerificationCodeInfo(existingCode.VCode, currentTime, id, extendedTime, userId);
            }
            await cacheRepository.AddObject(string.Format(VERIFICATIONCODE_KEY_FORMAT, userId?.ToString() ?? id), vCodeInfo).ConfigureAwait(false);
            return vCodeInfo.VCode;
        }
    }
}