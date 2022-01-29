using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using LeagueToolkit.Helpers.Cryptography;
using System.Text;
using LeagueToolkit.IO.WadFile;
using LeagueToolkit.Helpers;

namespace LoLWideScreenFix
{
    /// <summary>
    /// Provides static methods for creating the mod.
    /// </summary>
    public static class ModCreater
    {
        #region Constants/Readonly
        /// <summary>
        /// Information about the fashion for "LoLCustomSkin" and "Fantome".
        /// </summary>
        public static readonly ModInfo Info = new ModInfo()
        {
            Author = "Rerago",
            Description = "Centers the UI in the middle for better (ultra)wide screen support.",
            Name = "WideScreenFix",
            Version = new Version(1, 0).ToString()
        };

        /// <summary>
        /// "LeagueClient.exe" name
        /// </summary>
        private const string LeagueClientExe = "LeagueClient.exe";

        /// <summary>
        /// "League of Legends.exe" name
        /// </summary>
        private const string LoLExe = "League of Legends.exe";

        /// <summary>
        /// "Global.wad.client" name
        /// </summary>
        private const string GlobalWad = "Global.wad.client";

        /// <summary>
        /// Folder structure up to the WAD files within the league folder
        /// </summary>
        private static readonly string[] WadFolders = new string[] { "Game", "DATA", "FINAL" };

        /// <summary>
        /// List of files that need to be processed for the fix
        /// </summary>
        private static readonly List<string> FilesToFixPathes = new List<string>()
        {
            @"ux/renderui/default/basedata.bin",
            @"ux/renderui/spectatorupdate/basedata.bin",
            @"ux/renderui/tft/basedata.bin"
        };

        /// <summary>
        /// A dictionary of the hash of the files that will be modified and the path within the WAD file.
        /// </summary>
        private static readonly Dictionary<ulong, string> FilesToFix = FilesToFixPathes.ToDictionary(x => GetXXHash64Hash(x), y => y);

        /// <summary>
        /// The search string used to search for the wad files.
        /// </summary>
        private const string WadSearchPattern = "*.wad.client";

        /// <summary>
        /// <see cref="char"/> which is used in the wad in the folder to separate.
        /// </summary>
        private const char WadDirectorySeparatorChar = '/';
        #endregion

        #region RAW-Folder
        /// <summary>
        /// Creates a raw folder with the customized BIN files. Which serve as the basis for the mod.
        /// </summary>
        /// <param name="leaguePath">League of Legends path used as a starting point to determine the WAD path.</param>
        /// <param name="modOutputPath">Directory in which the mod should be created.</param>
        /// <param name="targetResolutionWidth">Width of the resolution to be achieved.</param>
        public static void CreateRawModFolder(string leaguePath, string modOutputPath, uint targetResolutionWidth)
        {
            #region Determine paths
            // Determine WAD path
            var leagueWadPath = GetLeagueWadPath(leaguePath);

            // Make sure that the OutPut path exists
            EnsureDirectoryExists(modOutputPath);

            // Delete old content
            CleanDirectory(modOutputPath);
            #endregion

            // find all "wads" files
            foreach (var wadFilePath in Directory.EnumerateFiles(leagueWadPath, WadSearchPattern, SearchOption.AllDirectories))
            {
                // WAD Mounting
                using var wadMound = Wad.Mount(wadFilePath, true);

                // Determine all files in the WAD that are to be processed
                var wadEntsToFix = wadMound?.Entries?.Where(x => FilesToFix.ContainsKey(x.Key)).Select(x => x.Value);

                // Is there no data available? => Skip entry
                if (wadEntsToFix?.Count() <= 0)
                    continue;

                // Determine the path of the WAD in the League folder
                var pathToWad = wadFilePath[leagueWadPath.Length..].TrimStart(Path.DirectorySeparatorChar);

                // Run through all entries
                foreach (var wadEnt in wadEntsToFix)
                {
                    // Determine complete path within the WAD
                    if (!FilesToFix.TryGetValue(wadEnt.XXHash, out string binFilePath))
                        continue;

                    // Local file name
                    var localBinPath = Path.Combine(modOutputPath, pathToWad, binFilePath.Replace(WadDirectorySeparatorChar, Path.DirectorySeparatorChar));

                    // Make sure that the path structure is present
                    EnsureDirectoryExists(localBinPath, true);

                    // Determine steam of the file from the WAD
                    using var entryDecompressedStream = wadEnt.GetDataHandle().GetDecompressedStream();

                    // Modify stream
                    var binTree = LoLWideScreenFix.GetModdedBinTree(entryDecompressedStream, targetResolutionWidth);

                    // Write file
                    binTree.Write(localBinPath, FileVersionProvider.GetSupportedVersions(LeagueFileType.PropertyBin)?.Last());
                }
            }
        }
        #endregion

