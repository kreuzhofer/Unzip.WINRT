using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using StorageExtensions.WINRT;

namespace Unzip.WINRT
{
    public static class UnZipHelper
    {
        public delegate void ReportUnzipProgress(int minimum, int maximum, int current, TimeSpan timeToGo);

        private static async Task<StorageFolder> CreateSubFolder(StorageFolder baseFolder, string[] subFolders)
        {
            if (subFolders.Length == 1) // only the file is left -> break;
            {
                return baseFolder;
            }
            var subFolder = subFolders[0];
            StorageFolder newFolder = null;
            if (!(await baseFolder.ExistsInFolder(subFolder)))
            {
                newFolder = await baseFolder.CreateFolderAsync(subFolder);
            }
            else
            {
                newFolder = await baseFolder.GetFolderAsync(subFolder);
            }
            var newFolderList = subFolders.Skip(1).ToArray();
            if (newFolder != null)
            {
                return await CreateSubFolder(newFolder, newFolderList);
            }
            return null;
        }

        public static async Task UnZip(StorageFolder outFolder, Stream stream, ReportUnzipProgress reportUnzipProgress)
        {
            bool bDelete = false;
            const int buffersize = 65535;

            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                try
                {
                    var count = archive.Entries.Count;
                    int current = 0;
                    if (reportUnzipProgress != null)
                    {
                        reportUnzipProgress(0, count, current, TimeSpan.MinValue);
                    }
                    Stopwatch watch = Stopwatch.StartNew();
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (entry.Name != "")
                        {
                            var targetFolder = outFolder;
                            string[] parts = entry.FullName.Split(new[]{'/'}, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length > 1) // name contains a path
                            {
                                targetFolder = await CreateSubFolder(outFolder, parts);
                            }
                            string fileName = entry.Name;
                            StorageFile newFile = await CreateFile(targetFolder, fileName);
                            using (Stream newFileStream = await newFile.OpenStreamForWriteAsync())
                            {
                                Stream fileData = entry.Open();
                                byte[] data = new byte[buffersize];
                                int readSize = 0;
                                do
                                {
                                    readSize = await fileData.ReadAsync(data, 0, buffersize);
                                    newFileStream.Write(data, 0, readSize);
                                } while (readSize >0);
                                await newFileStream.FlushAsync();
                            }
                        }
                        current++;
                        var elapsed = watch.ElapsedMilliseconds;
                        var percent = current*100/count;
                        TimeSpan timeSpanToGo = TimeSpan.MinValue;
                        if (percent > 5)
                        {
                            long timeToGo = elapsed*count/current;
                            timeSpanToGo = TimeSpan.FromMilliseconds(Math.Max(0, timeToGo-elapsed));
                        }
                        if (reportUnzipProgress != null)
                        {
                            reportUnzipProgress(0, count, current, timeSpanToGo);
                        }
                    }
                }
                catch (Exception ex)
                {
                    bDelete = true;
                }
                finally
                {
                    if (bDelete)
                        outFolder.DeleteAsync();
                }
            }
        }

        private static async Task<StorageFile> CreateFile(StorageFolder dataFolder, string fileName)
        {
            return await dataFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting).AsTask();

        }
    }
}