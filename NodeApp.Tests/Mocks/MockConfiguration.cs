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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Moq;
using System.Collections.Generic;

namespace NodeApp.Tests.Mocks
{
    public class MockConfiguration : IConfiguration
    {
        public string this[string key] 
        { 
            get => "default"; set =>  keys.Add(value); 
        }
        private List<string> keys = new List<string>();

        public IEnumerable<IConfigurationSection> GetChildren()
        {
            return new Mock<IEnumerable<IConfigurationSection>>().Object;
        }

        public IChangeToken GetReloadToken()
        {
            return new Mock<IChangeToken>().Object;
        }

        public IConfigurationSection GetSection(string key)
        {
            return new Mock<IConfigurationSection>().Object;
        }
    }
}
