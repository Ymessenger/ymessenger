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
using NodeApp.Blockchain;
using NodeApp.ExceptionClasses;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services;
using NodeApp.Interfaces.Services.Users;
using NodeApp.Objects;
using ObjectsLibrary.Blockchain.Services;
using ObjectsLibrary.Blockchain.ViewModels;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace NodeApp.RequestsHandlers
{
    public class DeleteUserRequestHandler : IRequestHandler
    {
        private readonly DeleteUserRequest request;
        private readonly ClientConnection clientConnection;
        private readonly INodeNoticeService nodeNoticeService;
        private readonly IDeleteUsersService deleteUsersService;
        private readonly IVerificationCodesService verificationCodesService;
        private readonly IConnectionsService connectionsService;

        public DeleteUserRequestHandler(
            Request request,
            ClientConnection clientConnection,
            INodeNoticeService nodeNoticeService,
            IDeleteUsersService deleteUsersService,
            IVerificationCodesService verificationCodesService,
            IConnectionsService connectionsService)
        {
            this.request = (DeleteUserRequest)request;
            this.clientConnection = clientConnection;
            this.nodeNoticeService = nodeNoticeService;
            this.deleteUsersService = deleteUsersService;
            this.verificationCodesService = verificationCodesService;
            this.connectionsService = connectionsService;
        }

        public async Task<Response> CreateResponseAsync()
        {
            try
            {
                if (await verificationCodesService.IsVerificationCodeValidAsync(clientConnection.UserId.GetValueOrDefault().ToString(), clientConnection.UserId, request.VCode).ConfigureAwait(false))
                {
                    await deleteUsersService.DeleteUserAsync(clientConnection.UserId.GetValueOrDefault()).ConfigureAwait(false);
                    nodeNoticeService.SendDeleteUsersNodeNoticeAsync(clientConnection.UserId.GetValueOrDefault());
                    BlockSegmentVm segment = await BlockSegmentsService.Instance.CreateDeleteUserSegmentAsync(
                        clientConnection.UserId.GetValueOrDefault(),
                        new List<long> { NodeSettings.Configs.Node.Id },
                        NodeSettings.Configs.Node.Id).ConfigureAwait(false);
                    BlockGenerationHelper.Instance.AddSegment(segment);
                    Task disconnectTask = new Task(async () =>
                    {
                        try
                        {
                            List<ClientConnection> clientConnections = connectionsService
                                .GetClientConnections(new List<long> { clientConnection.UserId.Value })
                                .Where(connection => connection != clientConnection)
                                .ToList();
                            foreach (var connection in clientConnections)
                            {
                                await connection.ClientSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "User deleted.", CancellationToken.None).ConfigureAwait(false);
                            }
                            await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
                            await clientConnection.ClientSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "User deleted.", CancellationToken.None).ConfigureAwait(false);
                        }
                        catch
                        {
                            return;
                        }
                    });
                    disconnectTask.Start();
                    return new ResultResponse(request.RequestId);
                }
                return new ResultResponse(request.RequestId, null, ObjectsLibrary.Enums.ErrorCode.WrongVerificationCode);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex, request);
                return new ResultResponse(request.RequestId, "Can't delete user.", ObjectsLibrary.Enums.ErrorCode.DeleteUserProblem);
            }
        }

        public bool IsRequestValid()
        {
            if (clientConnection.UserId == null)
            {
                throw new UnauthorizedUserException();
            }

            return request.VCode != 0;
        }
    }
}