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
using Microsoft.AspNetCore.Mvc;
using NodeApp.Blockchain;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services;
using NodeApp.Interfaces.Services.Users;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.MessengerData.Entities;
using ObjectsLibrary.Blockchain.Services;
using ObjectsLibrary.Blockchain.ViewModels;
using ObjectsLibrary.Converters;
using ObjectsLibrary.Encryption;
using ObjectsLibrary.Exceptions;
using System;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

namespace NodeApp.HttpServer.Controllers
{
    public class UserMigrationController : Controller
    {
        private readonly INodeNoticeService nodeNoticeService;
        private readonly IConnectionsService connectionsService;
        private readonly INoticeService noticeService;
        private readonly IChangeNodeOperationsService changeNodeOperationsService;
        private readonly ILoadUsersService loadUsersService;
        private readonly IUpdateUsersService updateUsersService;
        private readonly IDeleteUsersService deleteUsersService;
        private readonly INodeRequestSender nodeRequestSender;
        public UserMigrationController(
            INodeNoticeService nodeNoticeService,
            IConnectionsService connectionsService,
            INoticeService noticeService,
            IChangeNodeOperationsService changeNodeOperationsService,
            ILoadUsersService loadUsersService,
            IUpdateUsersService updateUsersService,
            IDeleteUsersService deleteUsersService,
            INodeRequestSender nodeRequestSender)
        {
            this.nodeNoticeService = nodeNoticeService;
            this.connectionsService = connectionsService;
            this.noticeService = noticeService;
            this.changeNodeOperationsService = changeNodeOperationsService;
            this.loadUsersService = loadUsersService;
            this.updateUsersService = updateUsersService;
            this.deleteUsersService = deleteUsersService;
            this.nodeRequestSender = nodeRequestSender;
        }
        [HttpGet]
        public async Task<IActionResult> Download([FromHeader] string encryptedOperationId, [FromHeader] string nodeId)
        {
            try
            {
                byte[] encryptedRequestData = Convert.FromBase64String(encryptedOperationId);
                var nodeConnection = connectionsService.GetNodeConnection(Convert.ToInt64(nodeId));
                if (nodeConnection != null)
                {
                    byte[] decryptedData = Encryptor.SymmetricDataDecrypt(
                        encryptedRequestData,
                        nodeConnection.SignPublicKey,
                        nodeConnection.SymmetricKey,
                        NodeData.Instance.NodeKeys.Password).DecryptedData;
                    string operationId = Encoding.UTF8.GetString(decryptedData);
                    if (!string.IsNullOrWhiteSpace(operationId))
                    {                        
                        long userId = await changeNodeOperationsService.GetOperationUserIdAsync(operationId, nodeConnection.Node.Id).ConfigureAwait(false);
                        UserDto userInfo = await loadUsersService.GetAllUserDataAsync(userId).ConfigureAwait(false);
                        byte[] encryptedData = Encryptor.SymmetricDataEncrypt(
                            ObjectSerializer.ObjectToByteArray(userInfo),
                            NodeData.Instance.NodeKeys.SignPrivateKey,
                            nodeConnection.SymmetricKey,
                            ObjectsLibrary.Enums.MessageDataType.Binary,
                            NodeData.Instance.NodeKeys.Password);
                        return File(encryptedData, MediaTypeNames.Application.Octet);
                    }
                }
                return StatusCode(StatusCodes.Status400BadRequest, "OperationId header is missed.");
            }
            catch(CryptographicException)
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }
            catch (ArgumentException ex)
            {
                Logger.WriteLog(ex);
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }               
        }

        [HttpGet]
        public async Task<IActionResult> StartDownloadingUserData([FromHeader] string operationId, [FromHeader] string nodeId)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(operationId))
                {
                    UserDto userInfo = await nodeRequestSender.DownloadUserDataAsync(operationId, Convert.ToInt64(nodeId)).ConfigureAwait(false);
                    bool saveResult = await changeNodeOperationsService.SaveUserDataAsync(userInfo).ConfigureAwait(false);
                    var nodeConnection = connectionsService.GetNodeConnection(Convert.ToInt64(nodeId));
                    if (saveResult && nodeConnection != null)
                    {
                        string requestUri = $"https://{nodeConnection.Uri.Authority}/UserMigration/OperationCompleted";
                        HttpWebRequest request = WebRequest.CreateHttp(requestUri);
                        byte[] encryptedRequestData = Encryptor.SymmetricDataEncrypt(
                            Encoding.UTF8.GetBytes(operationId),
                            NodeData.Instance.NodeKeys.SignPrivateKey,
                            nodeConnection.SymmetricKey,
                            ObjectsLibrary.Enums.MessageDataType.Binary,
                            NodeData.Instance.NodeKeys.Password);
                        request.Headers.Add("encryptedOperationId", Convert.ToBase64String(encryptedRequestData));
                        request.Headers.Add("nodeId", NodeSettings.Configs.Node.Id.ToString());
                        await request.GetResponseAsync().ConfigureAwait(false);
                        return StatusCode(StatusCodes.Status200OK);
                    }
                    else
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError);
                    }
                }
                else
                {
                    return StatusCode(StatusCodes.Status400BadRequest, "Header is missed.");
                }
            }
            catch(Exception ex)
            {
                Logger.WriteLog(ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet]
        public async Task<IActionResult> OperationCompleted([FromHeader]string encryptedOperationId, [FromHeader]string nodeId)
        {
            try
            {
                var nodeConnection = connectionsService.GetNodeConnection(Convert.ToInt64(nodeId));
                if (nodeConnection != null)
                {
                    byte[] decryptedRequestData = Encryptor.SymmetricDataDecrypt(
                        Convert.FromBase64String(encryptedOperationId),
                        nodeConnection.SignPublicKey,
                        nodeConnection.SymmetricKey,
                        NodeData.Instance.NodeKeys.Password).DecryptedData;
                    string operationId = Encoding.UTF8.GetString(decryptedRequestData);
                    ChangeUserNodeOperation operation = await changeNodeOperationsService.CompleteOperationAsync(operationId, nodeConnection.Node.Id).ConfigureAwait(false);
                    await updateUsersService.EditUserNodeAsync(operation.UserId, nodeConnection.Node.Id).ConfigureAwait(false);
                    await deleteUsersService.DeleteUserInformationAsync(operation.UserId).ConfigureAwait(false);                    
                    nodeNoticeService.SendUserNodeChangedNodeNoticeAsync(operation.UserId, nodeConnection.Node.Id);
                    noticeService.SendUserNodeChangedNoticeAsync(operation.UserId, nodeConnection.Node.Id);
                    BlockSegmentVm segment = await BlockSegmentsService.Instance.CreateUserNodeChangedSegmentAsync(
                        operation.UserId, 
                        nodeConnection.Node.Id, 
                        NodeSettings.Configs.Node.Id).ConfigureAwait(false);
                    BlockGenerationHelper.Instance.AddSegment(segment);
                    return StatusCode(StatusCodes.Status200OK);
                }
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            catch (CryptographicException)
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }
            catch(Exception ex)
            {
                Logger.WriteLog(ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}