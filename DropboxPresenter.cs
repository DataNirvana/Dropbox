using Dropbox.Api;
using Dropbox.Api.Files;
using MGL.Data.DataUtilities;
using MGL.Web.WebUtilities;
using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Xml.Linq;

//----------------------------------------------------------------------------------------------------------------------------------
namespace DataNirvana.Dropbox {

    //-------------------------------------------------------------------------------------------------------------------------------
    public class DropboxPresenter {

        //----------------------------------------------------------------------------------------------------------------------------
        public static async Task<StringBuilder> Test() {
            StringBuilder sb = new StringBuilder();

            Console.WriteLine("----- Testing list folder");
            List<Metadata> mdList = await DropboxWrapper.ListFolder("/GHSP Live Documents/LEGS");

            Console.WriteLine("----- Testing list folder recursively");
            mdList = await DropboxWrapper.ListFolderRecursive("/GHSP Live Documents/LEGS");

            foreach (Metadata item in mdList) {
                sb.Append("<div>" + item.PathDisplay + "  " + item.IsFolder + "</div>");
            }

            return sb;
        }

        /* This is the kind of output dbx.ListFolderRecursive Metadata objects will provide
            /GHSP Live Documents/LEGS True
            /GHSP Live Documents/LEGS/Resources True
            /GHSP Live Documents/LEGS/Standards True
            /GHSP Live Documents/LEGS/Standards/EN True
            /GHSP Live Documents/legs/standards/EN/LEGS_Handbook.xml False
            /GHSP Live Documents/legs/standards/EN/Figure 2.1.ai False
            /GHSP Live Documents/legs/standards/EN/Figure 3.1.ai False
            /GHSP Live Documents/legs/standards/EN/Figure 3_Introduction.ai False
            /GHSP Live Documents/legs/standards/EN/Figure 4.1.ai False
            /GHSP Live Documents/legs/standards/EN/Figure 4_Introduction.ai False
            /GHSP Live Documents/legs/standards/EN/Figure 5.1.ai False
            /GHSP Live Documents/legs/standards/EN/Figure 5_Introduction.ai False
        */

        //----------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     File extensions should be without the dot and in lower case e.g. "xml"
        /// </summary>
        /// <param name="filesExtensionsToInclude"></param>
        /// <returns></returns>
        public static async Task<TreeNode> GenerateTreeView(List<string> filesExtensionsToInclude) {

            List<Metadata> mdList = await DropboxWrapper.ListFolderRecursive("/GHSP Live Documents");

            //TreeNode tn = BuildNodeList(mdList, fileExtensionsToInclude);

            //List<string> filesAndFolders = new List<string>();
            //foreach (Metadata item in mdList) {
                //filesAndFolders.Add(item.PathDisplay);
            //}

            // OK now go through and chop out those that don't end with one of our file extensions
            for(int i = 0; i < mdList.Count; i++) {
                if (mdList[i].IsFile == true) {
                    bool isValidSuffix = false;

                    foreach (string fileExt in filesExtensionsToInclude) {
                        if (mdList[i].PathLower.EndsWith("." + fileExt.ToLower()) == true) {
                            isValidSuffix = true;
                            break;
                        }
                    }

                    if ( isValidSuffix == false) {
                        mdList.RemoveAt(i);
                        i--;
                    }
                }                    
            }

            //TreeView tv = new TreeView();


            //PopulateTreeView(tv, filesAndFolders, '/');

            TreeNode tn = CreateDirectoryNode(mdList, mdList[0]);

            //tv.SkipLinkText = "";

            //TreeView tv = new TreeView();
            //tv.ID = "TreeView";
            //tv.SkipLinkText = String.Empty;
            //tv.CollapseImageUrl = "/Images/View.png";
            //tv.CollapseImageToolTip = "/Images/Add.png";
            //tv.ExpandImageToolTip = "/Images/Search.png";
            //tv.ExpandImageUrl = "/Images/Help.png";
            //tv.NoExpandImageUrl = "/Images/Ind.png";


            //tv.ShowLines = false;
            //tv.LineImagesFolder = "";

            //tv.ShowCheckBoxes = TreeNodeTypes.None;

            //tv.Nodes.Add(tn);

            return tn;

            //return BuildHTMLList(tn);
        }


