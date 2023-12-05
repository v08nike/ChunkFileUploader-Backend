using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;

namespace ChunkFileUploader.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FileController(IConfiguration configuration, ILogger<FileController> logger) : ControllerBase
    {
        private readonly string _uploadPath = configuration["UploadPath"];
        private readonly ILogger<FileController> _logger = logger;

        [HttpPost]
        [Route("UploadFile")]
        public async Task<IActionResult> UploadFile()
        {

            string strFileInfo = Request.Form["fileInfo"].ToString();

            if (strFileInfo == null)
            {
                LogError("Missing Parameters!");
                return Ok(new { status = "fail", message = "Missing Parameters, Please try again with correct params!" });
            }

            if (!Directory.Exists(_uploadPath))
            {
                Directory.CreateDirectory(_uploadPath);
            }

            List<FileInfomation> fileInfos = JsonConvert.DeserializeObject<List<FileInfomation>>(strFileInfo);

            List<ReturnInfo> returnInfos = new List<ReturnInfo>();

            if (fileInfos != null && fileInfos.Count > 0)
            {
                int index = 0;

                foreach (FileInfomation fileInfo in fileInfos)
                {
                    if (fileInfo.name == "" || fileInfo.currentChunk > fileInfo.totalChunks)
                    {
                        returnInfos.Add(new ReturnInfo
                        {
                            currentChunk = fileInfo.currentChunk,
                            totalChunks = fileInfo.totalChunks,
                            fileIndex = fileInfo.fileIndex,
                            message = "The file information is not correct!",
                            status = "fail",
                            name = fileInfo.name,
                            size = fileInfo.size,
                            tempFileName = ""
                        });

                        Log("The file information is not correct!");

                        index++;
                        continue;
                    }

                    var tempFileName = fileInfo.tempFileName == "" ? fileInfo.name + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + ".temp" : fileInfo.tempFileName;
                    var filePath = Path.Combine(_uploadPath, tempFileName);

                    using (var fileStream = new FileStream(filePath, fileInfo.currentChunk == 0 ? FileMode.Create : FileMode.Append))
                    {
                        await Request.Form.Files[index].CopyToAsync(fileStream);
                    }


                    if (fileInfo.currentChunk == fileInfo.totalChunks)
                    {
                        FileInfo tempFile= new FileInfo(filePath);

                        if (fileInfo.size == tempFile.Length)
                        {
                            string newFileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileInfo.name);
                            string fileExtension = Path.GetExtension(fileInfo.name);
                            string newFileName = $"{newFileNameWithoutExtension}_{((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds()}{fileExtension}";
                            string newFilePath = Path.Combine(_uploadPath, newFileName);

                            System.IO.File.Move(filePath, newFilePath);
                            returnInfos.Add(new ReturnInfo { 
                                currentChunk = fileInfo.currentChunk, 
                                totalChunks = fileInfo.totalChunks, 
                                fileIndex = fileInfo.fileIndex, 
                                message = "File uploaded successfully!", 
                                status = "success",
                                name = fileInfo.name,
                                size = fileInfo.size,
                                tempFileName = tempFileName
                            });

                            Log(fileInfo.name + " File uploaded successfully!");

                            index++;
                            continue;
                        }
                        else
                        {
                            returnInfos.Add(new ReturnInfo
                            {
                                currentChunk = fileInfo.currentChunk,
                                totalChunks = fileInfo.totalChunks,
                                fileIndex = fileInfo.fileIndex,
                                message = "Oops! Something went wrong during the file upload. Please try again.",
                                status = "failed",
                                name = fileInfo.name,
                                size = fileInfo.size,
                                tempFileName = tempFileName
                            });

                            LogError(fileInfo.name + ": Oops! Something went wrong during the file upload!");
                            index++;
                            continue;
                        }
                    }

                    returnInfos.Add(new ReturnInfo
                    {
                        currentChunk = fileInfo.currentChunk,
                        totalChunks = fileInfo.totalChunks,
                        fileIndex = fileInfo.fileIndex,
                        message = "Chunk uploaded successfully!",
                        status = "chunk_file_uploaded",
                        name = fileInfo.name,
                        size = fileInfo.size,
                        tempFileName = tempFileName
                    });

                    Log(fileInfo.name + " Chunk uploaded successfully!");

                    index++;
                }
            }

            return Ok(new { res = JsonConvert.SerializeObject(returnInfos) });
        }

        private void LogError(string errorMessage)
        {
            _logger.LogError(errorMessage);
        }

        private void Log(string errorMessage)
        {
            _logger.LogInformation(errorMessage);
        }


        class FileInfomation
        {
            public required int currentChunk { get; set; }
            public int totalChunks { get; set; }
            public required int size { get; set; }
            public required string name { get; set; }
            public required string tempFileName { get; set; }
            public int fileIndex { get; set; }
        }

        class ReturnInfo
        {
            public required int currentChunk { get; set; }
            public int totalChunks { get; set; }
            public required int size { get; set; }
            public required string name { get; set; }
            public required string tempFileName { get; set; }
            public int fileIndex { get; set; }
            public required string status { get; set; }
            public required string message { get; set; }

        }
    }
}
