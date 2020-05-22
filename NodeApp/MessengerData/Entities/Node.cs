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
using ObjectsLibrary.Enums;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NodeApp.MessengerData.Entities
{
    public partial class Node
    {
        public Node()
        {
            DomainNodes = new HashSet<DomainNode>();
            Ipnodes = new HashSet<Ipnode>();
        }

        public Node(NodeVm node)
        {
            Id = node.Id;
            Name = node.Name;
            Tag = node.Tag;
            About = node.About;
            Photo = node.Photo;
            Country = node.Country;            
            Startday = node.StartDay;
            Visible = node.Visible;
            Storage = node.Storage;
            Routing = node.Routing;            
            ClientsPort = node.ClientsPort;
            NodesPort = node.NodesPort;            
        }

        public long Id { get; set; }
        public string Name { get; set; }
        [MaxLength(100)]
        public string Tag { get; set; }
        public string About { get; set; }
        public string Photo { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public DateTime? Startday { get; set; }
        public string Language { get; set; }
        public bool? Visible { get; set; }
        public bool? Storage { get; set; }
        public bool? Routing { get; set; }
        public int NodesPort { get; set; }
        public int ClientsPort { get; set; }
        public string SupportEmail { get; set; }
        public string AdminEmail { get; set; }           
        public EncryptionType EncryptionType { get; set; }
        public bool PermanentlyDeleting { get; set; }
        public RegistrationMethod RegistrationMethod { get; set; }
        public bool UserRegistrationAllowed { get; set; }

        public ICollection<DomainNode> DomainNodes { get; set; }
        public ICollection<Ipnode> Ipnodes { get; set; }
        public ICollection<ChangeUserNodeOperation> ChangeUserNodeOperations { get; set; }
        public ICollection<NodeKeys> NodeKeys { get; set; } 
    }
}
