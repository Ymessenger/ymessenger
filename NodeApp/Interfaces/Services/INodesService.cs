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
using ObjectsLibrary.ViewModels;

namespace NodeApp.Interfaces
{
    public interface INodesService
    {
        void CreateOrUpdateNodeInformationAsync(NodeVm nodeInfo);
        Task DeleteNodesInformationAsync(IEnumerable<long> nodesIds);
        Task<NodeVm> GetAllNodeInfoAsync(long nodeId);
        Task<List<NodeVm>> GetAllNodesInfoAsync();
        Task<List<NodeVm>> GetNodesAsync(List<long> nodesId);
        Task<List<LastVm>> GetUsersOnlineAsync(List<long> usersId);
        Task<bool> IsDatabaseEmptyAsync();
        Task MarkNodesAsBanned(IEnumerable<long> nodesIds);
        Task<List<NodeVm>> CreateOrUpdateNodesInformationAsync(List<NodeVm> nodes);
    }
}