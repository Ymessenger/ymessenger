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
using NodeApp.HttpServer.Models;
using NodeApp.Interfaces;
using ObjectsLibrary;
using ObjectsLibrary.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NodeApp.HttpServer.Controllers.Api
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]
    public class ConversationsController : ControllerBase
    {
        public readonly IConversationsService _conversationsService;
        public ConversationsController(IConversationsService conversationsService)
        {
            _conversationsService = conversationsService;
        }
        [HttpPost]
        public async Task<List<ConversationPreviewVm>> GetConversations([FromForm]GetConversationsModel model)
        {
            var conversations = await _conversationsService.GetUsersConversationsAsync(
                model.UserId,
                model.ConversationId,
                model.ConversationType,
                RandomExtensions.NextInt64())
                .ConfigureAwait(false);
            return conversations;
        }
    }
}