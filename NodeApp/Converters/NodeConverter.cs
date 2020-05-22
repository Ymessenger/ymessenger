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
using NodeApp.MessengerData.Entities;
using ObjectsLibrary;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NodeApp.Converters
{
    public static class NodeConverter
    {
        public static NodeVm GetNodeVm(Node node)
        {
            return node == null
                ? null
                : new NodeVm
                {
                    Id = node.Id,
                    About = node.About,
                    Country = node.Country,
                    Domains = node.DomainNodes?.Select(opt => opt?.Domain).ToList(),
                    Name = node.Name,
                    Photo = node.Photo,
                    Routing = node.Routing ?? true,
                    StartDay = node.Startday ?? DateTime.MinValue,
                    Storage = node.Storage ?? true,
                    Tag = node.Tag ?? RandomExtensions.NextString(10, "N"),
                    Visible = node.Visible ?? true,
                    NodeKey = new NodeKeyVm
                    {
                        EncPublicKey = node.NodeKeys?.OrderByDescending(opt => opt.GenerationTime).FirstOrDefault()?.PublicKey,
                        SignPublicKey = node.NodeKeys?.OrderByDescending(opt => opt.GenerationTime).FirstOrDefault()?.SignPublicKey,
                        ExpiredAt = (node.NodeKeys?.OrderByDescending(opt => opt.GenerationTime).FirstOrDefault()?.ExpirationTime).GetValueOrDefault(),
                        KeyId = (node.NodeKeys?.OrderByDescending(opt => opt.GenerationTime).FirstOrDefault()?.KeyId).GetValueOrDefault()
                    },
                    ClientsPort = node.ClientsPort,
                    NodesPort = node.NodesPort,
                    AdminEmail = node.AdminEmail,
                    SupportEmail = node.SupportEmail,
                    EncryptionType = node.EncryptionType,
                    PermanentlyDeleting = node.PermanentlyDeleting,
                    RegistrationMethod = node.RegistrationMethod,
                    UserRegistrationAllowed = node.UserRegistrationAllowed
                };
        }

        public static Node GetNode(NodeVm node)
        {
            return node == null
                ? null
                : new Node
                {
                    About = node.About,
                    Country = node.Country,
                    Id = node.Id,
                    Name = node.Name,
                    Photo = node.Photo,
                    Startday = node.StartDay,
                    Storage = node.Storage,
                    Routing = node.Routing,
                    Tag = node.Tag,
                    Visible = node.Visible,
                    DomainNodes = node.Domains
                        ?.Select(domain => new DomainNode
                        {
                            NodeId = node.Id,
                            Domain = domain
                        })
                        .ToList(),                    
                    ClientsPort = node.ClientsPort,
                    NodesPort = node.NodesPort,
                    AdminEmail = node.AdminEmail,
                    SupportEmail = node.SupportEmail,
                    PermanentlyDeleting = node.PermanentlyDeleting,
                    EncryptionType = node.EncryptionType,
                    RegistrationMethod = node.RegistrationMethod,
                    UserRegistrationAllowed = node.UserRegistrationAllowed
                };
        }

        public static Node GetNode(Node changedNode, NodeVm newNode)
        {
            if (newNode == null || changedNode == null)
            {
                return null;
            }

            changedNode.Id = newNode.Id;
            changedNode.Name = newNode.Name;
            changedNode.Photo = newNode.Photo;
            changedNode.Routing = newNode.Routing;
            changedNode.Startday = newNode.StartDay;
            changedNode.Storage = newNode.Storage;
            changedNode.Tag = newNode.Tag;
            changedNode.Visible = newNode.Visible;
            changedNode.About = newNode.About;
            changedNode.Country = newNode.Country;
            changedNode.DomainNodes = newNode.Domains
                        ?.Select(domain => new DomainNode
                        {
                            NodeId = newNode.Id,
                            Domain = domain
                        })
                        .ToList();            
            changedNode.NodesPort = newNode.NodesPort;
            changedNode.ClientsPort = newNode.ClientsPort;
            changedNode.SupportEmail = newNode.SupportEmail;
            changedNode.AdminEmail = newNode.AdminEmail;
            changedNode.EncryptionType = newNode.EncryptionType;
            changedNode.PermanentlyDeleting = newNode.PermanentlyDeleting;
            changedNode.RegistrationMethod = newNode.RegistrationMethod;
            changedNode.UserRegistrationAllowed = newNode.UserRegistrationAllowed;
            return changedNode;
        }

        public static List<NodeVm> GetNodesVm(List<Node> nodes)
        {
            return nodes.Select(GetNodeVm).ToList();
        }
    }
}
