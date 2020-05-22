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
using NodeApp.Interfaces;
using NodeApp.Objects;
using ObjectsLibrary;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using System.Threading.Tasks;

namespace NodeApp.RequestsHandlers
{
    public class GetRandomSequenceRequestHandler : IRequestHandler
    {
        private readonly GetRandomSequenceRequest request;
        private readonly ClientConnection clientConnection;

        public GetRandomSequenceRequestHandler(Request request, ClientConnection clientConnection)
        {
            this.request = (GetRandomSequenceRequest)request;
            this.clientConnection = clientConnection;
        }

        public async Task<Response> CreateResponseAsync()
        {            
            return new SequenceResponse(request.RequestId, RandomExtensions.NextBytes(request.Length));
        }
        public bool IsRequestValid()
        {
            return request.Length <= 10000;
        }
    }
}