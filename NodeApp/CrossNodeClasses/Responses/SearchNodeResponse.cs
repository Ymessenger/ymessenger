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
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;

namespace NodeApp.CrossNodeClasses.Responses
{
    [Serializable]
    public class SearchNodeResponse : NodeResponse
    {
        public List<UserVm> Users { get; set; }
        public List<ChannelVm> Channels { get; set; }
        public List<ChatVm> Chats { get; set; }

        public SearchNodeResponse(long requestId, List<UserVm> users, List<ChannelVm> channels, List<ChatVm> chats)
        {
            Users = users;
            Channels = channels;
            Chats = chats;
            RequestId = requestId;
            ResponseType = Enums.NodeResponseType.Search;
        }
    }
}
