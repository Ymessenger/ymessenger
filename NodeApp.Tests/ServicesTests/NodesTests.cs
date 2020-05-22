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
using NodeApp.Interfaces;
using ObjectsLibrary.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace NodeApp.Tests
{
    public class NodesTests
    {
        private readonly FillTestDbHelper fillTestDbHelper;
        private readonly INodesService nodesService;
        public NodesTests()
        {
            TestsData testsData = TestsData.Create(nameof(NodesTests));
            fillTestDbHelper = testsData.FillTestDbHelper;
            nodesService = testsData.AppServiceProvider.NodesService;
        }
        [Fact]
        public async Task GetNodes()
        {
            var nodes = await nodesService.GetNodesAsync(new List<long> { 1, 2 });
            Assert.True(nodes.Count == 2 && nodes.All(opt => opt.Id == 1 || opt.Id == 2));
        }
        [Fact]
        public async Task GetAllNodesInfo()
        {
            var nodes = await nodesService.GetAllNodesInfoAsync();
            Assert.True(nodes.Count == 2 && nodes.All(opt => opt.Id == 1 || opt.Id == 2));
        }
        [Fact]
        public async Task GetAllNodeInfo()
        {
            var node = await nodesService.GetAllNodeInfoAsync(1);
            Assert.True(node.Id == 1);
        }
        [Fact]
        public async Task DeleteNode()
        {
            nodesService.CreateOrUpdateNodeInformationAsync(new NodeVm
            {
                Id = 3,
                Name = "Node 3"
            });
            await nodesService.DeleteNodesInformationAsync(new List<long> { 3 });
            Assert.Null(fillTestDbHelper.Nodes.FirstOrDefault(opt => opt.Id == 3));            
        }
    }
}