        //----------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Note for this to work we need to add a key to the web.config as described here:
        ///  http://stackoverflow.com/questions/25132057/error-when-i-try-to-launch-website-in-iis-value-cannot-be-null-parameter-name
        ///  add key="PageInspector:ServerCodeMappingSupport" value="Disabled" 
        ///  Bit shit as this might also kill some useful validation, but after fiddling for a bit I can't otherwise make it work!!!
        /// </summary>
        /// <param name="tn"></param>
        /// <param name="isFirstLevel"></param>
        /// <returns></returns>
        public static string CreateTreeView(TreeNode tn) {
            HtmlGenericControl ul = new HtmlGenericControl("ul");
            //ul.Attributes.Add("class", "DBXHidden");
            // DBXHidden
            ul.Controls.Add(CustomTreeView(tn));

            //            return ul;
            string html = HTMLUtilities.RenderControlToHtml(ul);
            return html;
        }

        //static int idCounter = 1;

        //----------------------------------------------------------------------------------------------------------------------------
        private static HtmlGenericControl CustomTreeView(TreeNode tn) {
            //StringBuilder sb = new StringBuilder();
            HtmlGenericControl li = new HtmlGenericControl("li");

            try {


                //li.InnerHtml = tn.Text;
                //li.ID = "ctv" + idCounter++;


                // So now we need to identify if this is a file, empty folder or normal folder.
                //if (tn.ChildNodes != null && tn.ChildNodes.Count > 0) {

                //} else if (tn.Text)

                // get the navigate URL and see if this is an empty folder or a file ...

                if ((tn.ChildNodes == null || tn.ChildNodes.Count == 0) && string.IsNullOrEmpty(tn.NavigateUrl)) {
                    // this is an empty folder ...
                    li.Attributes.Add("class", "DBXFolderEmpty");
                    li.Attributes.Add("tooltip", "Contains no relevant files");
                    li.InnerHtml = tn.Text;

                } else {

                    // And generate the HRef and assign the relevant classes ...
                    HtmlAnchor liA = new HtmlAnchor();
                    liA.InnerHtml = tn.Text;

                    if (tn.ChildNodes != null && tn.ChildNodes.Count > 0) {
                        li.Attributes.Add("class", "DBXFolder DBXHidden");

                        liA.HRef = "javascript:DBXToggler();";

                        //li.Attributes.Add("class", "DBXFolder");

                    } else {

                        liA.HRef = "javascript:DBXToggler();";
                        li.Attributes.Add("data", tn.NavigateUrl);

                        li.Attributes.Add("class", "DBXWebDoc");
                    }

                    li.Controls.Add(liA);

                }



                // Empty folder, normal folder or file
                //                if (string.IsNullOrEmpty(tn.Value)) {
                //                    li.InnerHtml = tn.Text;
                //                    li.Attributes.Add("class", "DBXFolderEmpty");
                //                } else {

                //HtmlGenericControl liDiv = new HtmlGenericControl("div");
                //liDiv.Attributes.Add("class", "DBXLink");
                //li.Controls.Add(liDiv);



                // And now add the child lists as well ..
                if (tn.ChildNodes != null && tn.ChildNodes.Count > 0) {

                    HtmlGenericControl subUL = new HtmlGenericControl("ul");

                    li.Controls.Add(subUL);

                    //subUL.ID = "ctv" + idCounter++;

                    foreach (TreeNode tnChild in tn.ChildNodes) {
                        subUL.Controls.Add(CustomTreeView(tnChild));
                        //li.Controls.Add(CustomTreeView(tnChild));
                    }

                    //li.InnerHtml = HTMLUtilities.RenderControlToHtml(subUL);
                }



                //cell2Content.Attributes.Add("class", "TBRowPadding");

                ////cell2Content.InnerHtml = "<b>" + section + "</b> - " + name + ":";
                //HtmlTextArea hta = new HtmlTextArea();
                //hta.ID = "TB_" + langID + "_" + sectionNum;
                //hta.Rows = 3;
                //hta.Attributes.Add("class", "TBArea");

                //cell2Content.Controls.Add(hta);
                //cell2.Controls.Add(cell2Content);


            } catch (Exception ex) {

                Logger.LogError(6, "Problem building the CustomTreeView in DropboxPresenter: " + ex.ToString());

            }

            return li;

            /* Proof of concept!
            sb.Append("<li>" + tn.Text + "");

            if ( tn.ChildNodes != null && tn.ChildNodes.Count > 0) {
                sb.Append("<ul>");

                foreach( TreeNode tnChild in tn.ChildNodes) {
                    sb.Append(CustomTreeView(tnChild));
                }

                sb.Append("</ul>");
            }
            

            sb.Append("</li>");

            return sb;
            */
        }


