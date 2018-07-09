using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;

namespace SearchForAFile
{
    class Program
    {
        static List<FileInfo> FoundFiles;
        static List<FileSystemWatcher> watchers;
        static FileSystemWatcher newWatcher;
        static List<DirectoryInfo> archiveDirs;

        static void RecursiveSearch(List<FileInfo> foundFiles, string fileName, DirectoryInfo currentDirectory)
        {
            foreach (FileInfo fil in currentDirectory.GetFiles())
            {
                if (fil.Name == fileName)
                    foundFiles.Add(fil);
            }

            foreach (DirectoryInfo dir in currentDirectory.GetDirectories())
            {
                RecursiveSearch(foundFiles, fileName, dir);
            }
        }

        static void WatcherChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                newWatcher.EnableRaisingEvents = false;
            }
            finally
            {
                Console.WriteLine("{0} has been changed with {1}", e.FullPath, e.ChangeType);
                //find the the index of the changed file 
                FileSystemWatcher senderWatcher = (FileSystemWatcher)sender;
                int index = watchers.IndexOf(senderWatcher, 0);
                //now that we have the index, we can archive the file
                try
                {
                    ArchiveFile(archiveDirs[index], FoundFiles[index]);
                }
                catch (NullReferenceException ex)
                {
                    throw ex;
                }
                catch (IOException eio)
                {
                    throw eio;
                }
                newWatcher.EnableRaisingEvents = true;
            }
        }

        static void WatcherNameChanged(object sender, RenamedEventArgs e)
        {
            try
            {
                newWatcher.EnableRaisingEvents = false;
            }
            finally
            {
                Console.WriteLine("{0} has been changed with {1}", e.OldName, e.Name);
                newWatcher.EnableRaisingEvents = true;
            }

        }

        static void ArchiveFile(DirectoryInfo archiveDir, FileInfo fileToArchive)
        {
            FileStream originalFileStream = fileToArchive.OpenRead();

            if ((File.GetAttributes(fileToArchive.FullName) & FileAttributes.Hidden) != FileAttributes.Hidden & fileToArchive.Extension != ".gz")
            {
                using (FileStream compressedFileStream = File.Create(fileToArchive.FullName + ".gz"))
                {
                    using (GZipStream compressionStream = new GZipStream(compressedFileStream,
                        CompressionMode.Compress))
                    {
                        originalFileStream.CopyTo(compressionStream);
                    }
                }
                FileInfo info = new FileInfo(archiveDir + "\\" + fileToArchive.Name + ".gz");
                Console.WriteLine("archive completed");
            }
            originalFileStream.Close();
        }

        static void Main(string[] args)
        {
            string fileName = args[0];
            string directoryName = args[1];
            FoundFiles = new List<FileInfo>();
            watchers = new List<FileSystemWatcher>();

            //examine if the given directory exists at all
            DirectoryInfo rootDir = new DirectoryInfo(directoryName);
            if (!rootDir.Exists)
            {
                Console.WriteLine("The specified directory does not exist.");
                return;
            }

            //search recursively for the mathing files
            RecursiveSearch(FoundFiles, fileName, rootDir);
            //list the found files
            Console.WriteLine("Found {0} files.", FoundFiles.Count);
            foreach (FileInfo fil in FoundFiles)
            {
                Console.WriteLine("{0}", fil.FullName);
            }

            Console.WriteLine();

            foreach (FileInfo fil in FoundFiles)
            {
                newWatcher = new FileSystemWatcher(fil.DirectoryName, fil.Name);
                newWatcher.Changed += new FileSystemEventHandler(WatcherChanged);
                newWatcher.Renamed += new RenamedEventHandler(WatcherNameChanged);
                newWatcher.Deleted += new FileSystemEventHandler(WatcherChanged);
                newWatcher.EnableRaisingEvents = true;
                watchers.Add(newWatcher);
            }

            archiveDirs = new List<DirectoryInfo>();
            //create archive directories
            for (int i = 0; i < FoundFiles.Count; i++)
            {
                archiveDirs.Add(Directory.CreateDirectory("archive" + i.ToString()));
            }

            Console.ReadKey();
        }
    }
}
