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
using ObjectsLibrary.RequestClasses;
using System;
using System.Collections.Generic;

namespace NodeApp.CrossNodeClasses.Requests
{
    [Serializable]
    public class SearchNodeRequest : NodeRequest
    {
        public string SearchQuery { get; }
        public long? NavigationId { get; }
        public bool? Direction { get; }
        public long? RequestorId { get; }
        public List<SearchType> SearchTypes { get; set; }
        public SearchNodeRequest(string searchQuery, long? navigationUserId, bool? direction, List<SearchType> searchTypes, long? requestorId)
        {
            SearchQuery = searchQuery;
            NavigationId = navigationUserId;
            Direction = direction;
            SearchTypes = searchTypes;
            RequestorId = requestorId;
            RequestType = Enums.NodeRequestType.Search;
        }
    }
}
