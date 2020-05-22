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
using System.ComponentModel.DataAnnotations;

namespace NodeApp.HttpServer.Models
{
    public class CreateUserModel
    {
        public string NameFirst { get; set; }        
        public string NameSecond { get; set; }
        [Phone]       
        public string PhoneNumber { get; set; }
        [EmailAddress]        
        public string Email { get; set; }
        [DataType(DataType.Password)]       
        public string Password { get; set; }        
        [Compare("Password")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }
    }
}
