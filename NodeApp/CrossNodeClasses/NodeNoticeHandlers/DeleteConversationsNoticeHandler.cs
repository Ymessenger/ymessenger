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
using NodeApp.CrossNodeClasses.Notices;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services.Channels;
using NodeApp.Interfaces.Services.Chats;
using NodeApp.Interfaces.Services.Dialogs;
using NodeApp.MessengerData.Services;
using ObjectsLibrary;
using ObjectsLibrary.Blockchain.Services;
using ObjectsLibrary.Blockchain.ViewModels;
using ObjectsLibrary.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.CrossNodeClasses.NodeNoticeHandlers
{
    public class DeleteConversationsNoticeHandler : ICommunicationHandler
    {
        private readonly DeleteConversationsNodeNotice notice;
        private readonly IDeleteChatsService deleteChatsService;
        private readonly ILoadChatsService loadChatsService;
        private readonly ILoadDialogsService loadDialogsService;
        private readonly IDeleteDialogsService deleteDialogsService;
        private readonly ILoadChannelsService loadChannelsService;
        private readonly IDeleteChannelsService deleteChannelsService;

        public DeleteConversationsNoticeHandler(
            CommunicationObject @object,
            IDeleteChatsService deleteChatsService,
            ILoadChatsService loadChatsService,
            ILoadDialogsService loadDialogsService,
            IDeleteDialogsService deleteDialogsService,
            ILoadChannelsService loadChannelsService,
            IDeleteChannelsService deleteChannelsService)
        {
            notice = (DeleteConversationsNodeNotice)@object;
            this.deleteChatsService = deleteChatsService;
            this.loadChatsService = loadChatsService;
            this.loadDialogsService = loadDialogsService;
            this.deleteDialogsService = deleteDialogsService;
            this.loadChannelsService = loadChannelsService;
            this.deleteChannelsService = deleteChannelsService;
        }
        public async Task HandleAsync()
        {
            switch (notice.ConversationType)
            {
                case ConversationType.Dialog:
                    {
                        List<long> dialogsId = await loadDialogsService.GetDialogsIdByUsersIdPairAsync(notice.RequestingUserId, notice.ConversationId).ConfigureAwait(false);
                        await deleteDialogsService.DeleteDialogAsync(dialogsId.FirstOrDefault(), notice.RequestingUserId).ConfigureAwait(false);
                        UsersConversationsCacheService.Instance.UpdateUsersDialogsAsync(notice.RequestingUserId, notice.ConversationId);
                    }
                    break;
                case ConversationType.Chat:
                    {
                        var usersId = await loadChatsService.GetChatUsersIdAsync(notice.ConversationId).ConfigureAwait(false);
                        await deleteChatsService.DeleteChatAsync(notice.ConversationId, notice.RequestingUserId).ConfigureAwait(false);
                        UsersConversationsCacheService.Instance.UpdateUsersChatsAsync(usersId);
                        BlockSegmentVm segment = await BlockSegmentsService.Instance.CreateDeleteChatSegmentAsync(notice.ConversationId, notice.NodeId).ConfigureAwait(false);
                        BlockGenerationHelper.Instance.AddSegment(segment);
                    }
                    break;
                case ConversationType.Channel:
                    {
                        var usersId = await loadChannelsService.GetChannelUsersIdAsync(notice.ConversationId).ConfigureAwait(false);
                        await deleteChannelsService.DeleteChannelAsync(notice.ConversationId, notice.RequestingUserId).ConfigureAwait(false);
                        UsersConversationsCacheService.Instance.UpdateUsersChannelsAsync(usersId);
                        BlockSegmentVm segment = await BlockSegmentsService.Instance.CreateDeleteChannelSegmentAsync(notice.ConversationId, notice.NodeId).ConfigureAwait(false);
                        BlockGenerationHelper.Instance.AddSegment(segment);
                    }
                    break;
            }
        }

        public bool IsObjectValid()
        {
            return notice.ConversationId != 0
                && (notice.ConversationType >= ConversationType.Dialog || notice.ConversationType <= ConversationType.Channel);
        }
    }
}