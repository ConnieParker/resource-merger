// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

// === Utility wireframe ===
// 1. Input a csproj file
// 2. Load all resx files from that csproj
// 3. Find duplicates
// 4. Merge duplicates into single keypair (store duplicates in memory for next part)
// 5. Iterate over all files in csproj *apart from resx*
// 6. Find and replace merged keypairs in string
// 7. Save back down
// 8. Done