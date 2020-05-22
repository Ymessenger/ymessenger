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
using LinqKit;
using Microsoft.EntityFrameworkCore;
using NodeApp.Converters;
using NodeApp.Interfaces;
using NodeApp.MessengerData.Entities;
using ObjectsLibrary;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.MessengerData.Services
{
    public class NodesService : INodesService
    {
        private readonly IDbContextFactory<MessengerDbContext> contextFactory;
        public NodesService(IDbContextFactory<MessengerDbContext> contextFactory)
        {
            this.contextFactory = contextFactory;
        }
        public async Task<List<LastVm>> GetUsersOnlineAsync(List<long> usersId)
        {
            var condition = PredicateBuilder.New<User>();
            condition = usersId.Aggregate(condition,
                (current, value) => current.Or(e => e.Id == value));
            using (MessengerDbContext context = contextFactory.Create())
            {
                var users = await context.Users.Where(condition).ToListAsync().ConfigureAwait(false);
                return users.Select(user => new LastVm(user.Online.GetValueOrDefault().ToDateTime(), user.Id, 0)).ToList();
            }
        }

        public async Task<List<NodeVm>> GetNodesAsync(List<long> nodesId)
        {
            var nodesCondition = PredicateBuilder.New<Node>();
            nodesCondition = nodesId.Aggregate(nodesCondition,
                (current, value) => current.Or(opt => opt.Id == value).Expand());
            using (MessengerDbContext context = contextFactory.Create())
            {
                var nodes = await context.Nodes
                .Include(opt => opt.NodeKeys)
                .Include(opt => opt.DomainNodes)
                .Where(nodesCondition)
                .ToListAsync()
                .ConfigureAwait(false);
                return NodeConverter.GetNodesVm(nodes);
            }
        }
        public async void CreateOrUpdateNodeInformationAsync(NodeVm nodeInfo)
        {
            try
            {
                using (MessengerDbContext context = contextFactory.Create())
                {
                    Node node = await context.Nodes                        
                        .Include(opt => opt.DomainNodes)
                        .Include(opt => opt.Ipnodes)
                    .FirstOrDefaultAsync(opt => opt.Id == nodeInfo.Id)
                    .ConfigureAwait(false);
                    if (node == null)
                    {
                        node = NodeConverter.GetNode(nodeInfo);
                        await context.Nodes.AddAsync(node).ConfigureAwait(false);
                    }
                    else
                    {                        
                        node = NodeConverter.GetNode(node, nodeInfo);                        
                        context.Nodes.Update(node);
                    }
                    await context.SaveChangesAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        public async Task<NodeVm> GetAllNodeInfoAsync(long nodeId)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                return NodeConverter.GetNodeVm(
                await context.Nodes
                .Include(opt => opt.DomainNodes)
                .Include(opt => opt.Ipnodes)
                .Include(opt => opt.NodeKeys)
                .FirstOrDefaultAsync(opt => opt.Id == nodeId)
                .ConfigureAwait(false));
            }
        }

        public async Task DeleteNodesInformationAsync(IEnumerable<long> nodesIds)
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                var nodesCondition = PredicateBuilder.New<Node>();
                nodesCondition = nodesIds.Aggregate(nodesCondition,
                    (current, value) => current.Or(opt => opt.Id == value).Expand());
                List<Node> nodes = await context.Nodes.Where(nodesCondition).ToListAsync().ConfigureAwait(false);
                context.RemoveRange(nodes);
                await context.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        public async Task<List<NodeVm>> GetAllNodesInfoAsync()
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                return NodeConverter.GetNodesVm(await context.Nodes
                    .AsNoTracking()
                    .Include(opt => opt.NodeKeys)
                    .Include(opt => opt.DomainNodes)
                    .Include(opt => opt.Ipnodes)
                    .ToListAsync()
                    .ConfigureAwait(false));
            }
        }

        public Task MarkNodesAsBanned(IEnumerable<long> nodesIds)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> IsDatabaseEmptyAsync()
        {
            using (MessengerDbContext context = contextFactory.Create())
            {
                if (!await context.Users.AnyAsync().ConfigureAwait(false))
                    return true;
                return false;
            }
        }

        public async Task<List<NodeVm>> CreateOrUpdateNodesInformationAsync(List<NodeVm> nodes)
        {
            return null;
        }
    }
}