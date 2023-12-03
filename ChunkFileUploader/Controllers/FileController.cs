using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ChunkFileUploader.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FileController : ControllerBase
    {
        [HttpPost]
        [Route("UploadFile")]
        public async Task<IActionResult> UploadFile()
        {
            var totalChunks = int.Parse(Request.Form["totalChunks"]);
            var currentChunk = int.Parse(Request.Form["currentChunk"]);
            var totalBytes = int.Parse(Request.Form["totalBytes"]);
            var fileName = Request.Form["fileName"].ToString();

            if (totalChunks >= currentChunk && (fileName != "" || fileName != null)  && Request.Form.Files.Count > 0 && totalBytes > 0)
            {
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");

                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                var filePath = Path.Combine(uploadPath, fileName + ".temp");

                if (System.IO.File.Exists(filePath) && currentChunk == 0)
                {
                    System.IO.File.Delete(filePath);
                }
                
                using (var fileStream = new FileStream(filePath, currentChunk == 0 ? FileMode.Create : FileMode.Append))
                {
                    await Request.Form.Files[0].CopyToAsync(fileStream);
                }

                if (currentChunk == totalChunks)
                {
                    FileInfo fileInfo = new FileInfo(filePath);

                    if (totalBytes == fileInfo.Length)
                    {
                        string newFileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                        string fileExtension = Path.GetExtension(fileName);
                        string newFileName = $"{newFileNameWithoutExtension}_{((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds()}{fileExtension}";
                        string newFilePath = Path.Combine(uploadPath, newFileName);

                        System.IO.File.Move(filePath, newFilePath);
                        return Ok(new { status = "success", message = "File uploaded successfully!" });
                    } else
                    {
                        return Ok(new { status = "failed", message = "Sorry, File is not uploaded correctly! Please try again." });
                    }
                }

                return Ok(new { status = "chunk_file_uploaded", message = "Chunk uploaded successfully!", currentChunk, nextChunk = currentChunk + 1 });
            }

            return Ok(new { status = "failed", message = "Sorry, uploaded file or chunk is invailed. Try again" });
        }
    }
}
