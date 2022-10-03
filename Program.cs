using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;

namespace resource_merger
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // === Utility wireframe ===
            // --- Phase 1 - Resx ---
            // 1. Input a resx file and source file directory
            // 2. Find duplicates (store duplicates in memory for future use)
            // 3. Merge duplicates into single keypair and write to resx
            // --- Phase 2 - Source Updates ---
            // 4. Iterate over all files
            // 5. Perform duplicate substitutions
            // 6. Save back down

            // --- Intro ---
            Console.Write(ASCII.BANNER);
            Console.WriteLine();
            Console.WriteLine("===== WARNING =====");
            Console.WriteLine("This tool rewrites files!");
            Console.WriteLine();
            Console.WriteLine("Firearm rules apply: do not point this tool at anything you don't want to destroy!");
            Console.WriteLine("No reversion mechanism is provided; it is advised to only use this tool in");
            Console.WriteLine("directories which are managed by source control!");
            Console.WriteLine();
            Console.WriteLine("If you understand the above, press any key to continue!");
            Console.ReadKey(true);
            Console.Clear();

            // --- Phase 1 - Resx ---
            // Step 1. Input target directories.

            Console.WriteLine("Please specify a resx file.");
            var resxPath = Console.ReadLine();
            Console.WriteLine("Please specify a source directory to update.");
            var sourceDir = Console.ReadLine();

            // Step 2. Find duplicates
            //Caution in naming here; values become keys, keys become values.
            Dictionary<string, List<string>> duplicates = new Dictionary<string, List<string>>();
            List<ResXDataNode> resx;
            using (var reader = new ResXResourceReader(resxPath))
            {
                var toRemove = new List<ResXDataNode>();
                reader.UseResXDataNodes = true;
                resx = reader.Cast<DictionaryEntry>().Select(x => (ResXDataNode)x.Value).ToList();

                foreach (var item in resx)
                {
                    string resourceKey = (string)item.Name;
                    string resourceValue = (string)item.GetValue((ITypeResolutionService)null).ToString();
                    //Have we seen this resx value before?
                    //If yes, add this resx-key to this entry.
                    if (duplicates.ContainsKey(resourceValue))
                    {
                        duplicates[resourceValue].Add(resourceKey);
                        PrintDuplicateDetected(duplicates[resourceValue], resourceValue);
                    }
                    //If no, add an entry to the duplicates dictionary.
                    else
                    {
                        duplicates[resourceValue] = new List<string>() { resourceKey };
                    }
                }

                //Calculate which to remove
                foreach(var dupe in duplicates)
                {
                    var bestKey = SelectBestKey(dupe.Value);
                    var redundants = dupe.Value.Where(x => !x.Equals(bestKey));
                    foreach(var redundant in redundants)
                    {
                        var node = resx.Where(x => x.Name == redundant).SingleOrDefault();
                        toRemove.Add(node);
                    }
                }

                //Trim local resx ready for write
                foreach (var item in toRemove)
                {
                    resx.Remove(item);
                }
            }
            //Trim the duplicates dictionary to only entries with more than one key for the value.
            //Notice how this is getting semantically difficult as we've flipped the key/value relationship.
            duplicates = duplicates.Where(x => x.Key.Count() > 1).ToDictionary(x => x.Key, x => x.Value);

            // Step 3. Merge duplicates down and write to resx
            //TODO - Check that writing to an existing resx file is supported
            using (var writer = new ResXResourceWriter(resxPath))
            {
                resx.ForEach(resxNode =>
                {
                    // Again Adding all resource to generate with final items
                    writer.AddResource(resxNode);
                });
                writer.Generate();
            }

            Console.WriteLine();
            Console.WriteLine("Performing source file substitutions.");
            Console.WriteLine();

            // --- Phase 2 - Source Updates ---
            var allowedExtensions = new[] { ".cs", ".cshtml" };
            var files = Directory.GetFileSystemEntries(sourceDir, "*", SearchOption.AllDirectories)
                .Where(file => allowedExtensions.Any(file.ToLower().EndsWith))
                .ToList();

            // Step 4. Iterate over all files
            foreach (var filePath in files)
            {
                bool dirty = false;
                Encoding encoding = GetEncoding(files[0]);

                //Read out contents
                string fileContents = File.ReadAllText(filePath);

                // Step 5. Perform duplicate substitutions
                //fileContents = fileContents.Replace("some text", "some other text");
                foreach (var dupe in duplicates)
                {
                    var newKey = SelectBestKey(dupe.Value);
                    var defunctKeys = dupe.Value.Where(x => x != newKey);
                    foreach (var dupeKey in defunctKeys)
                    {
                        //Checking the file for the dupe key first means we don't do
                        //unneeded writes
                        if (fileContents.Contains(dupeKey))
                        {
                            fileContents = fileContents.Replace(dupeKey, newKey);
                            PrintDupeReplaced(filePath, dupeKey, newKey);
                            dirty = true;
                        }
                    }
                }

                // Step 6. Save back down
                if (dirty)
                {
                    File.WriteAllText(filePath, fileContents, encoding);
                }
            }

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
            WriteDuplicateLog(duplicates);
            Console.WriteLine("Resource merging successfully completed.");
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey(true);
        }

        private static void WriteDuplicateLog(Dictionary<string, List<string>> duplicates)
        {
            string log = "";

            foreach (var dupe in duplicates.Where(x => x.Value.Count() > 1))
            {
                log += "DUPLICATE" + Environment.NewLine;
                log += "VALUE: " + dupe.Key + Environment.NewLine;
                foreach(string key in dupe.Value)
                {
                    log += "FOUND KEY: " + key + Environment.NewLine;
                }
                log += "SELECTED KEY: " + SelectBestKey(dupe.Value) + Environment.NewLine;
                log += Environment.NewLine;
            }

            File.WriteAllText(AppContext.BaseDirectory + "\\merger-log.txt", log);
        }

        private static void PrintDuplicateDetected(List<string> keys, string value)
        {
            Console.WriteLine("--- DUPLICATE DETECTED ---");
            Console.WriteLine("VALUE: " + value);
            foreach (var key in keys)
            {
                Console.WriteLine("DUPE KEY: " + key);
            }
        }

        private static void PrintDupeReplaced(string fileName, string dupeKey, string newKey)
        {
            Console.WriteLine("--- DUPLICATE REPLACED ---");
            Console.WriteLine("FILE: " + fileName);
            Console.WriteLine("DUPLICATE: " + dupeKey);
            Console.WriteLine("NEW: " + newKey);
        }

        private static string SelectBestKey(IEnumerable<string> oldKeys)
        {
            //See if a common name exists
            var newKey = oldKeys.Where(x => x.Contains("Common")).FirstOrDefault();

            //Else use the first instance we found
            if(newKey == null)
            {
                return oldKeys.ElementAt(0);
            }

            return newKey;
        }

        public static Encoding GetEncoding(string filename)
        {
            // Read the BOM
            var bom = new byte[4];
            using (var file = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                file.Read(bom, 0, 4);
            }

            // Analyze the BOM
            if (bom[0] == 0x2b && bom[1] == 0x2f && bom[2] == 0x76) return Encoding.UTF7;
            if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) return Encoding.UTF8;
            if (bom[0] == 0xff && bom[1] == 0xfe && bom[2] == 0 && bom[3] == 0) return Encoding.UTF32; //UTF-32LE
            if (bom[0] == 0xff && bom[1] == 0xfe) return Encoding.Unicode; //UTF-16LE
            if (bom[0] == 0xfe && bom[1] == 0xff) return Encoding.BigEndianUnicode; //UTF-16BE
            if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff) return new UTF32Encoding(true, true);  //UTF-32BE

            // We actually have no idea what the encoding is if we reach this point, so
            // you may wish to return null instead of defaulting to ASCII
            return Encoding.ASCII;
        }
    }
}
