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
using NodeApp.Blockchain;
using NodeApp.HttpServer.Models;
using NodeApp.HttpServer.Models.ViewModels;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services;
using NodeApp.Interfaces.Services.Users;
using NodeApp.Objects;
using ObjectsLibrary.Blockchain.Services;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace NodeApp.HttpServer.Controllers
{
    [Authorize]
    public class UsersController : Controller
    {
        private readonly IConnectionsService _connectionsService;
        private readonly INodeNoticeService _nodeNoticeService;
        private readonly ILoadUsersService _loadUsersService;
        private readonly ICreateUsersService _createUsersService;
        private readonly IUpdateUsersService _updateUsersService;
        private readonly IQRCodesService _qrCodeService;
        public UsersController(
            IConnectionsService connectionsService,
            INodeNoticeService nodeNoticeService,
            ILoadUsersService loadUsersService,
            ICreateUsersService createUsersService,
            IUpdateUsersService updateUsersService,
            IQRCodesService codesService)
        {
            _connectionsService = connectionsService;
            _nodeNoticeService = nodeNoticeService;
            _loadUsersService = loadUsersService;
            _createUsersService = createUsersService;
            _updateUsersService = updateUsersService;
            _qrCodeService = codesService;
        }
        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] long? navigationId)
        {
            var confirmedUsers = await _loadUsersService.GetUsersAsync(null, 100, navigationId.GetValueOrDefault(), true).ConfigureAwait(false);
            var unconfirmedUsers = await _loadUsersService.GetUsersAsync(null, 100, navigationId.GetValueOrDefault(), false).ConfigureAwait(false);
            return View(new UsersViewModel(confirmedUsers, unconfirmedUsers));
        }
        [HttpGet]
        public IActionResult Connected()
        {
            List<ClientConnection> clientConnections = new List<ClientConnection>();
            var connections = _connectionsService.GetClientConnections();
            clientConnections.AddRange(connections.Where(opt => opt.ClientSocket?.State == WebSocketState.Open || opt.IsProxiedClientConnection));
            return View(new ConnectedUsersViewModel(clientConnections));
        }
        [HttpGet]
        public IActionResult Disconnect([FromQuery]long sessionId, [FromQuery]long userId)
        {
            var userConnections = _connectionsService.GetUserClientConnections(userId);
            var removingConnection = userConnections?.FirstOrDefault(connection => connection.CurrentToken?.Id == sessionId);
            if (removingConnection != null)
            {
                removingConnection.ClientSocket.Abort();
            }

            return Redirect(nameof(Connected));
        }
        [HttpGet]
        public IActionResult Create()
        {
            return PartialView();
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] CreateUserModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _createUsersService.CreateNewUserAsync(
                    new UserVm
                    {
                        NameFirst = model.NameFirst,
                        NameSecond = model.NameSecond,
                        Phones = !string.IsNullOrEmpty(model.PhoneNumber)
                            ? new List<UserPhoneVm>
                            {
                                new UserPhoneVm
                                {
                                    FullNumber = model.PhoneNumber,
                                    IsMain = true
                                }
                            }
                            : null,
                        Emails = !string.IsNullOrEmpty(model.Email)
                            ? new List<string> { model.Email }
                            : null
                    },
                    NodeSettings.Configs.Node.Id,
                    NodeSettings.Configs.ConfirmUsers,
                    model.Password).ConfigureAwait(false);
                var segment = await BlockSegmentsService.Instance.CreateNewUserSegmentAsync(
                    result.FirstValue,
                    NodeSettings.Configs.Node.Id,
                    NodeData.Instance.NodeKeys.SignPrivateKey,
                    NodeData.Instance.NodeKeys.SymmetricKey,
                    NodeData.Instance.NodeKeys.Password,
                    NodeData.Instance.NodeKeys.KeyId).ConfigureAwait(false);
                BlockGenerationHelper.Instance.AddSegment(segment);
                ShortUser shortUser = new ShortUser
                {
                    UserId = result.FirstValue.Id.GetValueOrDefault(),
                    PrivateData = segment.PrivateData
                };
                _nodeNoticeService.SendNewUsersNodeNoticeAsync(shortUser, segment);
                return Redirect(nameof(Index));
            }
            return BadRequest();
        }
        [HttpGet]
        public async Task<IActionResult> Details([FromQuery]long userId)
        {
            var user = await _loadUsersService.GetUserInformationAsync(userId).ConfigureAwait(false);
            if (user != null)
            {
                return PartialView(user);
            }

            return NotFound();
        }
        [HttpGet]
        public async Task<IActionResult> Confirm([FromQuery] long userId)
        {
            try
            {
                await _updateUsersService.SetUsersConfirmedAsync(new List<long> { userId }).ConfigureAwait(false);
                var clientConnections = _connectionsService.GetUserClientConnections(userId);
                clientConnections?.ForEach(connection => connection.Confirmed = true);
                return Redirect(nameof(Index));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        [HttpGet]
        public IActionResult CreateQR([FromQuery]long userId)
        {
            return PartialView(userId);
        }

        [HttpPost]
        public async Task<IActionResult> GetQr([FromForm]long userId)
        {
            var qrCode = await _qrCodeService.CreateQRCodeAsync(userId, NodeSettings.Configs.Node.Id).ConfigureAwait(false);
            return Json(qrCode);
        }
        [HttpGet]
        public IActionResult Conversations([FromQuery]long userId)
        {
            return PartialView(userId);
        }        
    }
}