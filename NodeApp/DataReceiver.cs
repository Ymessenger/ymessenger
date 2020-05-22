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
using NodeApp.ExceptionClasses;
using NodeApp.Interfaces;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace NodeApp
{
    public abstract class DataReceiver
    {
        protected const int BUFF_SIZE = NodeSettings.WEBSOCKET_BUFFER_SIZE;
        protected const int MAX_REQUEST_SIZE = NodeSettings.MAX_REQUEST_SIZE;
        protected const int TIME_PERIOD = 2000;
        protected const int ALLOWED_REQUEST_COUNT = 10;
        protected WebSocket socket;
        protected int requestCount = 0;
        protected Stopwatch timeValue = new Stopwatch();
        protected IAppServiceProvider _appServiceProvider;

        public abstract Task BeginReceiveAsync();

        public DataReceiver(IAppServiceProvider appServiceProvider)
        {
            _appServiceProvider = appServiceProvider;
        }

        protected async Task<byte[]> ReceiveBytesAsync(uint bufferLength = BUFF_SIZE, uint maxDataLength = MAX_REQUEST_SIZE)
        {
            byte[] buffer = new byte[bufferLength];
            List<byte> fullMessage = new List<byte>();
            WebSocketReceiveResult receiveResult = await socket.ReceiveAsync(buffer, CancellationToken.None).ConfigureAwait(true);
            while (!receiveResult.EndOfMessage)
            {
                if (fullMessage.Count < maxDataLength)
                {
                    fullMessage.AddRange(buffer.Take(receiveResult.Count));
                    buffer = new byte[bufferLength];
                    receiveResult = await socket.ReceiveAsync(buffer, CancellationToken.None).ConfigureAwait(true);
                }
                else
                {
                    throw new TooLargeReceivedDataException(
                        $"Max data length:{maxDataLength},  Current data length:{fullMessage.Count}");
                }
            }
            fullMessage.AddRange(buffer.Take(receiveResult.Count));
            return fullMessage.ToArray();
        }

        protected bool CheckRequestFrequency(int allowedRequestCount = ALLOWED_REQUEST_COUNT)
        {
            if (requestCount >= allowedRequestCount)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        protected async Task WatchForRequestsAsync()
        {
            while (socket.State == WebSocketState.Open)
            {
                requestCount = 0;
                await Task.Delay(TIME_PERIOD).ConfigureAwait(false);
            }
        }
    }
}