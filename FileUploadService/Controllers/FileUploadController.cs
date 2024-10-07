using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FileUploadService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileUploadController : ControllerBase
    {
        private readonly BlobServiceClient _blobServiceClient;

        public FileUploadController(BlobServiceClient blobServiceClient)
        {
            _blobServiceClient = blobServiceClient;
        }

        // 1. Upload File (POST /api/fileupload)
        [HttpPost("upload")]
        [Authorize]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var containerClient = _blobServiceClient.GetBlobContainerClient("file-uploads");
            await containerClient.CreateIfNotExistsAsync();

            var blobClient = containerClient.GetBlobClient(Guid.NewGuid().ToString() + "_" + file.FileName);

            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, true);
            }

            return Ok(new { FileName = file.FileName, BlobUrl = blobClient.Uri });
        }

        // 2. Get All Files Metadata (GET /api/fileupload)
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAllFiles()
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient("file-uploads");
            var blobs = containerClient.GetBlobsAsync();
            var fileInfos = new List<object>();

            await foreach (var blobItem in blobs)
            {
                fileInfos.Add(new { blobItem.Name, blobItem.Properties.LastModified });
            }

            return Ok(fileInfos);
        }

        // 3. Get File by Name (GET /api/fileupload/{fileName})
        [HttpGet("{fileName}")]
        [Authorize]
        public IActionResult GetFile(string fileName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient("file-uploads");
            var blobClient = containerClient.GetBlobClient(fileName);

            if (!blobClient.Exists())
                return NotFound("File not found.");

            return Ok(blobClient.Uri);
        }

        // 4. Delete File (DELETE /api/fileupload/{fileName})
        [HttpDelete("{fileName}")]
        [Authorize]
        public async Task<IActionResult> DeleteFile(string fileName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient("file-uploads");
            var blobClient = containerClient.GetBlobClient(fileName);

            if (!blobClient.Exists())
                return NotFound("File not found.");

            await blobClient.DeleteAsync();
            return Ok("File deleted successfully.");
        }
    }
}