        //----------------------------------------------------------------------------------------------------------------------------
        //public static TreeNode BuildNodeList(List<Metadata> dbxDocsList, List<string> fileExtensionsToInclude) {

        //    TreeNode tn = new TreeNode();

        //    // iterate through twice - once to load all the directories and then to add all the files to the right directories!
        //    foreach (Metadata item in dbxDocsList) {
        //        if ( item.IsFolder == true) {

        //            // break it down into its constituent parts - if any of the required branches dont exist, then add them....
        //            string[] folderBits = item.PathDisplay.Split(new string[] { "/" }, StringSplitOptions.None);

        //            int level = 0;
        //            foreach( string folderBit in folderBits) {



        //                level++;
        //            }


        //        }
        //    }
        //    foreach (Metadata item in dbxDocsList) {
        //        if (item.IsFile == true) {

        //        }
        //    }


        //    tn.ChildNodes.Add(null);

        //    return tn;
        //}


        //----------------------------------------------------------------------------------------------------------------------------
        //public static string BuildHTMLList(TreeNode tn) {

        //    HtmlGenericControl htmlWrapper = new HtmlGenericControl("div");
        //    htmlWrapper.Attributes.Add("class", "col-md-5");

        //    TreeView tv = new TreeView();
        //    tv.ID = "TreeView";
        //    tv.SkipLinkText = String.Empty;
        //    tv.CollapseImageUrl = "/Images/View.png";
        //    tv.CollapseImageToolTip = "/Images/Add.png";
        //    tv.ExpandImageToolTip = "/Images/Search.png";
        //    tv.ExpandImageUrl = "/Images/Help.png";
        //    tv.NoExpandImageUrl = "/Images/Ind.png";


        //    tv.ShowLines = false;
        //    tv.LineImagesFolder = "";

        //    tv.ShowCheckBoxes = TreeNodeTypes.None;

        //    tv.Nodes.Add(tn);
        //    htmlWrapper.Controls.Add(tv);

        //    return HTMLUtilities.RenderControlToHtml(htmlWrapper);
        //}

        //----------------------------------------------------------------------------------------------------------------------------
        //public static string BuildHTMLList(TreeView tv) {

        //    HtmlGenericControl htmlWrapper = new HtmlGenericControl("div");
        //    htmlWrapper.Attributes.Add("class", "col-md-5");

        //    htmlWrapper.Controls.Add(tv);

        //    return HTMLUtilities.RenderControlToHtml(htmlWrapper);
        //}


        //----------------------------------------------------------------------------------------------------------------------------
        //private static void PopulateTreeView(TreeView treeView, List<string> paths, char pathSeparator) {
        //    TreeNode lastNode = null;
        //    string subPathAgg;
        //    foreach (string path in paths) {
        //        subPathAgg = string.Empty;
        //        foreach (string subPath in path.Split(pathSeparator)) {
        //            subPathAgg += subPath + pathSeparator;

        //            TreeNode matchTN = null;
        //            foreach (TreeNode tn in treeView.Nodes) {
        //                if ( tn.Value.Equals(subPathAgg, StringComparison.CurrentCultureIgnoreCase)) {
        //                    matchTN = tn;
        //                    break;
        //                }
        //                //TreeNode[] nodes = treeView.Nodes.Find(subPathAgg, true);
        //            }                        

        //            //if (nodes.Length == 0) {
        //            if (matchTN == null) { 

        //                if (lastNode == null) {
        //                    treeView.Nodes.Add(new TreeNode(subPathAgg, subPath));
        //                    lastNode = matchTN; // nodes[0];
        //                } else {
        //                    lastNode.ChildNodes.Add(new TreeNode(subPathAgg, subPath));
        //                }
        //            } else {
        //                lastNode = matchTN; // nodes[0];
        //            }
        //        }
        //        lastNode = null; // This is the place code was changed

