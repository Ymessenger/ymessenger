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
using System.Collections.Generic;
using System.Threading.Tasks;
using NodeApp.MessengerData.DataTransferObjects;
using ObjectsLibrary.ViewModels;

namespace NodeApp.Interfaces.Services.Channels
{
    public interface ILoadChannelsService
    {
        Task<List<ChannelVm>> FindChannelsByStringQueryAsync(string searchQuery, long? navigationId, bool? direction = true);
        Task<List<ChannelUserVm>> GetAdministrationChannelUsersAsync(long channelId);
        Task<ChannelVm> GetChannelByIdAsync(long channelId);
        Task<List<long>> GetChannelNodesIdAsync(long channelId);
        Task<List<ChannelVm>> GetChannelsAsync(IEnumerable<long> channelsId, long userId);
        Task<List<ChannelDto>> GetChannelsWithSubscribersAsync(IEnumerable<long> channelsId);
        Task<List<ChannelUserVm>> GetChannelUsersAsync(long channelId, long? navigationUserId, long? requestorId);
        Task<List<long>> GetChannelUsersIdAsync(long channelId, bool banned = false, bool deleted = false);
        Task<List<ChannelDto>> GetUserChannelsAsync(long userId);
        Task<List<long>> GetUserChannelsIdAsync(long userId);
        Task<List<ConversationPreviewVm>> GetUserChannelsPreviewAsync(long userId);
    }
}