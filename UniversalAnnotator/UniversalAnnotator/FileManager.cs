using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;
using PortableCommon.Contract;
using Newtonsoft.Json;
using System.Diagnostics;

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

        string storageAccountConnectionString; 
        string blobContainerName; 

        private Dictionary<string, IndexedDocument> indexedDocuments = new Dictionary<string, IndexedDocument>();

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
            string text = "";
            var blobReference = container.GetBlobReference(uri);

            bool exists = await blobReference.ExistsAsync();

            if (exists)
            {
                var stream = await blobReference.OpenReadAsync();


                using (var memoryStream = new MemoryStream())
                {
                    await blobReference.DownloadToStreamAsync(memoryStream);
                    text = Encoding.UTF8.GetString(memoryStream.ToArray());
                }
            }

            return text;
        }

        public async Task PutFileContent(string uri, String content)
        {
            var blobReference = container.GetBlockBlobReference(uri);
            await blobReference.UploadTextAsync(content);
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


        public async Task<IndexedDocument> GetIndexedDocument(String fileName)
        {
            IndexedDocument result = null;

            if (indexedDocuments.ContainsKey(fileName))
            {
                // If we already created it in the past, just return it.
                result = indexedDocuments[fileName];
            }

            if (result == null)
            {
                string rawText = await GetFileContent(fileName);
                
                if (indexedDocuments.ContainsKey(fileName)) { result = indexedDocuments[fileName]; }

                if( result == null)
                {
                    result = new IndexedDocument(fileName, rawText);
                    indexedDocuments.Add(fileName, result);
                }
            }
            return result;
        }


        public async void InitializeAnnotations()
        {
            string annotationsString = await GetFileContent("allAnnotations/allAnnotations.json");

            List<Annotation> dsallAnnotations = JsonConvert.DeserializeObject<List<Annotation>>(annotationsString);

            if (dsallAnnotations != null)
            {
                foreach (Annotation annotation in dsallAnnotations)
                {
                    // get the file -- or create a new one if it already exists.
                    IndexedDocument newDocument = await GetIndexedDocument(annotation.FileName);
                    newDocument.Annotations.Add(annotation.StartOffset, annotation);
                }
            }
        }

        public async Task<int> AddLearnedAnnotations(string learnedAnnotations)
        {
            string annotationsString = learnedAnnotations;

            List<Annotation> dsallAnnotations = JsonConvert.DeserializeObject<List<Annotation>>(annotationsString);
            int i = 0;

            if (dsallAnnotations != null)
            {   
                for (i = 0; i<dsallAnnotations.Count; i++)
                {
                    // For some reason the iterator was not working well.. doing an old-fashioned for loop to debug.
                    Annotation annotation = dsallAnnotations[i];

                    // get the file -- or create a new one if it already exists.
                    IndexedDocument newDocument = await GetIndexedDocument(annotation.FileName);
                    annotation.AnnotationName = "Learned Date";

                    if (!newDocument.Annotations.ContainsKey(annotation.StartOffset))
                    {
                        newDocument.Annotations.Add(annotation.StartOffset, annotation);
                    }
                }
            }

            return i;
        }


        public async Task<string> SaveAnnotations()
        {
            // Create JSON for all annotations.
            List<Annotation> humanAnnotations = new List<Annotation>();
            List<Annotation> learnedAnnotations = new List<Annotation>();

            foreach (IndexedDocument document in indexedDocuments.Values)
            {
                foreach(Annotation annotation in document.Annotations.Values)
                {
                    if (!annotation.AnnotationName.Contains("Learned"))
                    {
                        humanAnnotations.Add(annotation);
                    }
                    else
                    {
                        learnedAnnotations.Add(annotation);
                    }
                }
            }

            string output = JsonConvert.SerializeObject(learnedAnnotations);
            await PutFileContent("allAnnotations/learnedAnnotations.json", output);

            // now that we flattened all the annotations, let's add them to a JSON file.
            output = JsonConvert.SerializeObject(humanAnnotations);
            await PutFileContent("allAnnotations/allAnnotations.json", output);

            return output;
        }
    }
}