        #region LoLCustomSkin-Tools
        /// <summary>
        /// Creates a mod for "LoLCustomSkin-Tools".
        /// </summary>
        /// <param name="leaguePath">League of Legends path used as a starting point to determine the WAD path.</param>
        /// <param name="modOutputPath">Directory in which the mod should be created.</param>
        /// <param name="targetResolutionWidth">Width of the resolution to be achieved.</param>
        /// <remarks>https://github.com/LoL-Fantome/lolcustomskin-tools</remarks>
        public static void CreatLoLCustomSkinMod(string leaguePath, string modOutputPath, uint targetResolutionWidth)
        {
            #region Determine paths
            // Determine WAD path
            var leagueWadPath = GetLeagueWadPath(leaguePath);

            // Make sure that the OutPut path exists
            EnsureDirectoryExists(modOutputPath);

            // Required folders
            var modBaseFolder = Path.Combine(modOutputPath, $"{Info.Name} ({targetResolutionWidth}p) - {Info.Version} (by {Info.Author})");
            var modWadFolder = Path.Combine(modBaseFolder, "WAD");
            var modMetaFolder = Path.Combine(modBaseFolder, "META");

            // Delete old content
            CleanDirectory(modBaseFolder);

            // Make sure that all subfolders exist
            EnsureDirectoryExists(modWadFolder);
            EnsureDirectoryExists(modMetaFolder);
            #endregion

            #region Set mod info
            // Add the desired resolution to the name
            var modInfo = Info;
            modInfo.Name += $" ({targetResolutionWidth}p)";

            // Create Info.json
            File.WriteAllText(Path.Combine(modMetaFolder, "info.json"), JsonSerializer.Serialize(modInfo, new JsonSerializerOptions() { WriteIndented = true }));
            #endregion

            // find all "wads" files
            foreach (var wadFilePath in Directory.EnumerateFiles(leagueWadPath, WadSearchPattern, SearchOption.AllDirectories))
            {
                // WAD Mounting
                using var wadMound = Wad.Mount(wadFilePath, true);

                // Determine all files in the WAD that are to be processed
                var wadEntsToFix = wadMound?.Entries?.Where(x => FilesToFix.ContainsKey(x.Key)).Select(x => x.Value);

                // Is there no data available? => Skip entry
                if (wadEntsToFix?.Count() <= 0)
                    continue;

                // Create path to WAD
                var modWadFilePath = Path.Combine(modWadFolder, wadFilePath[leagueWadPath.Length..].TrimStart(Path.DirectorySeparatorChar));

                // WAD-Builder 
                using var wadBuilder = new WadBuilder();

                // Run through all entries
                foreach (var wadEnt in wadEntsToFix)
                {
                    // Determine complete path within the WAD
                    if (!FilesToFix.TryGetValue(wadEnt.XXHash, out string binFilePath))
                        continue;

                    // Determine steam of the file from the WAD
                    using var entryDecompressedStream = wadEnt.GetDataHandle().GetDecompressedStream();

                    // Modify stream
                    var binTree = LoLWideScreenFix.GetModdedBinTree(entryDecompressedStream, targetResolutionWidth);

                    // Create stream for modified file (stream will be closed by WadBuilder during Dispose)
                    var moddedBinTree = new MemoryStream();

                    // Write file
                    binTree.Write(moddedBinTree, FileVersionProvider.GetSupportedVersions(LeagueFileType.PropertyBin)?.Last(), true);

                    // Reset stream position
                    moddedBinTree.Seek(0, SeekOrigin.Begin);

                    // Wad-Entry-Builder
                    var entryBuilder = new WadEntryBuilder(WadEntryChecksumType.XXHash3);
                    entryBuilder.WithPathXXHash(wadEnt.XXHash).WithGenericDataStream(binFilePath, moddedBinTree);

                    // Add entry to WAD
                    wadBuilder.WithEntry(entryBuilder);
                }

                // Create wad file
                wadBuilder.Build(modWadFilePath);
            }
        }
        #endregion

