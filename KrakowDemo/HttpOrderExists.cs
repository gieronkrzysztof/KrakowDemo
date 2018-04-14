
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using Microsoft.WindowsAzure.Storage;
using System;

namespace KrakowDemo
{
    public static class HttpResizePicture
    {
        [FunctionName("HttpResizePicture")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]HttpRequest req,
            [Blob("photos", FileAccess.Read, Connection = "StorageConnection")]CloudBlobContainer photosContainer,
            [Blob("doneorders/{rand-guid}", FileAccess.ReadWrite, Connection = "StorageConnection")]ICloudBlob resizedPhotoCloudBlob,
            TraceWriter log)
        {
            var pictureResizeRequest = GetResizeRequest(req);
            var photoStream = await GetSourcePhotoStream(photosContainer, pictureResizeRequest.FileName);
            SetAttachmentAsContentDisposition(resizedPhotoCloudBlob, pictureResizeRequest);

            var image = Image.Load(photoStream);
            image.Mutate(e => e.Resize(pictureResizeRequest.RequiredWidth, pictureResizeRequest.RequiredHeight));

            var resizedPhotoStream = new MemoryStream();
            image.Save(resizedPhotoStream, new JpegEncoder());
            resizedPhotoStream.Seek(0, SeekOrigin.Begin);

            await resizedPhotoCloudBlob.UploadFromStreamAsync(resizedPhotoStream);

            return new JsonResult(new { FileName = resizedPhotoCloudBlob.Name });
        }

        private static void SetAttachmentAsContentDisposition(ICloudBlob resizedPhotoCloudBlob,
            PictureResizeRequest pictureResizeRequest)
        {
            resizedPhotoCloudBlob.Properties.ContentDisposition =
                $"attachment; filename={pictureResizeRequest.RequiredWidth}x{pictureResizeRequest.RequiredHeight}.jpeg";
        }

        private static async Task<Stream> GetSourcePhotoStream(CloudBlobContainer photosContainer,
            string fileName)
        {
            var photoBlob = await photosContainer.GetBlobReferenceFromServerAsync(fileName);
            var photoStream = await photoBlob.OpenReadAsync(AccessCondition.GenerateEmptyCondition(),
                new BlobRequestOptions(), new OperationContext());
            return photoStream;
        }

        private static PictureResizeRequest GetResizeRequest(HttpRequest req)
        {
            string requestBody = new StreamReader(req.Body).ReadToEnd();
            PictureResizeRequest pictureResizeRequest = JsonConvert.DeserializeObject<PictureResizeRequest>(requestBody);
            return pictureResizeRequest;
        }
    }

    public class PictureResizeRequest
    {
        public string FileName { get; set; }
        public int RequiredWidth { get; set; }
        public int RequiredHeight { get; set; }
    }


    public static class HttpGetSharedAccessSignatureForBlob
    {
        [FunctionName("HttpGetSharedAccessSignatureForBlob")]
        public static async System.Threading.Tasks.Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]HttpRequest req,
            [Blob("doneorders", FileAccess.Read, Connection = "AzureStorageConnectionString")]CloudBlobContainer photosContainer,
            TraceWriter log)
        {
            string fileName = req.Query["fileName"];
            if (string.IsNullOrWhiteSpace(fileName))
                return new BadRequestResult();

            var photoBlob = await photosContainer.GetBlobReferenceFromServerAsync(fileName);
            var photoUri = GetBlobSasUri(photoBlob);
            return new JsonResult(new { PhotoUri = photoUri });
        }

        static string GetBlobSasUri(ICloudBlob cloudBlob)
        {
            SharedAccessBlobPolicy sasConstraints = new SharedAccessBlobPolicy();
            sasConstraints.SharedAccessStartTime = DateTimeOffset.UtcNow.AddHours(-1);
            sasConstraints.SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddHours(24);
            sasConstraints.Permissions = SharedAccessBlobPermissions.List | SharedAccessBlobPermissions.Read;

            string sasToken = cloudBlob.GetSharedAccessSignature(sasConstraints);

            return cloudBlob.Uri + sasToken;
        }
    }
}