        //    }
        //}

        //----------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Recursivelovelyyyy
        /// </summary>
        /// <param name="filesAndFolders"></param>
        /// <param name="currentDirectory"></param>
        /// <returns></returns>
        private static TreeNode CreateDirectoryNode(List<Metadata> filesAndFolders, Metadata currentDirectory) {
            TreeNode directoryNode = new TreeNode(currentDirectory.Name);
            //directoryNode.Parent = parentDirectory;

            Regex rFolder = new Regex("^" + currentDirectory.PathLower + "[/][a-zA-Z0-9-_ ]+$");
            Regex rFile = new Regex("^" + currentDirectory.PathLower + "[/][a-zA-Z0-9-_ .]+$");

            foreach (Metadata item in filesAndFolders) {
                if (item.IsFolder && rFolder.IsMatch(item.PathLower)) {
                    directoryNode.ChildNodes.Add(CreateDirectoryNode(filesAndFolders, item));
                } else if (item.IsFile) {
                    //&& rFile.IsMatch(item.PathDisplay)) {
                    if (rFile.IsMatch(item.PathLower)) {
                        directoryNode.ChildNodes.Add(new TreeNode(item.Name));
                        directoryNode.ChildNodes[directoryNode.ChildNodes.Count - 1].NavigateUrl = item.PathDisplay;
                        //directoryNode.ChildNodes.Add(new TreeNode(item.Name));
                    }
                }
            }

            // The JS function to fire ...
            //if ( directoryNode.ChildNodes != null && directoryNode.ChildNodes.Count > 0) {
            //    directoryNode.Value = "DBXToggleFolder";
            //} else {
            //    directoryNode.Value = "";
            //}

            return directoryNode;
        }



        //----------------------------------------------------------------------------------------------------------------------------
        /*        List<TreeNode> BuildTreeAndGetRoots(List<Metadata> actualObjects) {
                    var lookup = new Dictionary<string, TreeNode>();
                    var rootNodes = new List<TreeNode>();

                    foreach (Metadata item in actualObjects) {
                        // add us to lookup
                        TreeNode ourNode;
                        if (lookup.TryGetValue(item.PathLower, out ourNode)) {   // was already found as a parent - register the actual object
                            ourNode..Source = item;
                        } else {
                            ourNode = new TreeNode() { Source = item };
                            lookup.Add(item.ID, ourNode);
                        }

                        // hook into parent
                        if (item.ParentID == item.ID) {   // is a root node
                            rootNodes.Add(ourNode);
                        } else {   // is a child row - so we have a parent
                            TreeNode parentNode;
                            if (!lookup.TryGetValue(item.ParentID, out parentNode)) {   // unknown parent, construct preliminary parent
                                parentNode = new Node();
                                lookup.Add(item.ParentID, parentNode);
                            }
                            parentNode.Children.Add(ourNode);
                        }
                    }

                    return rootNodes;
                }
                */


        //----------------------------------------------------------------------------------------------------------------------------
        public static async Task<string> ListFigures(string path) {
            
            List<Metadata> mdList = await DropboxWrapper.ListFolderRecursive(path);

            HtmlGenericControl ul = new HtmlGenericControl("ul");

            // This is a little nastily coded here ...
            Regex rFigureFile = new Regex("[.]svg[z]?$");
           
            int figuresFound = 0;

            if (mdList != null && mdList.Count > 0) {
                foreach(Metadata item in mdList) {
                    if (item.IsFile && rFigureFile.IsMatch(item.PathLower) == true) {

                        HtmlGenericControl li = new HtmlGenericControl("li");
                        li.InnerHtml = item.Name;
                        li.Attributes.Add("class", "DBXWebFigure");

                        ul.Controls.Add(li);

                        figuresFound++;
                    }
                }                
            }

            string html = "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;No figures available in this document's Dropbox folder.";
            if ( figuresFound > 0 ) {
                html = HTMLUtilities.RenderControlToHtml(ul);
            }
                        
            return html;
        }

    }
}
