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
using NodeApp.ExceptionClasses;
using NodeApp.Interfaces;
using NodeApp.LicensorClasses;
using ObjectsLibrary.Enums;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.RequestsHandlers
{
    public class VerifyNodeRequestHandler : IRequestHandler
    {
        private readonly VerifyNodeRequest request;

        public VerifyNodeRequestHandler(Request request)
        {
            this.request = (VerifyNodeRequest)request;
        }

        public async Task<Response> CreateResponseAsync()
        {
            try
            {
                byte[] encryptedSequence = await LicensorClient.Instance.VerifyNodeAsync(request.Sequence, request.EncryptedKey, request.SignPubicKey).ConfigureAwait(false);
                return new SequenceResponse(request.RequestId, encryptedSequence);
            }
            catch (ResponseException ex)
            {
                return new ResultResponse(request.RequestId, ex.Message, ErrorCode.AuthorizationProblem);
            }
            catch (LicensorException ex)
            {
                return new ResultResponse(request.RequestId, ex.Message, ErrorCode.UnknownError);
            }
        }

        public bool IsRequestValid()
        {
            return request.EncryptedKey != null && request.EncryptedKey.Any()
                && request.Sequence != null && request.Sequence.Any()
                && request.SignPubicKey != null && request.SignPubicKey.Any();
        }
    }
}