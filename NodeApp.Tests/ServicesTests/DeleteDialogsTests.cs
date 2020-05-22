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
    public class DeleteDialogsTests
    {
        private readonly FillTestDbHelper fillTestDbHelper;
        private readonly IDeleteDialogsService deleteDialogsService;
        public DeleteDialogsTests()
        {
            TestsData testsData = TestsData.Create(nameof(DeleteDialogsTests));
            fillTestDbHelper = testsData.FillTestDbHelper;
            deleteDialogsService = testsData.AppServiceProvider.DeleteDialogsService;
        }
        [Fact]
        public async Task DeleteDialog()
        {
            var dialog = fillTestDbHelper.Dialogs.FirstOrDefault();
            await deleteDialogsService.DeleteDialogAsync(dialog.Id, dialog.FirstUID);
            Assert.Null(fillTestDbHelper.Dialogs.FirstOrDefault(opt => opt.Id == dialog.Id));
        }
    }
}
