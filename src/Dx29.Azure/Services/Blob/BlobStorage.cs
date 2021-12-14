using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;

namespace Dx29.Services
{
    public partial class BlobStorage
    {
        public BlobStorage(string connectionString)
        {
            BlobServiceClient = new BlobServiceClient(connectionString);
        }

        public BlobServiceClient BlobServiceClient { get; }

        public BlobContainerClient GetContainer(string blobContainerName, PublicAccessType accessType = PublicAccessType.None)
        {
            return BlobServiceClient.GetBlobContainerClient(blobContainerName);
        }

        public async Task<BlobContainerClient> CreateContainerAsync(string blobContainerName, PublicAccessType accessType = PublicAccessType.None)
        {
            var containerClient = BlobServiceClient.GetBlobContainerClient(blobContainerName);
            var response = await containerClient.CreateAsync(accessType);
            return containerClient;
        }

        public async Task<BlobContainerClient> GetOrCreateContainerAsync(string blobContainerName, PublicAccessType accessType = PublicAccessType.None)
        {
            var containerClient = BlobServiceClient.GetBlobContainerClient(blobContainerName);
            var response = await containerClient.CreateIfNotExistsAsync(accessType);
            return containerClient;
        }

        public async Task<(bool, BlobContainerClient)> EnsureContainerAsync(string blobContainerName, PublicAccessType accessType = PublicAccessType.None)
        {
            bool exists = false;
            var containerClient = BlobServiceClient.GetBlobContainerClient(blobContainerName);
            try
            {
                var response = await containerClient.CreateAsync(accessType);
            }
            catch (RequestFailedException ex)
            {
                exists = ex.ErrorCode == "ContainerAlreadyExists";
            }
            return (exists, containerClient);
        }

        //
        //  List Containers
        //
        public IEnumerable<string> ListContainerNames(BlobContainerTraits traits = BlobContainerTraits.None, string prefix = null)
        {
            foreach (var item in BlobServiceClient.GetBlobContainers(traits, prefix: prefix))
            {
                yield return item.Name;
            }
        }

        //
        //  List Blobs
        //
        public async Task<IList<string>> ListBlobsAsync(string blobContainerName, string prefix)
        {
            var names = new List<string>();
            var containerClient = GetContainer(blobContainerName);
            var asyncBlobs = containerClient.GetBlobsAsync(prefix: prefix);
            await foreach (var item in asyncBlobs)
            {
                names.Add(item.Name);
            }
            return names;
        }

        //
        //  Move Blob
        //
        public async Task MoveBlobAsync(string blobContainerName, string source, string target)
        {
            await CopyBlobAsync(blobContainerName, source, target, true);
        }

        //
        //  Copy Blob
        //
        public async Task CopyBlobAsync(string blobContainerName, string source, string target, bool deleteSource = false)
        {
            var containerClient = BlobServiceClient.GetBlobContainerClient(blobContainerName);
            var sourceClient = containerClient.GetBlockBlobClient(source);
            var targetClient = containerClient.GetBlockBlobClient(target);
            await targetClient.StartCopyFromUriAsync(sourceClient.Uri);
            if (deleteSource)
            {
                await sourceClient.DeleteAsync();
            }
        }

        //
        //  Create Blob Share
        //
        public string CreateBlobShare(string blobContainerName, string path, int seconds)
        {
            var containerClient = BlobServiceClient.GetBlobContainerClient(blobContainerName);
            var blobClient = containerClient.GetBlockBlobClient(path);

            var sasUri = blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddSeconds(seconds));
            return sasUri.AbsoluteUri;
        }

        //
        //  Upload Blobs
        //
        public async Task UploadBlobsAsync(string blobContainerName, string path, string folder)
        {
            foreach (var filename in Directory.GetFiles(folder))
            {
                using (var stream = new FileStream(filename, FileMode.Open))
                {
                    await UploadStreamAsync(blobContainerName, $"{path}/{Path.GetFileName(filename)}", stream);
                }
            }
        }

        //
        //  Download Blobs
        //
        public async Task<IList<string>> DownloadBlobsAsync(string blobContainerName, string prefix, string folder)
        {
            int len = prefix.Length + 1;
            var names = new List<string>();
            var containerClient = GetContainer(blobContainerName);
            var asyncBlobs = containerClient.GetBlobsAsync(prefix: prefix);
            await foreach (var item in asyncBlobs)
            {
                names.Add(item.Name);
            }

            var hash = new HashSet<string>();
            var filenames = new List<string>();
            foreach (var name in names)
            {
                string filename = Path.Combine(folder, name.Substring(len));
                filenames.Add(filename);
                string targetPath = Path.GetDirectoryName(filename);
                if (hash.Add(targetPath))
                {
                    Directory.CreateDirectory(targetPath);
                }
                using (var stream = await DownloadStreamAsync(blobContainerName, name))
                {
                    using (var fileStream = new FileStream(filename, FileMode.Create))
                    {
                        await stream.CopyToAsync(fileStream);
                    }
                }
            }

            return filenames;
        }