        #region Helper
        /// <summary>
        /// Determines with the passed path of the "WAD" folder within the League of Legends path.
        /// </summary>
        /// <param name="leaguePath">League of Legends path used as a starting point to determine the WAD path.</param>
        /// <returns>The path to the League of Legends "WAD" folder.</returns>
        private static string GetLeagueWadPath(string leaguePath)
        {
            // Return variable
            var returnVal = leaguePath;

            // Check if the path exists
            if (string.IsNullOrEmpty(leaguePath))
                throw new ArgumentNullException(nameof(leaguePath));

            // Check if the path exists
            if (!Directory.Exists(leaguePath))
                throw new DirectoryNotFoundException();

            // Depending on which path was specified adjust return
            if (File.Exists(Path.Combine(returnVal, LeagueClientExe)))
                returnVal = Path.Combine(returnVal, string.Join(Path.DirectorySeparatorChar, WadFolders));
            else if (LastFolderCompare(returnVal, WadFolders[0]) && File.Exists(Path.Combine(returnVal, LoLExe)))
                returnVal = Path.Combine(returnVal, string.Join(Path.DirectorySeparatorChar, WadFolders.Skip(1)));
            else if (LastFolderCompare(returnVal, WadFolders[1]))
                returnVal = Path.Combine(returnVal, WadFolders.Last());

            // Check if it is really the "Wad" folder
            if (!File.Exists(Path.Combine(returnVal, GlobalWad)))
                throw new FileNotFoundException($"{GlobalWad} not found");

            // Check if the path exists
            if (!Directory.Exists(returnVal))
                throw new DirectoryNotFoundException();

            // Return
            return returnVal;
        }

        /// <summary>
        /// Compare whether the last folder corresponds to the name to be compared with. 
        /// </summary>
        /// <param name="path">Path where the last folder should be checked.</param>
        /// <param name="folderName">Folder name on which to check.</param>
        /// <returns>TRUE if equal, FALLS otherwise.</returns>
        private static bool LastFolderCompare(string path, string folderName)
            => string.Compare(Path.GetFileName(Path.GetDirectoryName(path)), folderName, true) == 0;

        /// <summary>
        /// Make sure that the specified folder exists
        /// </summary>
        /// <param name="dirPath">The path for which you want to ensure that it exists.</param>
        /// <param name="isFile">Indicates whether the path is a file.</param>
        public static void EnsureDirectoryExists(string dirPath, bool isFile = false)
        {
            // Path determined which should be checked
            var fileDirectory = isFile ? Path.GetDirectoryName(dirPath) : dirPath;

            // If the path does not exist create
            if (!Directory.Exists(fileDirectory))
                Directory.CreateDirectory(fileDirectory);
        }

        /// <summary>
        /// Deletes the complete contents of the specified path.
        /// </summary>
        /// <param name="path">Path whose content is to be deleted</param>
        public static void CleanDirectory(string path)
        {
            // Get folder information
            var dirInfo = new DirectoryInfo(path);

            // Check if the path exists
            if (dirInfo.Exists)
            {
                // Delete files and folders
                foreach (var file in dirInfo.GetFiles()) file.Delete();
                foreach (var subDirectory in dirInfo.GetDirectories()) subDirectory.Delete(true);
            }
        }

        /// <summary>
        /// Generates an XXHash64 hash for the specified path.
        /// </summary>
        /// <param name="path">Path for which the hash is to be created.</param>
        /// <returns>XXHash64 hash for the specified path.</returns>
        public static ulong GetXXHash64Hash(string path)
            => XXHash.XXH64(Encoding.UTF8.GetBytes(path.ToLower()));
        #endregion
    }

    /// <summary>
    /// Mod information used e.g. by "LoLCustomSkin tools".
    /// </summary>
    public class ModInfo
    {
        [JsonPropertyName("Author")]
        public string Author { get; set; }

        [JsonPropertyName("Description")]
        public string Description { get; set; }

        [JsonPropertyName("Name")]
        public string Name { get; set; }

        [JsonPropertyName("Version")]
        public string Version { get; set; }
    }
}
