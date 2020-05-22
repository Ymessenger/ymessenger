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
using NodeApp.Converters;
using NodeApp.Extensions;
using NodeApp.HttpServer.Models;
using NodeApp.HttpServer.Models.ViewModels;
using NodeApp.Interfaces.Services.Messages;
using System.Threading.Tasks;

namespace NodeApp.HttpServer.Controllers.Api
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]
    public class MessagesController : ControllerBase
    {
        private readonly static byte messagesLimit = 30;
        private readonly ILoadMessagesService _loadMessagesService;
        public MessagesController(ILoadMessagesService loadMessagesService)
        {
            _loadMessagesService = loadMessagesService;
        }        
        [HttpPost]
        public async Task<MessagesPageViewModel> GetMessages([FromForm]GetMessagesModel model)
        {            
            MessagesPageViewModel responseModel;
            if (model.MessagesIds.IsNullOrEmpty())
            {
                responseModel = await _loadMessagesService.GetMessagesPageAsync(model.ConversationId, model.ConversationType, model.PageNumber.GetValueOrDefault(), messagesLimit);
            }
            else
            {
                var messages = await _loadMessagesService.GetMessagesByIdAsync(model.MessagesIds, model.ConversationType, model.ConversationId, null)
                    .ConfigureAwait(false);
                responseModel = new MessagesPageViewModel { Messages = MessageConverter.GetMessagesVm(messages, null) };
            }
            return responseModel;
        }
    }}