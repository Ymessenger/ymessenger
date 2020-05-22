﻿/** 
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
using ObjectsLibrary.ViewModels;
using System.Net;
using System.Net.WebSockets;

namespace NodeApp.Objects
{
    public class ClientConnection
    {
        public long? UserId { get; set; }
        public IPAddress ClientIP { get; set; } 
        public int ClientPort { get; set; }
        public WebSocket ClientSocket { get; set; }
        public WebSocket ProxyNodeWebSocket { get; set; }
        public string FileAccessToken { get; set; }
        public TokenVm CurrentToken { get; set; }
        public string CurrentDeviceTokenId { get; set; }
        public byte[] SymmetricKey { get; set; }
        public byte[] PublicKey { get; set; }
        public byte[] SignPublicKey { get; set; }
        public bool SentKey { get; set; } = false;
        public byte[] RandomSequence { get; set; }
        public bool Confirmed { get; set; }       
        public bool? Banned { get; set; }

        public bool IsProxiedClientConnection
        {
            get
            {
                return ProxyNodeWebSocket != null;
            }
        }

        public bool IsEncryptedConnection
        {
            get
            {
                return SymmetricKey != null && SentKey;
            }
        }
        
        public ClientConnection(long? userId, WebSocket clientSocket)
        {
            UserId = userId;
            ClientSocket = clientSocket;
        }
        public ClientConnection(WebSocket clientSocket, IPAddress remoteIP, int remotePort, string connectionId)
        {
            ClientSocket = clientSocket;
            ClientIP = remoteIP;
            ClientPort = remotePort;
        }

        public ClientConnection(long userId, WebSocket nodeProxyWebSocket)
        {
            ProxyNodeWebSocket = nodeProxyWebSocket;
            UserId = userId;
        } 

        public ClientConnection() { }
    }
}
