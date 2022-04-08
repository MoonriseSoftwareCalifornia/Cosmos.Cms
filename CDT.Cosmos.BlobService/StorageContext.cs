using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CDT.Cosmos.BlobService.Drivers;
using CDT.Cosmos.BlobService.Models;
using CDT.Cosmos.Cms.Common.Services.Configurations;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace CDT.Cosmos.BlobService
{
    /// <summary>
    ///     Multi cloud blob service context
    /// </summary>
    public class StorageContext
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="cosmosConfig"></param>
        /// <param name="cache"></param>
        public StorageContext(IOptions<CosmosConfig> cosmosConfig, IMemoryCache cache)
        {
            _config = cosmosConfig;
            _memoryCache = cache;
        }

        /// <summary>
        ///     Determine if this service is configured
        /// </summary>
        /// <returns></returns>
        public bool IsConfigured()
        {
            // Are there configuration settings at all?
            if (_config.Value == null || _config.Value.StorageConfig == null) return false;

            // If both AWS and Azure blob storage settings are provided,
            // make sure the distributed cache is provided and there is a primary provider.
            if (_config.Value.StorageConfig.AmazonConfigs.Any() && _config.Value.StorageConfig.AzureConfigs.Any())
                return _memoryCache != null && string.IsNullOrEmpty(_config.Value.PrimaryCloud) == false;

            // If just AWS is configured, make sure distributed cache exists, as this is needed for uploading files.
            if (_config.Value.StorageConfig.AmazonConfigs.Any()) return _memoryCache != null;

            // Finally, make sure at the very least, Azure blob storage is
            return _config.Value.StorageConfig.AzureConfigs.Any();
        }

        /// <summary>
        ///     Determines if a blob exists.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task<bool> BlobExistsAsync(string path)
        {
            var driver = GetPrimaryDriver();
            return await driver.BlobExistsAsync(path);
        }

        /// <summary>
        ///     Copies a file or folder.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        public async Task CopyAsync(string target, string destination)
        {
            await CopyObjectsAsync(target, destination, false);
        }

        /// <summary>
        ///     Delete a folder
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        public async Task DeleteFolderAsync(string folder)
        {
            // Ensure leading slash is removed.
            folder = folder.TrimStart('/');

            var blobs = await GetBlobNamesByPath(folder);

            foreach (var blob in blobs) DeleteFile(blob);
        }

        /// <summary>
        ///     Deletes a file
        /// </summary>
        /// <param name="target"></param>
        public void DeleteFile(string target)
        {
            // Ensure leading slash is removed.
            target = target.TrimStart('/');

            var drivers = GetDrivers();
            var tasks = new List<Task>();
            foreach (var cosmosStorage in drivers) tasks.Add(cosmosStorage.DeleteIfExistsAsync(target));

            Task.WaitAll(tasks.ToArray());
        }

        /// <summary>
        ///     Gets a file
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public async Task<FileManagerEntry> GetFileAsync(string target)
        {
            // Ensure leading slash is removed.
            target = target.TrimStart('/');

            FileManagerEntry fileManagerEntry;
            switch (_config.Value.PrimaryCloud.ToLower())
            {
                case "amazon":
                {
                    var driver = (AmazonStorage) GetPrimaryDriver();
                    var blob = await driver.GetBlobAsync(target);

                    var isDirectory = blob.Key.EndsWith("folder.stubxx");
                    var hasDirectories = false;
                    var fileName = Path.GetFileName(blob.Key);
                    var path = blob.Key;
                    if (isDirectory)
                    {
                        var children = await driver.GetObjectsAsync(target, null);
                        hasDirectories = children.Count(c => c.Key.EndsWith("folder.stubxx")) > 1;
                        fileName = ParseFirstFolder(blob.Key)[0];
                        path = path?.Replace("/folder.stubxx", "");
                    }

                    fileManagerEntry = new FileManagerEntry
                    {
                        Created = blob.LastModified.Date,
                        CreatedUtc = blob.LastModified.ToUniversalTime(),
                        Extension = isDirectory ? "" : Path.GetExtension(blob.Key),
                        HasDirectories = hasDirectories,
                        IsDirectory = isDirectory,
                        Modified = blob.LastModified.Date,
                        ModifiedUtc = blob.LastModified.ToUniversalTime(),
                        Name = fileName,
                        Path = path,
                        Size = blob.ContentLength
                    };
                }
                    break;
                case "azure":
                {
                    var driver = (AzureStorage) GetPrimaryDriver();
                    var blob = await driver.GetBlobAsync(target);
                    var isDirectory = blob.Name.EndsWith("folder.stubxx");
                    var hasDirectories = false;
                    var fileName = Path.GetFileName(blob.Name);
                    var path = blob.Name;

                    if (isDirectory)
                    {
                        var children = await driver.GetObjectsAsync(target);
                        hasDirectories = children.Any(c => c.IsPrefix);
                        fileName = ParseFirstFolder(blob.Name)[0];
                        path = path?.Replace("/folder.stubxx", "");
                    }

                    var props = await blob.GetPropertiesAsync();


                    fileManagerEntry = new FileManagerEntry
                    {
                        Created = props.Value.CreatedOn.DateTime,
                        CreatedUtc = props.Value.CreatedOn.UtcDateTime,
                        Extension = isDirectory ? "" : Path.GetExtension(blob.Name),
                        HasDirectories = hasDirectories,
                        IsDirectory = isDirectory,
                        Modified = props.Value.LastModified.DateTime,
                        ModifiedUtc = props.Value.LastModified.UtcDateTime,
                        Name = fileName,
                        Path = path,
                        Size = props.Value.ContentLength
                    };
                }
                    break;
                default:
                    throw new Exception($"Primary provider '{_config.Value.PrimaryCloud}' not supported.");
            }

            return fileManagerEntry;
        }

        /// <summary>
        ///     Returns a response stream from the primary blob storage provider.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public async Task<Stream> OpenBlobReadStreamAsync(string target)
        {
            // Ensure leading slash is removed.
            target = target.TrimStart('/');

            switch (_config.Value.PrimaryCloud.ToLower())
            {
                case "amazon":
                {
                    var driver = (AmazonStorage) GetPrimaryDriver();
                    var blob = await driver.GetBlobAsync(target);
                    return blob.ResponseStream;
                }
                case "azure":
                {
                    var driver = (AzureStorage) GetPrimaryDriver();
                    var blob = await driver.GetBlobAsync(target);
                    return await blob.OpenReadAsync();
                }
                default:
                    throw new Exception($"Primary provider '{_config.Value.PrimaryCloud}' not supported.");
            }
        }

        /// <summary>
        ///     Renames a file or folder.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        public async Task RenameAsync(string target, string destination)
        {
            await CopyObjectsAsync(target, destination, true);
        }

        #region PRIVATE FIELDS AND METHODS

        private readonly IOptions<CosmosConfig> _config;
        private readonly IMemoryCache _memoryCache;

        private async Task CopyObjectsAsync(string target, string destination, bool deleteSource)
        {
            // Make sure leading slashes are removed.
            target = target.TrimStart('/');
            destination = destination.TrimStart('/');

            if (string.IsNullOrEmpty(target)) throw new Exception("Cannot move the root folder.");

            var drivers = GetDrivers();

            //
            // Get a list of blobs to rename
            //
            var blobNames = await GetBlobNamesByPath(target);

            //
            // Work through the list here
            //
            foreach (var srcBlobName in blobNames)
            {
                var tasks = new List<Task>();

                var destBlobName = srcBlobName.Replace(target, destination);

                //
                // Check to see if the destination already exists
                //
                foreach (var cosmosStorage in drivers)
                    if (await cosmosStorage.BlobExistsAsync(destBlobName))
                        throw new Exception($"Could not copy {srcBlobName} as {destBlobName} already exists.");

                //
                // Copy the blob here
                //
                foreach (var cosmosStorage in drivers)
                    tasks.Add(cosmosStorage.CopyBlobAsync(srcBlobName, destBlobName));
                Task.WaitAll(tasks.ToArray());

                //
                // Now check to see if files were copied
                //
                var success = true;

                foreach (var cosmosStorage in drivers) success = await cosmosStorage.BlobExistsAsync(destBlobName);

                if (success)
                {
                    //
                    // Deleting the source is in the case of RENAME.
                    // Copying things does not delete the source
                    if (deleteSource)
                        //
                        // The copy was successful, so delete the old files
                        //
                        foreach (var cosmosStorage in drivers)
                            await cosmosStorage.DeleteIfExistsAsync(srcBlobName);
                }
                else
                {
                    //
                    // The copy was NOT successfull, delete any copied files and halt, throw an error.
                    //
                    foreach (var cosmosStorage in drivers) await cosmosStorage.DeleteIfExistsAsync(destBlobName);
                    throw new Exception($"Could not copy: {srcBlobName} to {destBlobName}");
                }
            }
        }

        private async Task<List<string>> GetBlobNamesByPath(string path, string[] filter = null)
        {
            var primaryDriver = GetPrimaryDriver();
            return await primaryDriver.GetBlobNamesByPath(path, filter);
        }

        private List<ICosmosStorage> GetDrivers()
        {
            var drivers = new List<ICosmosStorage>();
            switch (_config.Value.PrimaryCloud.ToLower())
            {
                case "amazon":
                {
                    foreach (var storageConfigAmazonConfig in _config.Value.StorageConfig.AmazonConfigs)
                        drivers.Add(new AmazonStorage(storageConfigAmazonConfig, _memoryCache));

                    if (_config.Value.StorageConfig.AzureConfigs.Any())
                        foreach (var storageConfigAzureConfig in _config.Value.StorageConfig.AzureConfigs)
                            drivers.Add(new AzureStorage(storageConfigAzureConfig));

                    break;
                }
                case "azure":
                {
                    foreach (var storageConfigAzureConfig in _config.Value.StorageConfig.AzureConfigs)
                        drivers.Add(new AzureStorage(storageConfigAzureConfig));

                    if (_config.Value.StorageConfig.AmazonConfigs.Any())
                        foreach (var storageConfigAmazonConfig in _config.Value.StorageConfig.AmazonConfigs)
                            drivers.Add(new AmazonStorage(storageConfigAmazonConfig, _memoryCache));

                    break;
                }
                default:
                    throw new Exception("No primary storage provider defined.");
            }

            return drivers;
        }

        private ICosmosStorage GetPrimaryDriver()
        {
            switch (_config.Value.PrimaryCloud.ToLower())
            {
                case "amazon":
                    return new AmazonStorage(_config.Value.StorageConfig.AmazonConfigs.FirstOrDefault(),
                        _memoryCache);
                case "azure":
                    return new AzureStorage(_config.Value.StorageConfig.AzureConfigs.FirstOrDefault());
                default:
                    throw new Exception($"Primary provider '{_config.Value.PrimaryCloud}' is not supported.");
            }
        }

        private string[] ParseFirstFolder(string path)
        {
            var parts = path.Split('/');
            return parts;
        }

        #endregion

        #region FILE MANAGER FUNCTION

        /// <summary>
        ///     Append bytes to blob(s)
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="fileMetaData"></param>
        /// <returns></returns>
        public void AppendBlob(MemoryStream stream, FileUploadMetaData fileMetaData)
        {
            var mark = DateTimeOffset.UtcNow;

            var drivers = GetDrivers();
            var data = stream.ToArray();
            var uploadTasks = new List<Task>();
            foreach (var cosmosStorage in drivers)
            {
                var cloneArray = data.ToArray();
                uploadTasks.Add(cosmosStorage.AppendBlobAsync(cloneArray, fileMetaData, mark));
            }

            // Wait for all the tasks to complete
            Task.WaitAll(uploadTasks.ToArray());
        }

        /// <summary>
        ///     Creates a folder in all the cloud storage accounts
        /// </summary>
        /// <param name="folderName"></param>
        /// <returns></returns>
        /// <remarks>Creates the folder if it does not already exist.</remarks>
        public FileManagerEntry CreateFolder(string folderName)
        {
            var drivers = GetDrivers();

            var tasks = new List<Task>();

            var primary = GetPrimaryDriver();
            if (!primary.BlobExistsAsync(folderName + "/folder.stubxx").Result)
            {
                foreach (var cosmosStorage in drivers) tasks.Add(cosmosStorage.CreateFolderAsync(folderName));

                Task.WaitAll(tasks.ToArray());
                var parts = folderName.TrimEnd('/').Split('/');

                return new FileManagerEntry
                {
                    Name = parts.Last(),
                    Path = folderName,
                    Created = DateTime.UtcNow,
                    CreatedUtc = DateTime.UtcNow,
                    Extension = "",
                    HasDirectories = false,
                    Modified = DateTime.UtcNow,
                    ModifiedUtc = DateTime.UtcNow,
                    IsDirectory = true,
                    Size = 0
                };
            }

            var results = GetFolderContents(folderName).Result;

            var folder = results.FirstOrDefault(f => f.Path == folderName);

            return folder;
        }

        /// <summary>
        ///     Gets files and subfolders for a given path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task<List<FileManagerEntry>> GetObjectsAsync(string path)
        {
            if (!string.IsNullOrEmpty(path)) path = path.TrimStart('/');
            var entries = new List<FileManagerEntry>();

            switch (_config.Value.PrimaryCloud.ToLower())
            {
                case "amazon":
                    var amazonDriver = (AmazonStorage) GetPrimaryDriver();

                    var amazonResults = await amazonDriver.GetObjectsAsync(path, null);

                    var length = path.Length;

                    foreach (var amazonResult in amazonResults)
                    {
                        var keyParts = amazonResult.Key.Substring(length).Split('/');

                        if (keyParts[0] == "folder.stubxx") continue;

                        //
                        // Is this a folder stub?
                        //
                        if (keyParts.Last() == "folder.stubxx" && entries.All(a => a.Name != keyParts[0]))
                        {
                            var blobPath = path.TrimEnd('/') + "/" + keyParts[0];

                            var children = await amazonDriver.GetObjectsAsync(blobPath, null);

                            // Add the folder here
                            entries.Add(new FileManagerEntry
                            {
                                Created = amazonResult.LastModified,
                                CreatedUtc = amazonResult.LastModified.ToUniversalTime(),
                                Extension = string.Empty,
                                HasDirectories = children.Any(a => a.Key == "folder.stubxx"),
                                IsDirectory = true,
                                Modified = amazonResult.LastModified,
                                ModifiedUtc = amazonResult.LastModified.ToUniversalTime(),
                                Name = keyParts[0],
                                Path = blobPath,
                                Size = 0
                            });
                        }
                        else if (keyParts.Length < 3)
                        {
                            var fileName = Path.GetFileNameWithoutExtension(amazonResult.Key);
                            var extension = Path.GetExtension(fileName);


                            var blobPath = path.TrimEnd('/') + "/" + fileName;

                            if (entries.All(a => a.Path != blobPath))
                                entries.Add(new FileManagerEntry
                                {
                                    Created = amazonResult.LastModified,
                                    CreatedUtc = amazonResult.LastModified.ToUniversalTime(),
                                    Extension = extension,
                                    HasDirectories = false,
                                    IsDirectory = false,
                                    Modified = amazonResult.LastModified,
                                    ModifiedUtc = amazonResult.LastModified.ToUniversalTime(),
                                    Name = fileName,
                                    Path = blobPath,
                                    Size = amazonResult.Size
                                });
                        }
                    }

                    break;
                case "azure":
                    var azureDriver = (AzureStorage) GetPrimaryDriver();

                    var azureResults = await azureDriver.GetObjectsAsync(path);

                    foreach (var azureResult in azureResults)
                        if (azureResult.IsBlob)
                        {
                            if (azureResult.Blob.Name.EndsWith("folder.stubxx")) continue;

                            var fileName = Path.GetFileNameWithoutExtension(azureResult.Blob.Name);

                            var modified = azureResult.Blob.Properties.LastModified?.UtcDateTime ?? DateTime.UtcNow;

                            entries.Add(new FileManagerEntry
                            {
                                Created = DateTime.Now,
                                CreatedUtc = DateTime.UtcNow,
                                Extension = Path.GetExtension(azureResult.Blob.Name),
                                HasDirectories = false,
                                IsDirectory = false,
                                Modified = modified,
                                ModifiedUtc = modified,
                                Name = fileName,
                                Path = azureResult.Blob.Name,
                                Size = azureResult.Blob.Properties.ContentLength ?? 0
                            });
                        }
                        else
                        {
                            var parse = azureResult.Prefix.TrimEnd('/').Split('/');

                            var subDirectory = await azureDriver.GetObjectsAsync(azureResult.Prefix);

                            entries.Add(new FileManagerEntry
                            {
                                Created = DateTime.Now,
                                CreatedUtc = DateTime.UtcNow,
                                Extension = string.Empty,
                                HasDirectories = subDirectory.Any(a => a.IsPrefix),
                                IsDirectory = true,
                                Modified = DateTime.Now,
                                ModifiedUtc = DateTime.UtcNow,
                                Name = parse.Last(),
                                Path = azureResult.Prefix.TrimEnd('/'),
                                Size = 0
                            });
                        }

                    break;
                default:
                    throw new Exception($"Provider '{_config.Value.PrimaryCloud}' is not supported.");
            }

            return entries;
        }

        /// <summary>
        ///     Gets the contents for a folder
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        public async Task<List<FileManagerEntry>> GetFolderContents(string folder)
        {
            if (!string.IsNullOrEmpty(folder))
            {
                folder = folder.TrimStart('/');

                if (folder == "/")
                {
                    folder = "";
                }
                else
                {
                    if (!folder.EndsWith("/")) folder = folder + "/";
                }
            }

            return await GetObjectsAsync(folder);
        }

        #endregion
    }
}