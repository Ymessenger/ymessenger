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
using ObjectsLibrary.Encryption;
using ObjectsLibrary.Interfaces;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NodeApp.DbBackup
{
    public static class DbBackupHelper
    {
        private readonly static IFileStorage fileStorage;

        static DbBackupHelper()
        {
            fileStorage = AppServiceProvider.Instance.FileStorage;
        }

        public static async void StartDbBackupOperationAsync(DatabaseConnectionInfo dbConnection, string filename = null)
        {
            try
            {
                while (true)
                {
                    try
                    {
                        await CreateBackupAsync(dbConnection, filename).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(ex);
                    }
                    finally
                    {
                        await Task.Delay(TimeSpan.FromHours(24)).ConfigureAwait(false);
                    }
                }
            }
            catch(Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }
        
        public static async Task<bool> RestoreDbAsync(DatabaseConnectionInfo dbConnection, string backupFilename = null)
        {
            try
            {
                string userName = dbConnection.Username;
                string dbName = dbConnection.Database;
                string dbHost = dbConnection.Host;
                string dbPass = dbConnection.Password;
                int dbPort = dbConnection.Port;
                if (backupFilename == null)
                {
                    var backupsNames = await fileStorage.SearchAsync($"{dbName}_backup_{NodeSettings.Configs.Node.Id}").ConfigureAwait(false);
                    backupFilename = backupsNames.FirstOrDefault()?.FileId;
                }
                using (var backupStream = await fileStorage.GetStreamAsync(backupFilename).ConfigureAwait(false))
                {
                    var decryptedStream = await Encryptor.DecryptFileAsync(backupStream, NodeSettings.Configs.Password, backupFilename).ConfigureAwait(false);
                    decryptedStream.Close();
                }
                string backupCommand = $"\"PGPASSWORD={dbPass} pg_restore -U {userName} --host {dbHost} --port {dbPort} -d {dbName} --clean --if-exists --create {backupFilename}\"";
                Process restoreProcess = Process.Start("/bin/bash", $"-c {backupCommand}");
                restoreProcess.WaitForExit();
                if (restoreProcess.ExitCode == 0)
                    return true;
                return false;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
                return false;
            }
        }

        public static async Task<bool> CreateBackupAsync(DatabaseConnectionInfo dbConnection, string filename = null)
        {
            if (dbConnection == null)
                throw new ArgumentNullException(nameof(dbConnection));
            string dbName = dbConnection.Database;
            string userName = dbConnection.Username;
            string dbHost = dbConnection.Host;
            string dbPass = dbConnection.Password;
            int dbPort = dbConnection.Port;
            if (filename == null)
                filename = $"{dbName}_backup_{NodeSettings.Configs.Node.Id}_{DateTime.UtcNow.ToString("ddMMyyyy")}";
            if ((await fileStorage.SearchAsync($"{filename}.bin").ConfigureAwait(false)).Count == 0)
            {
                string command = $"\"PGPASSWORD={dbPass} pg_dump -U {userName} {dbName} --compress=9 --format=custom --host {dbHost} --port {dbPort}\"";
                ProcessStartInfo processStartInfo = new ProcessStartInfo("/bin/bash", $"-c {command}")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                var process = Process.Start(processStartInfo);
                using (var encryptedStream = await Encryptor.EncryptFileAsync(process.StandardOutput.BaseStream, NodeSettings.Configs.Password, $"{filename}.bin").ConfigureAwait(false))
                {                    
                    process.WaitForExit();
                    if (process.ExitCode != 0)
                    {
                        string message = await process.StandardError.ReadToEndAsync().ConfigureAwait(false);
                        throw new Exception($"Error creating backup: {message}");
                    }
                    var existingFiles = await fileStorage.SearchAsync($"{dbName}_backup_{NodeSettings.Configs.Node.Id}").ConfigureAwait(false);
                    if (existingFiles.Count > NodeSettings.Configs.MaxDbBackups)
                    {
                        var excessFiles = existingFiles.OrderByDescending(opt => opt.Uploaded).TakeLast(existingFiles.Count - NodeSettings.Configs.MaxDbBackups);
                        foreach (var file in excessFiles)
                            await fileStorage.RemoveAsync(file.FileId).ConfigureAwait(false);
                    }
                    await fileStorage.UploadAsync(encryptedStream, $"{filename}.bin").ConfigureAwait(false);
                }
                File.Delete($"{filename}.bin");
            }
            return true;
        }

        public static async Task<List<FileInfoVm>> GetBackupsListAsync(DatabaseConnectionInfo dbConnection)
        {
            try
            {                
                string dbName = dbConnection.Database;
                return await fileStorage.SearchAsync($"{dbName}_backup_{NodeSettings.Configs.Node.Id}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
                return new List<FileInfoVm>();
            }
        }
    }
}