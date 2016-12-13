using Dropbox.Api;
using Dropbox.Api.Files;
using Dropbox.Api.Sharing;
using MGL.Data.DataUtilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataNirvana.Dropbox {

    /// <summary>
    ///     See this web page for more details on this .NET wrapper for Dropbox
    ///     https://www.dropbox.com/developers/documentation/dotnet#tutorial
    ///     
    /// </summary>
    public class DropboxWrapper {

        //-------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     GHSPAdmin ...
        /// </summary>
        static string DropboxAccessToken = "ooPOeAJssoAAAAAAAAAAGzoHG6s6zuMZZU5WhmyLjR0TAjmPO890j-GSyvTlRPTU";

        //-------------------------------------------------------------------------------------------------------------------------------
        static DropboxClient dbx = null;

        //-------------------------------------------------------------------------------------------------------------------------------
        public static void Start() {
            dbx = new DropboxClient(DropboxAccessToken);
            /// umm need some error handling hin here!!!
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        public static void Disconnect() {
            dbx.Dispose();
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        public static void TestRun() {
            var task = Task.Run((Func<Task>)DropboxWrapper.Run);
            task.Wait();
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        static async Task Run() {
            //using (var dbx = new DropboxClient(DropboxAccessToken)) {
                var full = await dbx.Users.GetCurrentAccountAsync();
                Console.WriteLine("{0} - {1}", full.Name.DisplayName, full.Email);
            //}
        }

        //Check if a folder exists ...


        // Get all the files in a folder ...
        //-------------------------------------------------------------------------------------------------------------------------------
        public static async Task ListRootFolder() {
            var list = await dbx.Files.ListFolderAsync(string.Empty);

            // show folders then files
            foreach (var item in list.Entries.Where(i => i.IsFolder)) {
                Console.WriteLine("D  {0}/", item.Name);
            }

            foreach (var item in list.Entries.Where(i => i.IsFile)) {
                Console.WriteLine("F{0,8} {1}", item.AsFile.Size, item.Name);
            }
        }


        //-------------------------------------------------------------------------------------------------------------------------------
        //public static void ListFolder(string folder) {
        //    var task = Task.Run(() => DropboxWrapper.ListFolderAsync(folder));
        //    task.Wait();
        //}
        //-------------------------------------------------------------------------------------------------------------------------------
        public static async Task<List<Metadata>> ListFolder(string folder) {
            var list = await dbx.Files.ListFolderAsync(folder);

            // Dropbox.Api.Files.Metadata ...
            List<Metadata> allMetadata = new List<Metadata>();           

            // show folders then files
            foreach (var item in list.Entries.Where(i => i.IsFolder)) {
                allMetadata.Add(item);
                Console.WriteLine("D  {0}/", item.Name);
            }

            foreach (var item in list.Entries.Where(i => i.IsFile)) {
                allMetadata.Add(item);
                Console.WriteLine("F{0,8} {1}", item.AsFile.Size, item.Name);
            }

            return allMetadata;
        }

        ////-------------------------------------------------------------------------------------------------------------------------------
        //public static List<string> ListFolderRecursive(string folder) {
        //    var task = Task.Run(() => DropboxWrapper.ListFolderRecursive(folder));

        //    List<string> all = task.Result;

        //    return all;
        //}
        //-------------------------------------------------------------------------------------------------------------------------------
        public static async Task<List<Metadata>> ListFolderRecursive(string folder) {
            List<Metadata> allMetadata = new List<Metadata>();

            try {

                ListFolderResult list = await dbx.Files.ListFolderAsync(folder, true);

                // Check we have something so that the folder exists ... Dropbox.Api.Files.Metadata ...
                if (list != null && list.Entries != null && list.Entries.Count > 0) {

                    bool finished = false;

                    while (finished == false) {

                        // show folders then files
                        foreach (var item in list.Entries.Where(i => i.IsFolder)) {
                            Console.WriteLine("D  {0}/", item.Name);
                            allMetadata.Add(item);
                        }

                        foreach (var item in list.Entries.Where(i => i.IsFile)) {
                            Console.WriteLine("F{0,8} {1}", item.AsFile.Size, item.Name);
                            allMetadata.Add(item);
                        }

                        if (list.HasMore == true) {
                            list = await dbx.Files.ListFolderContinueAsync(list.Cursor);
                        } else {
                            finished = true;
                        }
                    }
                }
            } catch(Exception ex) {
                Logger.LogError(4, "DropboxWrapper.ListFolderRecursive crashed - probably because folder name "+folder+" does not exist.  The specific exception was: " + ex.ToString());
            }

            return allMetadata;
        }



        //-------------------------------------------------------------------------------------------------------------------------------
        public static async Task Download(string targetFolder, string dbxSourceFolder, string fileName) {

            string lastSeparator = (dbxSourceFolder.EndsWith("/")) ? "" : "/"; 

            using (var response = await dbx.Files.DownloadAsync(dbxSourceFolder + lastSeparator + fileName)) {
                //Console.WriteLine(await response.GetContentAsStringAsync());
                //File.Delete(targetFolder + fileName);
                string content = await response.GetContentAsStringAsync();
                File.WriteAllText(targetFolder + fileName, content);
            }
        }


        //-------------------------------------------------------------------------------------------------------------------------------
        public static async Task Upload(string folder, string file, string content) {
            using (var mem = new MemoryStream(Encoding.UTF8.GetBytes(content))) {
                var updated = await dbx.Files.UploadAsync(
                    folder + "/" + file,
                    WriteMode.Overwrite.Instance,
                    body: mem);
                Console.WriteLine("Saved {0}/{1} rev {2}", folder, file, updated.Rev);
            }
        }


        //-------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets the URL to the shared link created ...
        ///     Dropbox.Api.Sharing.FileAction.ShareLink
        ///     http://dropbox.github.io/dropbox-sdk-dotnet/html/T_Dropbox_Api_Sharing_FileAction_ShareLink.htm
        ///     http://stackoverflow.com/questions/32099136/is-there-a-way-to-check-if-a-file-folder-in-dropbox-has-a-shared-link-without-cr
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        public static async Task<string> ShareFile(string folder, string file) {

            //RequestedVisibility rv = new 
            //SharedLinkSettings sls = new SharedLinkSettings();
            //sls.R.LinkPassword = "";

            //RequestedVisibility.Public, "TEST");

            // Check to see if it exists
            ListSharedLinksResult lslr = await dbx.Sharing.ListSharedLinksAsync(folder + "/" + file);
            SharedLinkMetadata slmd = null;

            if ( lslr.Links != null && lslr.Links.Count > 0) {
                slmd = lslr.Links[0];
            } else {
                // Create it
                slmd = await dbx.Sharing.CreateSharedLinkWithSettingsAsync(folder + "/" + file);
            }

            //SharedLinkMetadata slmd = await dbx.Sharing.CreateSharedLinkWithSettingsAsync(folder + "/" + file);

            Console.WriteLine("Shared link to " + slmd.Name + " is " + slmd.Url);

            return slmd.Url;
        }


    }
}
