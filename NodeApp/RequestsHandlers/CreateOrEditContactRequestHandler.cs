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
using NodeApp.Converters;
using NodeApp.ExceptionClasses;
using NodeApp.Interfaces;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.Objects;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using System.Threading.Tasks;

namespace NodeApp.RequestsHandlers
{
    public class CreateOrEditContactRequestHandler : IRequestHandler
    {
        private readonly CreateOrEditContactRequest request;
        private readonly ClientConnection clientConnection;
        private readonly IContactsService contactsService;

        public CreateOrEditContactRequestHandler(Request request, ClientConnection clientConnection, IContactsService contactsService)
        {
            this.request = (CreateOrEditContactRequest) request;
            this.clientConnection = clientConnection;
            this.contactsService = contactsService;
        }

        public async Task<Response> CreateResponseAsync()
        {
            try
            {
                ContactDto resultContact = await contactsService.CreateOrEditContactAsync(
                    ContactConverter.GetContactDto(request.Contact, clientConnection.UserId.GetValueOrDefault())).ConfigureAwait(false);
                return new ContactsResponse(request.RequestId, ContactConverter.GetContactVm(resultContact));
            }
            catch(ObjectDoesNotExistsException ex)
            {
                Logger.WriteLog(ex);
                return new ResultResponse(request.RequestId, "User not found.", ObjectsLibrary.Enums.ErrorCode.ObjectDoesNotExists);
            }
        }

        public bool IsRequestValid()
        {
            if (clientConnection.UserId == null)
                throw new UnauthorizedUserException();
            if (!clientConnection.Confirmed)
                throw new PermissionDeniedException("User is not confirmed.");
            return request.Contact != null;
        }
    }
}