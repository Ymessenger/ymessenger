﻿/** 
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
using NodeApp.Interfaces.Services.Channels;
using NodeApp.Objects;
using ObjectsLibrary.Enums;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using ObjectsLibrary.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.RequestsHandlers
{
    public class GetChannelUsersRequestHandler : IRequestHandler
    {
        private readonly GetChannelUsersRequest request;
        private readonly ClientConnection clientConnection;
        private readonly ILoadChannelsService loadChannelsService;

        public GetChannelUsersRequestHandler(Request request, ClientConnection clientConnection, ILoadChannelsService loadChannelsService)
        {
            this.request = (GetChannelUsersRequest)request;
            this.clientConnection = clientConnection;
            this.loadChannelsService = loadChannelsService;
        }

        public async Task<Response> CreateResponseAsync()
        {
            try
            {
                var channelUsers = new List<ChannelUserVm>(
                    await loadChannelsService.GetChannelUsersAsync(
                        request.ChannelId, request.NavigationUserId, clientConnection.UserId.GetValueOrDefault()).ConfigureAwait(false));
                return new ChannelUsersResponse(
                    request.RequestId,
                    channelUsers?.Where(opt => opt.ChannelUserRole >= ChannelUserRole.Administrator && opt.Banned == false),
                    channelUsers?.Where(opt => opt.ChannelUserRole == ChannelUserRole.Subscriber && opt.Banned == false),
                    channelUsers?.Where(opt => opt.Banned == true));
            }
            catch (PermissionDeniedException ex)
            {
                Logger.WriteLog(ex, request);
                return new ResultResponse(request.RequestId, "User does not have access to the channel.", ErrorCode.PermissionDenied);
            }
        }

        public bool IsRequestValid()
        {
            if (clientConnection.UserId == null)
            {
                throw new UnauthorizedUserException();
            }

            if (!clientConnection.Confirmed)
            {
                throw new PermissionDeniedException("User is not confirmed.");
            }

            return request.ChannelId != 0;
        }
    }
}