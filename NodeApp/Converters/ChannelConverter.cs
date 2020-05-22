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
using NodeApp.Extensions;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.MessengerData.Entities;
using ObjectsLibrary.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace NodeApp.Converters
{
    public static class ChannelConverter
    {
        public static ChannelVm GetChannel(Channel channel)
        {
            if (channel == null)
            {
                return null;
            }

            return new ChannelVm
            {
                About = channel.About,
                ChannelId = channel.ChannelId,
                ChannelName = channel.ChannelName,
                Photo = channel.Photo,
                Tag = channel.Tag,
                ChannelUsers = channel.ChannelUsers != null
                    ? GetChannelUsers(channel.ChannelUsers)?.ToList()
                    : null,
                NodesId = channel.NodesId?.ToList()
            };
        }

        public static ChannelDto GetChannelDto(ChannelVm channelVm)
        {
            if (channelVm == null)
            {
                return null;
            }

            return new ChannelDto
            {
                About = channelVm.About,
                ChannelId = channelVm.ChannelId.GetValueOrDefault(),
                Photo = channelVm.Photo,
                Tag = channelVm.Tag,
                NodesId = channelVm.NodesId?.ToArray(),
                ChannelName = channelVm.ChannelName,
                ChannelUsers = channelVm.ChannelUsers?.Select(channelUser => GetChannelUserDto(channelUser)).ToList()
            };
        }

        public static Channel GetChannel(ChannelVm channel)
        {
            return new Channel
            {
                About = channel.About,
                ChannelId = channel.ChannelId.GetValueOrDefault(),
                ChannelName = channel.ChannelName,
                Photo = channel.Photo,
                Tag = channel.Tag
            };
        }
        public static ChannelUserDto GetChannelUserDto(ChannelUserVm channelUserVm)
        {
            if (channelUserVm == null)
            {
                return null;
            }

            return new ChannelUserDto
            {
                Banned = channelUserVm.Banned.GetValueOrDefault(),
                ChannelId = channelUserVm.ChannelId.GetValueOrDefault(),
                ChannelUserRole = channelUserVm.ChannelUserRole.GetValueOrDefault(),
                Deleted = channelUserVm.Deleted.GetValueOrDefault(),
                IsMuted = channelUserVm.IsMuted.GetValueOrDefault(),
                SubscribedTime = channelUserVm.SubscribedTime.GetValueOrDefault(),
                UserId = channelUserVm.UserId
            };
        }

        public static ChannelUser GetChannelUser(ChannelUserVm channelUser)
        {
            return new ChannelUser
            {
                ChannelId = channelUser.ChannelId.GetValueOrDefault(),
                ChannelUserRole = channelUser.ChannelUserRole.GetValueOrDefault(),
                Deleted = channelUser.Deleted.GetValueOrDefault(),
                UserId = channelUser.UserId,
                Banned = channelUser.Banned.GetValueOrDefault(),
                IsMuted = channelUser.IsMuted.GetValueOrDefault()
            };
        }

        public static ChannelUserVm GetChannelUser(ChannelUser channelUser)
        {
            return new ChannelUserVm
            {
                ChannelId = channelUser.ChannelId,
                ChannelUserRole = channelUser.ChannelUserRole,
                Deleted = channelUser.Deleted,
                UserId = channelUser.UserId,
                Banned = channelUser.Banned,
                SubscribedTime = channelUser.SubscribedTime,
                IsMuted = channelUser.IsMuted
            };
        }

        public static Channel GetChannel(Channel editable, ChannelVm edited)
        {
            editable.ChannelName = edited.ChannelName ?? editable.ChannelName;
            editable.About = edited.About ?? editable.About;
            editable.Photo = edited.Photo ?? editable.Photo;
            editable.Deleted = edited.Deleted.GetValueOrDefault(editable.Deleted);
            return editable;
        }

        public static Channel GetChannel(Channel editable, ChannelDto edited)
        {
            editable.ChannelName = edited.ChannelName ?? editable.ChannelName;
            editable.About = edited.About ?? editable.About;
            editable.Photo = edited.Photo ?? editable.Photo;
            editable.Deleted = edited.Deleted;
            editable.CreationTime = edited.CreationTime;
            editable.NodesId = edited.NodesId;
            editable.Tag = edited.Tag;
            editable.ChannelUsers = !edited.ChannelUsers.IsNullOrEmpty() ? GetChannelUsers(edited.ChannelUsers) : editable.ChannelUsers;
            return editable;
        }

        public static Channel GetChannel(ChannelDto channel)
        {
            return new Channel
            {
                About = channel.About,
                ChannelId = channel.ChannelId,
                ChannelName = channel.ChannelName,
                ChannelUsers = GetChannelUsers(channel.ChannelUsers),
                CreationTime = channel.CreationTime,
                Deleted = channel.Deleted,
                NodesId = channel.NodesId,
                Photo = channel.Photo,
                Tag = channel.Tag
            };
        }

        public static ChannelDto GetChannelDto(Channel channel)
        {
            if (channel == null)
            {
                return null;
            }

            return new ChannelDto
            {
                About = channel.About,
                ChannelId = channel.ChannelId,
                ChannelName = channel.ChannelName,
                CreationTime = channel.CreationTime,
                Deleted = channel.Deleted,
                Messages = MessageConverter.GetMessagesDto(channel.Messages),
                NodesId = channel.NodesId,
                Photo = channel.Photo,
                Tag = channel.Tag,
                ChannelUsers = GetChannelUsersDto(channel.ChannelUsers)
            };
        }

        public static ChannelUser GetChannelUser(ChannelUserDto channelUser)
        {
            return new ChannelUser
            {
                Banned = channelUser.Banned,
                ChannelId = channelUser.ChannelId,
                ChannelUserRole = channelUser.ChannelUserRole,
                Deleted = channelUser.Deleted,
                SubscribedTime = channelUser.SubscribedTime,
                UserId = channelUser.UserId,
                IsMuted = channelUser.IsMuted
            };
        }

        public static ChannelUserDto GetChannelUserDto(ChannelUser channelUser)
        {
            return new ChannelUserDto
            {
                Banned = channelUser.Banned,
                ChannelId = channelUser.ChannelId,
                ChannelUserRole = channelUser.ChannelUserRole,
                Deleted = channelUser.Deleted,
                SubscribedTime = channelUser.SubscribedTime,
                UserId = channelUser.UserId,
                IsMuted = channelUser.IsMuted
            };
        }

        public static ChannelVm GetChannelVm(ChannelDto channel, long? userId = null)
        {
            if (channel == null)
            {
                return null;
            }

            return new ChannelVm
            {
                About = channel.About,
                ChannelId = channel.ChannelId,
                ChannelName = channel.ChannelName,
                Deleted = channel.Deleted,
                NodesId = channel.NodesId?.ToList(),
                Photo = channel.Photo,
                Tag = channel.Tag,
                SubscribersCount = channel.ChannelUsers?.Count,
                UserRole = channel.ChannelUsers?.FirstOrDefault(opt => opt.UserId == userId)?.ChannelUserRole
            };
        }

        public static List<ChannelVm> GetChannelsVm(List<ChannelDto> channels, long? userId = null) => channels?.Select(channel => GetChannelVm(channel, userId)).ToList();


        public static List<ChannelUser> GetChannelUsers(IEnumerable<ChannelUserDto> channelUsers)
        {
            return channelUsers?.Select(GetChannelUser).ToList();
        }

        private static List<ChannelUserDto> GetChannelUsersDto(ICollection<ChannelUser> channelUsers)
        {
            return channelUsers?.Select(GetChannelUserDto).ToList();
        }

        public static List<Channel> GetChannels(IEnumerable<ChannelVm> channels)
        {
            return channels?.Select(GetChannel).ToList();
        }

        public static List<ChannelVm> GetChannels(IEnumerable<Channel> channels)
        {
            return channels?.Select(GetChannel).ToList();
        }

        public static List<ChannelUser> GetChannelUsers(IEnumerable<ChannelUserVm> channelUsers)
        {
            return channelUsers?.Select(GetChannelUser).ToList();
        }

        public static List<Channel> GetChannels(List<ChannelDto> channels)
        {
            return channels?.Select(GetChannel).ToList();
        }

        public static List<ChannelUserVm> GetChannelUsers(IEnumerable<ChannelUser> channelUsers)
        {
            return channelUsers?.Select(GetChannelUser).ToList();
        }

        public static List<ChannelDto> GetChannelsDto(IEnumerable<Channel> channels)
        {
            return channels?.Select(GetChannelDto).ToList();
        }
        public static List<ChannelDto> GetChannelsDto(IEnumerable<ChannelVm> channels)
        {
            return channels?.Select(GetChannelDto).ToList();
        }

    }
}
