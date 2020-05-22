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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NodeApp.Interfaces;
using NodeApp.Objects;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace NodeApp.HttpServer.Controllers
{
    public class WebSocketController : Controller
    {
        private readonly IAppServiceProvider _serviceProvider;
        public WebSocketController(IAppServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public async Task Index()
        {
            if (NodeSettings.Configs.RecoveryMode)
            {
                HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
            }

            if (HttpContext.Connection.LocalPort == NodeSettings.Configs.Node.ClientsPort ||
                HttpContext.Connection.LocalPort == NodeSettings.Configs.Node.NodesPort)
            {
                if (HttpContext.WebSockets.IsWebSocketRequest)
                {
                    WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync().ConfigureAwait(true);
                    DataReceiver dataReceiver;
                    if (HttpContext.Connection.LocalPort == NodeSettings.Configs.Node.ClientsPort)
                    {
                        ClientConnection client = new ClientConnection(
                           webSocket,
                           HttpContext.Connection.RemoteIpAddress,
                           HttpContext.Connection.RemotePort,
                           HttpContext.Connection.Id);
                        dataReceiver = new ClientDataReceiver(client, _serviceProvider);
                        await dataReceiver.BeginReceiveAsync().ConfigureAwait(true);
                    }
                    else if (HttpContext.Connection.LocalPort == NodeSettings.Configs.Node.NodesPort)
                    {
                        var nodeConnection = new NodeConnection
                        {
                            NodeWebSocket = webSocket                            
                        };
                        dataReceiver = new NodeDataReceiver(nodeConnection, _serviceProvider);
                        await dataReceiver.BeginReceiveAsync().ConfigureAwait(true);
                    }
                }
            }
            else if (HttpContext.Connection.LocalPort == 80 || HttpContext.Connection.LocalPort == 443)
            {
                if (!HttpContext.WebSockets.IsWebSocketRequest)
                {
                    HttpContext.Response.Redirect("/index.html");
                }
            }
        }
    }
}