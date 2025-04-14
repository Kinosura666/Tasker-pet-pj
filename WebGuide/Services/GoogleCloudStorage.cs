using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace WebGuide.Services
{
    public class GoogleCloudStorageService
    {
        private readonly StorageClient _storageClient;
        private readonly string _bucketName;

        public GoogleCloudStorageService(IConfiguration configuration)
        {
            var projectId = configuration["GoogleCloudStorage:ProjectId"];
            _bucketName = configuration["GoogleCloudStorage:BucketName"];
            var credentialFilePath = configuration["GoogleCloudStorage:CredentialFilePath"];

            GoogleCredential credential;

            if (!string.IsNullOrEmpty(credentialFilePath) && File.Exists(credentialFilePath))
            {
                credential = GoogleCredential.FromFile(credentialFilePath);
            }
            else
            {
                var credentialsJson = Environment.GetEnvironmentVariable("GOOGLE_CREDENTIALS_JSON");

                if (string.IsNullOrEmpty(credentialsJson))
                    throw new Exception("GOOGLE_CREDENTIALS_JSON is not set!");

                using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(credentialsJson));
                credential = GoogleCredential.FromStream(stream);
            }

            _storageClient = StorageClient.Create(credential);
        }


        public GoogleCloudStorageService(StorageClient storageClient, string bucketName)
        {
            _storageClient = storageClient;
            _bucketName = bucketName;
        }


        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(fileName);
            await _storageClient.UploadObjectAsync(_bucketName, uniqueFileName, contentType, fileStream);
            return $"https://storage.googleapis.com/{_bucketName}/{uniqueFileName}";
        }
    }
}
