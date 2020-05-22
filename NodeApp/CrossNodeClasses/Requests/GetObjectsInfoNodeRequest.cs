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
using NodeApp.CrossNodeClasses.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NodeApp.CrossNodeClasses.Requests
{
    [Serializable]
    public class GetObjectsInfoNodeRequest : NodeRequest
    {
        public List<long> ObjectsId { get; set; }     
        public List<string> FilesIds { get; set; }
        public long? RequestorUserId { get; set; }
        public GetObjectsInfoNodeRequest(IEnumerable<long> objectsId, long? requestorUserId, NodeRequestType nodeRequestType)
        {
            ObjectsId = objectsId.ToList();
            RequestorUserId = requestorUserId;
            switch (nodeRequestType)
            {
                case NodeRequestType.GetChats:
                case NodeRequestType.GetUsers:
                case NodeRequestType.GetChannels:                    
                    RequestType = nodeRequestType;
                    break;                
                default:
                    throw new ArgumentException($"NodeRequestType can only {NodeRequestType.GetUsers.ToString()} or {NodeRequestType.GetChats.ToString()}");
            }
        }
        public GetObjectsInfoNodeRequest(IEnumerable<string> filesIds)
        {
            FilesIds = filesIds?.ToList();
            RequestType = NodeRequestType.GetFiles;
        }
        public GetObjectsInfoNodeRequest() { }
    }
}
