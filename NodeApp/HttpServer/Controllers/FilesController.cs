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
using NodeApp.Blockchain;
using NodeApp.ExceptionClasses;
using NodeApp.Extensions;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services;
using NodeApp.MessengerData.Services;
using NodeApp.Objects;
using ObjectsLibrary;
using ObjectsLibrary.Blockchain.Services;
using ObjectsLibrary.Converters;
using ObjectsLibrary.Exceptions;
using ObjectsLibrary.Interfaces;
using ObjectsLibrary.ResponseClasses;
using ObjectsLibrary.ViewModels;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace NodeApp.HttpServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FilesController : ControllerBase
    {
        private readonly IFileStorage fileStorage;
        private readonly INodeNoticeService nodeNoticeService;
        private readonly IConnectionsService connectionsService;
        private readonly IFilesService filesService;
        private readonly INodesService nodesService;
        private readonly INodeRequestSender nodeRequestSender;

        public FilesController(IFileStorage fileStorage, INodeNoticeService nodeNoticeService, IConnectionsService connectionsService, IFilesService filesService, INodesService nodesService, INodeRequestSender nodeRequestSender)
        {
            this.fileStorage = fileStorage;
            this.nodeNoticeService = nodeNoticeService;
            this.connectionsService = connectionsService;
            this.filesService = filesService;
            this.nodesService = nodesService;
            this.nodeRequestSender = nodeRequestSender;
        }

        [HttpGet("{id}", Name = "Get")]
        public async Task<IActionResult> Get(string id)
        {            
            try
            {
                var fileInfo = await filesService.GetFileInfoAsync(id).ConfigureAwait(false);
                if (fileInfo == null)
                {
                    var nodeConnections = connectionsService.GetNodeConnections().Where(conn => conn.NodeWebSocket.State == WebSocketState.Open);
                    foreach (var nodeConnection in nodeConnections)
                    {
                        var filesInfo = await nodeRequestSender.GetFilesInformationAsync(new List<string> { id }, nodeConnection.Node.Id).ConfigureAwait(false);
                        if (!filesInfo.Any())
                        {
                            continue;
                        }
                        Uri fileUri = new Uri($"https://{nodeConnection.Node.Domains.FirstOrDefault()}:{nodeConnection.Node.NodesPort}/api/Files/{id}");
                        var fileRequest = WebRequest.CreateHttp(fileUri);
                        fileRequest.Method = HttpMethods.Get;
                        var fileResponse = (HttpWebResponse)await fileRequest.GetResponseAsync().ConfigureAwait(false);
                        var contentDisposition = fileResponse.Headers["content-disposition"];
                        string filename = id;
                        if (contentDisposition != null)
                        {
                            System.Net.Mime.ContentDisposition disposition = new System.Net.Mime.ContentDisposition(contentDisposition);
                            filename = disposition.FileName;
                        }
                        return File(fileResponse.GetResponseStream(), MediaTypeNames.Application.Octet, filename);
                    }
                    return StatusCode(StatusCodes.Status404NotFound);
                }
                if (fileInfo.Storage == "Local" || string.IsNullOrWhiteSpace(fileInfo.Storage))
                {
                    LocalFileStorage localFileStorage = new LocalFileStorage();
                    if (fileInfo != null && fileInfo.Url != null)
                    {
                        return File(await localFileStorage.GetStreamAsync(fileInfo.Url).ConfigureAwait(false), MediaTypeNames.Application.Octet, fileInfo.FileName);
                    }
                    else if (fileInfo != null && fileInfo.Url == null && fileInfo.NodeId != NodeSettings.Configs.Node.Id)
                    {
                        NodeVm nodeInformation = await nodesService.GetAllNodeInfoAsync(fileInfo.NodeId).ConfigureAwait(false);
                        Uri fileUri = new Uri($"https://{nodeInformation.Domains.FirstOrDefault()}:{nodeInformation.NodesPort}/api/Files/{fileInfo.Id}");
                        var fileRequest = WebRequest.CreateHttp(fileUri);
                        fileRequest.Method = HttpMethods.Get;
                        var fileResponse = await fileRequest.GetResponseAsync().ConfigureAwait(false);
                        return File(fileResponse.GetResponseStream(), MediaTypeNames.Application.Octet, fileInfo.FileName);
                    }
                    else
                    {
                        return NotFound();
                    }
                }
                else if (!string.IsNullOrWhiteSpace(fileInfo.Storage))
                {
                    var stream = await fileStorage.GetStreamAsync(fileInfo.Id).ConfigureAwait(false);
                    return File(stream, MediaTypeNames.Application.Octet, fileInfo.FileName);
                }
                else
                {
                    return NotFound();

                }
            }
            catch (DownloadFileException ex)
            {
                if (ex.InnerException is WebException webException)
                {
                    Console.WriteLine(ex.InnerException.ToString());
                    return StatusCode(StatusCodes.Status503ServiceUnavailable);
                }
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
            catch (ObjectDoesNotExistsException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromForm]IFormFile file, [FromQuery] bool isDocument = false)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest();
                }
                string fileAccessToken = HttpContext.Request.Headers["FileAccessToken"];
                if (string.IsNullOrWhiteSpace(fileAccessToken))
                {
                    return BadRequest();
                }
                ClientConnection uploaderClient = connectionsService.GetClientConnections()
                        .FirstOrDefault(opt => opt.FileAccessToken == fileAccessToken);
                if (uploaderClient == null)
                {
                    return BadRequest();
                }
                if(uploaderClient.Banned == true || uploaderClient.Confirmed == false)
                {
                    return Forbid();
                }
                Stream uploadStream;
                ImageMetadata imageMetadata = null;
                if (!isDocument && file.TryGetImage(out Image<Rgba32> imageFile, out var imageFormat))
                {
                    if (imageFile.Height > 800 || imageFile.Width > 800)
                    {
                        imageFile.Mutate(opt =>
                        {
                            opt.Resize(new ResizeOptions
                            {
                                Mode = ResizeMode.Max,
                                Size = new SixLabors.Primitives.Size(800)
                            });
                        });
                    }
                    if (!Directory.Exists("tmp"))
                    {
                        Directory.CreateDirectory("tmp");
                    }                    
                    var tempFileName = $"{Guid.NewGuid()}.{imageFormat.Name}";
                    uploadStream = new FileStream($"tmp/{tempFileName}", FileMode.OpenOrCreate, FileAccess.ReadWrite);                    
                    imageFile.Save(uploadStream, imageFormat);
                    imageMetadata = new ImageMetadata
                    {
                        Format = imageFormat.Name,
                        Height = imageFile.Height,
                        Width = imageFile.Width
                    };
                }
                else
                {
                    uploadStream = file.OpenReadStream();
                }
                string storageFileName = $"[{RandomExtensions.NextString(10)}]{file.FileName}";                              
                SHA256 sha256 = SHA256.Create();
                var fileHash = sha256.ComputeHash(uploadStream);
                FileInfoVm fileInfo = null;
                uploadStream.Position = 0;
                string fileUrl;
                string uploadFileName;
                if (fileStorage.GetType() != typeof(LocalFileStorage))
                {
                    fileUrl = string.Empty;                    
                }
                else
                {
                    fileUrl = Path.Combine(NodeSettings.LOCAL_FILE_STORAGE_PATH, storageFileName);                    
                }               
                fileInfo = await filesService.SaveFileAsync(
                        uploaderClient.UserId.GetValueOrDefault(),
                        file.FileName,
                        fileUrl,
                        file.Length,
                        fileHash,
                        fileStorage.StorageName,
                        imageMetadata).ConfigureAwait(false);
                if(fileStorage.GetType() != typeof(LocalFileStorage))
                {
                    uploadFileName = fileInfo.FileId;
                }
                else
                {
                    uploadFileName = storageFileName;
                }
                await fileStorage.UploadAsync(uploadStream, uploadFileName).ConfigureAwait(false);
                var segment = await BlockSegmentsService.Instance.CreateNewFileSegmentAsync(
                    fileInfo,
                    NodeSettings.Configs.Node.Id,
                    NodeData.Instance.NodeKeys.PrivateKey,
                    NodeData.Instance.NodeKeys.SymmetricKey,
                    NodeData.Instance.NodeKeys.Password,
                    NodeData.Instance.NodeKeys.KeyId).ConfigureAwait(false);
                BlockGenerationHelper.Instance.AddSegment(segment);
                nodeNoticeService.SendNewFilesNodeNoticeAsync(fileInfo, segment.PrivateData, NodeData.Instance.NodeKeys.KeyId);
                FileResponse response = new FileResponse(0, fileInfo, null);
                sha256.Dispose();
                uploadStream.Dispose();
                return Content(ObjectSerializer.ObjectToJson(response), MediaTypeNames.Application.Json);
            }
            catch (DownloadFileException ex)
            {
                Logger.WriteLog(ex);
                if (ex.InnerException is WebException webException)
                {
                    return StatusCode(StatusCodes.Status503ServiceUnavailable);
                }
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
