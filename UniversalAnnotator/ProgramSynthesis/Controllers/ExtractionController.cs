
namespace ProgramSynthesis.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Web.Configuration;
    using System.Web.Http;
    using Microsoft.ProgramSynthesis.Utils;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;
    using PortableCommon.Contract;
    using ProgramSynthesis.Utilities;

    public class ExtractionController : ApiController
    {
        private readonly CloudBlobClient storageClient;

        private int NonLabeledSampleSize = 20;
        private string programsFolder = "programs";

        public ExtractionController()
        {
            var storageAccount = CloudStorageAccount.Parse(WebConfigurationManager.AppSettings["StorageConnectionString"] ?? "UseDevelopmentStorage=true;");
            this.storageClient = storageAccount.CreateCloudBlobClient();
        }

        // POST api/train
        [HttpPost]
        public async Task<Guid> Train([FromUri] string blobContainerName, 
                                      [FromUri] string trainDirectory,
                                      [FromBody] IEnumerable<Annotation> annotations)
        {
            // Check annoations
            var annotationsArr = annotations as Annotation[] ?? annotations.ToArray();
            if (!annotationsArr.Any())
            {
                throw new AggregateException("No annotations provided for learning");
            }

            // Check blobs folder
            var storageContainer = this.storageClient.GetContainerReference(blobContainerName);
            if (!storageContainer.Exists())
            {
                throw new AggregateException($"{blobContainerName} does not exists");
            }

            var blobs = storageContainer.ListBlobs().ToArray();

            var dir = storageContainer.GetDirectoryReference(trainDirectory);

            blobs = dir.ListBlobs().ToArray();

            if (!blobs.Any())
            {
                throw new AggregateException($"No blobs found in blobs container: {blobContainerName}  folder: {trainDirectory}");
            }

            // Read annotations
            var annotatedSamplesContentFetchingTasks = annotationsArr.Select(async annotation =>
            {
                var blobRef = storageContainer.GetBlobReference(annotation.FileName);
                using (var stream = new MemoryStream())
                {
                    await blobRef.DownloadToStreamAsync(stream);
                    stream.Position = 0;
                    using (var sr = new StreamReader(stream))
                    {
                        var content = await sr.ReadToEndAsync();
                        return new Tuple<string, uint, uint>(content, (uint)annotation.StartOffset, (uint)annotation.EndOffset);
                    }
                }
            });

            var annotatedSampleFileNames = annotationsArr.Select(a => a.FileName).ToHashSet();
            var nonLabeledSampleBlobs = blobs.Where(b => !annotatedSampleFileNames.Contains(((CloudBlockBlob) b).Name)).Take(NonLabeledSampleSize);

            var nonLabeledSamplesContentFetchingTasks = nonLabeledSampleBlobs.Select(async b =>
            {
                var blob = (CloudBlockBlob)b;
                return await blob.DownloadTextAsync();
            });

            var examples = await Task.WhenAll(annotatedSamplesContentFetchingTasks);
            var nonLabeledSamples = await Task.WhenAll(nonLabeledSamplesContentFetchingTasks);

            // Train
            var extractor = await StructureExtractor.TrainExtractorAsync(examples, nonLabeledSamples);

            // Save program
            var serializedExtractor = extractor.Serialize();
            var extractorGuid = Guid.NewGuid();
            var blockBlob = storageContainer.GetBlockBlobReference($"{programsFolder}/{extractorGuid:D}");
            await blockBlob.UploadTextAsync(serializedExtractor);

            return extractorGuid;
        }

        [HttpGet]
        public async Task<IEnumerable<Annotation>> Extract([FromUri] string blobContainerName,
                                                           [FromUri] string scoreDirectory, 
                                                           [FromUri] Guid programId)
        {
            // Check blobs folder
            var storageContainer = this.storageClient.GetContainerReference(blobContainerName);
            if (!storageContainer.Exists())
            {
                throw new AggregateException($"{blobContainerName} does not exists");
            }

            var dir = storageContainer.GetDirectoryReference(scoreDirectory);

            //var blobs = dir.ListBlobs().ToArray();

            var blobFiles = dir.ListBlobs().Where(b => b as CloudBlockBlob != null).ToArray();
            if (!blobFiles.Any())
            {
                throw new AggregateException($"No blobs found in blobs folder {scoreDirectory}");
            }

            var programBlobRef = storageContainer.GetBlobReference($"{programsFolder}/{programId:D}");
            if (!programBlobRef.Exists())
            {
                throw new AggregateException($"Program with id {programId:D} not found");
            }

            // Load program
            string serializedProgram;
            using (var stream = new MemoryStream())
            {
                await programBlobRef.DownloadToStreamAsync(stream);
                stream.Position = 0;
                using (var sr = new StreamReader(stream))
                {
                    serializedProgram = await sr.ReadToEndAsync();
                }
            }

            var extractor = StructureExtractor.Deserialize(serializedProgram);

            // Extraction
            var extractionTasks = blobFiles.Select(async b =>
            {
                var blob = (CloudBlockBlob)b;
                var content = await blob.DownloadTextAsync();
                var stringRegion = extractor.Extract(content);

                if (stringRegion == null || string.IsNullOrEmpty(stringRegion.Value))
                {
                    return new Annotation
                    {
                        FileName = blob.Name
                    };
                }

                return new Annotation
                {
                    FileName = blob.Name,
                    StartOffset = (int) stringRegion.Start,
                    EndOffset = (int) stringRegion.End
                };
            });

            return await Task.WhenAll(extractionTasks);
        }
    }
}
