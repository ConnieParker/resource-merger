using resource_merger.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Resources;

namespace resource_merger
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // === Utility wireframe ===
            // --- Phase 1 - Dupe Calc ---
            // 1. Input a resx file
            // 2. Find duplicates (store duplicates in memory for future use)
            // 3. Merge duplicates into single keypair 
            // --- Phase 2 - Source Updates ---
            // 4. Input an array of csproj files
            // 4. Iterate over all files in csproj *apart from resx*
            // 5. Find and replace merged keypairs in string
            // 6. Save back down
            // 7. Done

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

                foreach(var item in resx)
                {
                    string resourceKey = (string)item.Key;
                    string resourceValue = (string)item.Value;
                    //Have we seen this resx value before?
                    //If yes, add this resx-key to this entry.
                    if(duplicates.ContainsKey(resourceValue))
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
            duplicates = (Dictionary<string, List<string>>)duplicates.Where(x => x.Key.Count() > 1);

            // Step 3. Merge duplicates down
            // Rule; always use the *first key*, as it'll be the first time that resource was added to the file.

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
    }
}