        //
        //  Upload
        //
        public async Task<BlobContentInfo> UploadObjectAsync(string blobContainerName, string blobName, object value, BlobRequestConditions conditions = null, CancellationToken cancellationToken = default)
        {
            string json = value.Serialize(indented: true);
            return await UploadStringAsync(blobContainerName, blobName, json, conditions, cancellationToken);
        }

        public async Task<BlobContentInfo> UploadStringAsync(string blobContainerName, string blobName, string value, BlobRequestConditions conditions = null, CancellationToken cancellationToken = default)
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(value)))
            {
                return await UploadStreamAsync(blobContainerName, blobName, stream, conditions, cancellationToken);
            }
        }

        public async Task<BlobContentInfo> UploadFileAsync(string blobContainerName, string blobName, string filename, BlobRequestConditions conditions = null, CancellationToken cancellationToken = default)
        {
            using (var stream = new FileStream(filename, FileMode.Open))
            {
                return await UploadStreamAsync(blobContainerName, blobName, stream, conditions, cancellationToken);
            }
        }

        public async Task<BlobContentInfo> UploadStreamAsync(string blobContainerName, string blobName, Stream content, BlobRequestConditions conditions = null, CancellationToken cancellationToken = default)
        {
            var containerClient = await GetOrCreateContainerAsync(blobContainerName);
            var blobClient = containerClient.GetBlobClient(blobName);
            return await blobClient.UploadAsync(content, conditions: conditions, cancellationToken: cancellationToken);
        }

        //
        //  Download
        //
        public async Task<TValue> DownloadObjectAsync<TValue>(string blobContainerName, string blobName, BlobRequestConditions conditions = null, CancellationToken cancellationToken = default)
        {
            string json = await DownloadStringAsync(blobContainerName, blobName, conditions, cancellationToken);
            if (json != null)
            {
                return JsonSerializer.Deserialize<TValue>(json);
            }
            return default(TValue);
        }

        public async Task<string> DownloadStringAsync(string blobContainerName, string blobName, BlobRequestConditions conditions = null, CancellationToken cancellationToken = default)
        {
            var stream = await DownloadStreamAsync(blobContainerName, blobName, conditions, cancellationToken);
            if (stream != null)
            {
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
            return null;
        }

        public async Task<Stream> DownloadStreamAsync(string blobContainerName, string blobName, BlobRequestConditions conditions = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var containerClient = BlobServiceClient.GetBlobContainerClient(blobContainerName);
                var blobClient = containerClient.GetBlockBlobClient(blobName);
                var response = await blobClient.DownloadAsync(cancellationToken);
                return response.Value.Content;
            }
            catch (RequestFailedException ex)
            {
                if (ex.Status == 404)
                {
                    return null;
                }
                throw;
            }
        }

        // Lease
        public BlobLeaseClient GetLeaseClient(string blobContainerName, string blobName)
        {
            var container = BlobServiceClient.GetBlobContainerClient(blobContainerName);
            var blob = container.GetBlobClient(blobName);
            return blob.GetBlobLeaseClient();
        }

        public async Task<BlobLease> AcquireLeaseAsync(BlobLeaseClient leaseClient, int seconds = 15)
        {
            var random = new Random();

            var lease = await TryAcquireLeaseAsync(leaseClient, seconds);
            while (lease == null)
            {
                await Task.Delay(random.Next(500, 1000));
                lease = await TryAcquireLeaseAsync(leaseClient, seconds);
            }
            return lease;
        }
        private async Task<BlobLease> TryAcquireLeaseAsync(BlobLeaseClient leaseClient, int seconds = 15)
        {
            try
            {
                return await leaseClient.AcquireAsync(TimeSpan.FromSeconds(seconds));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        // Delete
        public async Task<Response> DeleteContainerAsync(string blobContainerName, BlobRequestConditions conditions = null, CancellationToken cancellationToken = default)
        {
            var containerClient = BlobServiceClient.GetBlobContainerClient(blobContainerName);
            return await containerClient.DeleteAsync(cancellationToken: cancellationToken);
        }

        public async Task<Response> DeleteBlobAsync(string blobContainerName, string blobName, BlobRequestConditions conditions = null, CancellationToken cancellationToken = default)
        {
            var containerClient = BlobServiceClient.GetBlobContainerClient(blobContainerName);
            var blobClient = containerClient.GetBlockBlobClient(blobName);
            return await blobClient.DeleteAsync(cancellationToken: cancellationToken);
        }
    }
}
