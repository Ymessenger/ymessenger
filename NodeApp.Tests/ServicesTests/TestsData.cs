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
using NodeApp.Tests.Mocks;
using System.Collections.Concurrent;

namespace NodeApp.Tests
{
    public class TestsData 
    {      
        public FillTestDbHelper FillTestDbHelper { get; private set; }        

        public MockAppServiceProvider AppServiceProvider { get; private set; }

        private static readonly ConcurrentDictionary<string, TestsData> _instances = new ConcurrentDictionary<string, TestsData>();

        private TestsData()
        {
            NodeSettings.Configs = new Objects.SettingsObjects.Configs(new MockConfiguration());
            NodeSettings.Configs.Node = new ObjectsLibrary.ViewModels.NodeVm
            {
                EncryptionType = ObjectsLibrary.ViewModels.EncryptionType.Allowed
            };
        }
        static TestsData()
        {
            NodeSettings.Configs = new Objects.SettingsObjects.Configs(new MockConfiguration());
            NodeSettings.Configs.Node = new ObjectsLibrary.ViewModels.NodeVm
            {
                EncryptionType = ObjectsLibrary.ViewModels.EncryptionType.Allowed
            };
        }
        public static TestsData Create(string dbName)
        {
            if (_instances.TryGetValue(dbName, out var result))
                return result;
            TestsData testsData = new TestsData();
            MockAppServiceProvider appServiceProvider = new MockAppServiceProvider(dbName);
            testsData.FillTestDbHelper = new FillTestDbHelper((MockMessengerDbContextFactory)appServiceProvider.MessengerDbContextFactory);
            testsData.AppServiceProvider = appServiceProvider;
            testsData.FillTestDbHelper.FillMessengerContextAsync().Wait();
            _instances.TryAdd(dbName, testsData);
            return testsData;           
        }       
    }
}
