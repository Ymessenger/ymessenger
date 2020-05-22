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
using NodeApp.CacheStorageClasses;
using NodeApp.Objects;
using ObjectsLibrary;
using ObjectsLibrary.Enums;
using ObjectsLibrary.RequestClasses;
using ObjectsLibrary.ResponseClasses;
using System;
using System.IO;
using System.Text;

namespace NodeApp
{
    public static class Logger
    {
        private static string logPath;
        public static object locker = new object();
        public static object metricsLocker = new object();
        private const string divideLine = "***";
        private const string requestLogTitle = "Request";
        private const string errorLogTitle = "Error";
        private const string crossNodeLogTitle = "CrossNode";
        private static FileStream errorFileStream;
        private static FileStream requestFileStream;
        private static FileStream crossNodeLogFileStream;
        private static FileStream metricsFileStream;
        public static bool Init(string logDirectory)
        {
            try
            {
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }
                logPath = logDirectory;                
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void WriteLog(CommunicationObject communicationObject, ClientConnection clientConnection)
        {
            try
            {
                if (communicationObject == null)
                    return;
                string logText = string.Format("{3}{4}{3}UserId:{5}{3}IP-address:{3}{5}{3}Time:{3}{0}{3}{6}:{3}{1}{3}",
                    DateTime.UtcNow,
                    communicationObject.ToString(),
                    Environment.NewLine,
                    divideLine,
                    clientConnection.UserId.GetValueOrDefault(),
                    clientConnection.ClientIP.ToString(),
                    communicationObject.Type.ToString());
                WriteTextToFile(requestFileStream, logText, requestLogTitle);
            }
            catch
            {
                return;
            }
        }
        public static void WriteLog(CommunicationObject @object)
        {
            string logText = $"{@object.Type}\nTime: {DateTime.UtcNow}\n{@object.ToString()}\n";
            WriteTextToFile(crossNodeLogFileStream, logText, crossNodeLogTitle);
        }
        public static void WriteLog(Exception except, CommunicationObject @object)
        {
            try
            {                
                string logText = string.Format("{4}{3}Time:{3}{0}{3}Exception:{3}{1}{3}Request:{3}{2}{3}{4}",
                    DateTime.UtcNow, except.ToString(), @object.ToString(), Environment.NewLine, divideLine);
                WriteTextToFile(errorFileStream, logText,errorLogTitle);
            }
            catch
            {
                return;
            }
        }

        public static void WriteLog(string text, string errorText = null)
        {
            try
            {                
                string logText = string.Format("{3}{2}Request:{2}{0}{2}Error text:{2}{1}{2}{3}",
                    text, errorText, Environment.NewLine, divideLine);
                WriteTextToFile(requestFileStream, logText, requestLogTitle);
            }
            catch
            {
                return;
            }
        }

        public static void WriteLog(Exception except, string text = null)
        {
            try
            {                
                string logText = string.Format("{3}{4}{3}Time:{3}{0}{3}Exception:{3}{1}{3}TEXT:{3}{2}{3}",
                    DateTime.UtcNow, except.ToString(), text, Environment.NewLine, divideLine);
                WriteTextToFile(errorFileStream, logText, errorLogTitle);
            }
            catch
            {
                return;
            }
        }

        public static void WriteLog(Request request, Response response, ClientConnection clientConnection)
        {
            try
            {
                if (request == null || response == null)
                    return;                
                string logText = string.Format("{3}{4}{3}UserId:{5}{3}IP-address:{3}{6}{3}Time:{3}{0}{3}Request:{3}{1}{3}Response:{3}{2}{3}",
                    DateTime.UtcNow, 
                    request.ToString(), 
                    response.ToString(), 
                    Environment.NewLine, 
                    divideLine, 
                    clientConnection.UserId.GetValueOrDefault(), 
                    clientConnection.ClientIP.ToString());
                WriteTextToFile(requestFileStream, logText, requestLogTitle);
            }
            catch
            {
                return;
            }
        }       

        public static void WriteRequestMetrics(RequestType requestType, long milliseconds, int requestSize, long requestId)
        {
            WriteRequestMetrics(metricsFileStream, requestType, milliseconds, requestSize, requestId);
        }


        private static async void WriteRequestMetrics(Stream metricsStream, RequestType requestType, long milliseconds, int requestSize, long requestId)
        {
            try
            {
                bool isCrossNodeApiInvolved = await MetricsHelper.Instance.IsCrossNodeApiInvolvedAsync(requestId).ConfigureAwait(false);
                string metricsText = $"{(byte)requestType},{milliseconds},{requestSize},{isCrossNodeApiInvolved}\n";
                lock (metricsLocker)
                {                    
                    if (metricsStream == null)
                    {
                        string filePath = $"{logPath}/metrics.csv";                        
                        metricsStream = new FileStream(filePath, FileMode.Append, FileAccess.Write);
                    }
                    using (StreamWriter streamWriter = new StreamWriter(metricsStream, Encoding.UTF8))
                        streamWriter.Write(metricsText);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        
        private static void WriteTextToFile(FileStream fileStream, string text, string logName)
        {
            lock (locker)
            {
                if (fileStream == null || !fileStream.Name.Contains(DateTime.UtcNow.Date.ToString("ddMMyyyy")))
                {
                    string filePath = $"{logPath}/{logName}{DateTime.UtcNow.Date.ToString("ddMMyyyy")}.txt";
                    fileStream?.Close();
                    fileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write);
                }
                using (StreamWriter streamWriter = new StreamWriter(fileStream, Encoding.UTF8))
                    streamWriter.Write(text);
            }
        }
    }
}