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
using NodeApp.Interfaces.Services.Dialogs;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace NodeApp.Tests
{
    public class LoadDialogsTests
    {
        private readonly FillTestDbHelper fillTestDbHelper;
        private readonly ILoadDialogsService loadDialogsService;
        public LoadDialogsTests()
        {
            TestsData testsData = TestsData.Create(nameof(LoadDialogsTests));
            fillTestDbHelper = testsData.FillTestDbHelper;
            loadDialogsService = testsData.AppServiceProvider.LoadDialogsService;
        }
        [Fact]
        public async Task GetDialog()
        {
            var dialog =  fillTestDbHelper.Dialogs.FirstOrDefault();
            var actualDialog = await loadDialogsService.GetDialogAsync(dialog.Id);
            Assert.True(
                dialog.FirstUID == actualDialog.FirstUserId 
                && dialog.SecondUID == actualDialog.SecondUserId 
                && dialog.Id == actualDialog.Id);
        }
        [Fact]
        public async Task GetDialogsIdByUsersIdPair()
        {
            var dialog = fillTestDbHelper.Dialogs.FirstOrDefault();
            var mirrorDialog = fillTestDbHelper.Dialogs.FirstOrDefault(opt => opt.FirstUID == dialog.SecondUID && opt.SecondUID == dialog.FirstUID);
            var dialogsIds = await loadDialogsService.GetDialogsIdByUsersIdPairAsync(dialog.FirstUID, dialog.SecondUID);
            Assert.True(dialogsIds.Contains(dialog.Id) && dialogsIds.Contains(mirrorDialog.Id));
        }
        [Fact]
        public async Task GetDialogUsers()
        {
            var dialog = fillTestDbHelper.Dialogs.FirstOrDefault();
            var users = await loadDialogsService.GetDialogUsersAsync(dialog.Id);
            Assert.True(users.All(opt => opt.Id == dialog.FirstUID || opt.Id == dialog.SecondUID));
        }
        [Fact]
        public async Task GetDialogNodes()
        {
            var dialog = fillTestDbHelper.Dialogs.FirstOrDefault();      
            var nodesIds = await loadDialogsService.GetDlalogNodesAsync(dialog.Id);
            Assert.True(nodesIds.Contains(dialog.SecondU.NodeId.Value) || nodesIds.Contains(dialog.FirstU.NodeId.Value));
        }
        [Fact]
        public async Task GetMirrorDialogId()
        {
            var dialog = fillTestDbHelper.Dialogs.FirstOrDefault();
            var expectedDialog = fillTestDbHelper.Dialogs.FirstOrDefault(opt => opt.FirstUID == dialog.SecondUID && opt.SecondUID == dialog.FirstUID);
            var actualDialogId = await loadDialogsService.GetMirrorDialogIdAsync(dialog.Id);
            Assert.True(expectedDialog.Id == actualDialogId);
        }
        [Fact]
        public async Task GetUserDialogsIds()
        {
            var user = fillTestDbHelper.Users.FirstOrDefault();
            var expectedDialogsIds = user.DialogsFirstU.Select(opt => opt.Id);
            var actualDialogsIds = await loadDialogsService.GetUserDialogsIdAsync(user.Id);
            Assert.Equal(expectedDialogsIds, actualDialogsIds);
        }
        [Fact]
        public async Task GetUsersDialogs()
        {
            var user = fillTestDbHelper.Users.FirstOrDefault();
            var secondUser = fillTestDbHelper.Users.FirstOrDefault(opt => opt.Id != user.Id);
            var usersDialogs = await loadDialogsService.GetUsersDialogsAsync(user.Id, secondUser.Id);
            Assert.True(usersDialogs.All(
                dialog => (dialog.FirstUserId == user.Id || dialog.SecondUserId == user.Id) 
                && (dialog.FirstUserId == secondUser.Id || dialog.SecondUserId == secondUser.Id)));            
        }
    }
}
