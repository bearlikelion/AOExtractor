/**
 * AO-Extractor
 * A data decryption tool for Albion Online
 * @author: Mark Arneman 
 */

using System;
using System.IO;
using System.Xml;
using System.Linq;
using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;

namespace AOExtractor
{
    class App
    {   
        // Default directory paths
        public static string GameDir = "C:\\Program Files (x86)\\AlbionOnline\\game\\Albion-Online_Data\\StreamingAssets\\GameData";  // TODO: Staging dir
        public static string OutputDir = GameDir + "\\_extracted\\";
        
        // Array of extractable sub-folders
        public static string[] Extractables = { 
            "cluster",
            "templates"
        };

        // Static encryption keys pulled from the game client
        public static byte[] aKey = new byte[] { 48, 239, 114, 71, 66, 242, 4, 50 };
        public static byte[] bKey = new byte[] { 14, 166, 220, 137, 219, 237, 220, 79 };

        /**         
         * Decrypt encoded binary files back to XML          
         */
        public static void extractBin(FileInfo file, string directory)
        {            
            DES dES = new DESCryptoServiceProvider();
            XmlDocument xmlDocument = new XmlDocument();

            FileStream stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
            CryptoStream stream2 = new CryptoStream(stream, dES.CreateDecryptor(App.aKey, App.bKey), CryptoStreamMode.Read);

            GZipStream decompressionStream = new GZipStream(stream2, CompressionMode.Decompress);

            xmlDocument.Load(decompressionStream);
            xmlDocument.Save(App.OutputDir + directory + "\\" + file.Name.Replace(".bin", ".xml"));
            Console.WriteLine("DONE: " + "\\" + directory + "\\" + file.Name);

            // Garbage Collection
            // Since this method is called recursively xmlDocument 
            // re-initializes per call, causing 12,000+ instances using 330 MB of RAM
            // adding the collect/wait after the XML is processed it is released
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        /**         
         * Recursively extract game data directories
         * Search for .bin files -> extract them
         * Search for directories, extract those directories         
         */
        public static void extractDir(DirectoryInfo directory)
        {
            // Array of all *.bin files in current directory
            FileInfo[] binaries = directory.GetFiles("*.bin");

            foreach (FileInfo bin in binaries)
            {
                if (directory.Name == "GameData") // Extract gamedata.bin to output directly 
                {
                    App.extractBin(bin, "");
                }
                else
                {
                    App.extractBin(bin, directory.Name);
                }
                
            }

            // Recursively run the function for sub-folders defined in the static Extractables array
            foreach (DirectoryInfo dir in directory.GetDirectories())
            {
                if (App.Extractables.Contains(dir.Name))
                {                    
                    Directory.CreateDirectory(App.OutputDir + dir.Name); // create sub-folder to extract to
                    App.extractDir(dir);
                }                
            }

        }        

        /**
         * Run App
         */
        static void Main(string[] args)
        {
            // Create AO-Extracted in GameDir
            if (!Directory.Exists(App.OutputDir)) Directory.CreateDirectory(App.OutputDir);

            // Fetch GameDir info and extract files
            DirectoryInfo gameDir = new DirectoryInfo(App.GameDir);
            App.extractDir(gameDir);

            // Open Explorer to the OutputDir
            Process.Start("explorer.exe", App.OutputDir);
        }
    }
}
