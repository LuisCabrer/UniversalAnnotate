using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;

namespace UniversalAnnotator
{
    /// <summary>
    /// Reads files from blob storage.
    /// </summary>
    public class FileManager
    {
        // modify the lines below based on the values you received when the site was configured.
        // note that the connection string is only needed by the sample because it uploads some sample
        // data to create a recommendation model.

        string storageAccountConnectionString; // = @"DefaultEndpointsProtocol=https;AccountName=luiscareco;AccountKey=/v4BhVyWMHo4eG3KjakPaqyvRSZNFgbmiotUBHhOsP0I2nw+L+Br8VBsZStCWAHFTgtQImj5rkdp1chJVaE8yw==;BlobEndpoint=https://luiscareco.blob.core.windows.net/;QueueEndpoint=https://luiscareco.queue.core.windows.net/;TableEndpoint=https://luiscareco.table.core.windows.net/;FileEndpoint=https://luiscareco.file.core.windows.net;";
        string blobContainerName; // = "enricherdemo";

        CloudStorageAccount storageAccount;
        CloudBlobClient blobClient;
        CloudBlobContainer container;


        public FileManager(String storageAccountConnectionString, String blobContainerName)
        {
            this.storageAccountConnectionString = storageAccountConnectionString;
            this.blobContainerName = blobContainerName;

            storageAccount = CloudStorageAccount.Parse(storageAccountConnectionString);
            blobClient = storageAccount.CreateCloudBlobClient();
            container = blobClient.GetContainerReference(blobContainerName);
        }


        public async Task<String> GetFileContent(string uri)
        {
            var blobReference = container.GetBlobReference(uri);
            var stream = await blobReference.OpenReadAsync();

            /*StreamReader reader = new StreamReader(stream);
            string fileContent = reader.ReadToEnd();
            return fileContent;
            */

            string text;
            using (var memoryStream = new MemoryStream())
            {
                await blobReference.DownloadToStreamAsync(memoryStream);
                text = Encoding.UTF8.GetString(memoryStream.ToArray());
            }
            return text;
        }


        public async Task<List<IListBlobItem>> ListBlobsAsync(BlobContinuationToken currentToken)
        {
            CloudBlobDirectory dir = container.GetDirectoryReference("files");
            BlobContinuationToken continuationToken = null;
            List<IListBlobItem> results = new List<IListBlobItem>();
            do
            {
                var response = await dir.ListBlobsSegmentedAsync(true, BlobListingDetails.None, 500, continuationToken, null, null);
                continuationToken = response.ContinuationToken;
                results.AddRange(response.Results);
            }
            while (continuationToken != null);
            return results;
        }

        public async Task<IEnumerable<IListBlobItem>> GetFileList()
        {
            var result = await ListBlobsAsync(null);
            return result;
        }
    }
}
