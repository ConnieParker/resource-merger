using resource_merger.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources;

namespace resource_merger
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // === Utility wireframe ===
            // --- Phase 1 - Resx ---
            // 1. Input a resx file
            // 2. Find duplicates (store duplicates in memory for future use)
            // 3. Merge duplicates into single keypair and write to resx
            // --- Phase 2 - Source Updates ---
            // 4. Input a source file directory
            // 5. Iterate over all files
            // 6. Perform duplicate substitutions
            // 7. Save back down

            // --- Phase 1 - Resx ---
            // Step 1. Input resx file.
            Console.WriteLine("Please specify a resx file.");
            var resxPath = Console.ReadLine();

            // Step 2. Find duplicates
            //Caution in naming here; values become keys, keys become values.
            Dictionary<string, List<string>> duplicates = new Dictionary<string, List<string>>();
            List<DictionaryEntry> resx;
            using (var reader = new ResXResourceReader(resxPath))
            {
                var toRemove = new List<DictionaryEntry>();
                resx = reader.Cast<DictionaryEntry>().ToList();

                foreach (var item in resx)
                {
                    string resourceKey = (string)item.Key;
                    string resourceValue = (string)item.Value;
                    //Have we seen this resx value before?
                    //If yes, add this resx-key to this entry.
                    if (duplicates.ContainsKey(resourceValue))
                    {
                        duplicates[resourceValue].Add(resourceKey);
                        PrintDuplicateDetected(duplicates[resourceValue], resourceValue);
                        toRemove.Add(item);

                    }
                    //If no, add an entry to the duplicates dictionary.
                    else
                    {
                        duplicates[resourceValue] = new List<string>() { resourceKey };
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
                resx.ForEach(r =>
                {
                    // Again Adding all resource to generate with final items
                    writer.AddResource(r.Key.ToString(), r.Value.ToString());
                });
                writer.Generate();
            }

            // --- Phase 2 - Source Updates ---
            // Step 4. Input a source directory
            Console.WriteLine("Please specify a source directory to update.");
            var sourceDir = Console.ReadLine();
            var allowedExtensions = new[] { ".cs", ".cshtml" };
            var files = Directory.GetFileSystemEntries(sourceDir, "*", SearchOption.AllDirectories)
                .Where(file => allowedExtensions.Any(file.ToLower().EndsWith))
                .ToList();

            // Step 5. Iterate over all files
            foreach (var filePath in files)
            {
                bool dirty = false;

                //Read out contents
                string fileContents = File.ReadAllText(filePath);

                // Step 6. Perform duplicate substitutions
                //fileContents = fileContents.Replace("some text", "some other text");
                foreach (var dupe in duplicates)
                {
                    var newKey = dupe.Value[0];
                    foreach (var dupeKey in dupe.Value.Skip(1))
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

                // Step 7. Save back down
                if (dirty)
                {
                    File.WriteAllText(filePath, fileContents);
                }
            }

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Duplicate merging successfully completed.");
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey(true);
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
    }
}
