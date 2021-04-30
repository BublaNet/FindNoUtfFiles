using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FindNoUtfFiles
{
    public partial class Form1 : Form
    {
        private DirectoryInfo root { get; set; }
        private TreeNode selectedNode { get; set; }
        private string editor { get; set; }
        private string[] ignoredFileTypes { get; set; }
        private int NumberOfBadFiles { get; set; }
        private int NumberOfFiles { get; set; }

        public Form1(string root)
        {
            InitializeComponent();
            this.root = new DirectoryInfo(root);
            editor = ConfigurationManager.AppSettings["TextEditor"];
            ignoredFileTypes = ConfigurationManager.AppSettings["IgnoredFileTypes"].ToLower().Split(new char[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }



        private void Form1_Shown(object sender, EventArgs e)
        {
            NumberOfFiles = 0;
            NumberOfBadFiles = 0;
            ScanDir(root, null);
            treeView1.ExpandAll();
            treeView1.SelectedNode = treeView1.Nodes[0];

            if (NumberOfBadFiles > 0)
                this.Text = string.Format("Nalezeno {0} souborů které nejsou v UTF-8", NumberOfBadFiles);
            else
                this.Text = "Všechny soubory jsou v UTF-8";
        }

        private void ScanDir(DirectoryInfo dir, TreeNode parentnode)
        {
            TreeNode node = new TreeNode(dir.Name);
            node.ForeColor = Color.Black;
            if (parentnode == null)
            {
                node.Text = dir.FullName;
                treeView1.Nodes.Add(node);
            }
            else
            {
                parentnode.Nodes.Add(node);
            }

            var subdirs = Directory.GetDirectories(dir.FullName);
            foreach (string subdir in subdirs)
            {
                ScanDir(new DirectoryInfo(subdir), node);
            }

            var files = Directory.GetFiles(dir.FullName);
            foreach (string file in files)
            {
                if (SkipFile(file)) continue;

                TreeNode fnode = new TreeNode(new FileInfo(file).Name);
                Encoding en = GetEncoding(file);
                if (en == Encoding.UTF8)
                {
                    fnode.ForeColor = Color.DarkGray;
                }
                else if (en == Encoding.ASCII)
                {
                    fnode.ForeColor = Color.Orange;
                }
                else
                {
                    fnode.ForeColor = Color.Red;
                    NumberOfBadFiles++;
                }
                fnode.ToolTipText = en.ToString();
                node.Nodes.Add(fnode);

                NumberOfFiles++;
                this.Text = string.Format("Načítám soubory...  ({0})", NumberOfFiles);
                this.Refresh();
            }

            if (node.Nodes.Count == 0) node.Remove();
        }

        /// <summary>
        /// Determines a text file's encoding by analyzing its byte order mark (BOM).
        /// Defaults to ASCII when detection of the text file's endianness fails.
        /// </summary>
        /// <param name="filename">The text file to analyze.</param>
        /// <returns>The detected encoding.</returns>
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

            var bytes = File.ReadAllBytes(filename);
            bool cz = bytes.Any(b => b > 127);
            return cz ? Encoding.Default : Encoding.ASCII;
        }

        private void treeView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            string fileName = selectedNode.FullPath;


            if (fileName == root.FullName)
            {
                Form1_Shown(this, EventArgs.Empty);
            }

            FileAttributes attr = File.GetAttributes(fileName);
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                return;
            else
                Process.Start(editor, fileName);
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            selectedNode = e.Node;
        }


        private bool SkipFile(string filename)
        {
            foreach (string fileType in ignoredFileTypes)
            {
                if (filename.EndsWith("." + fileType)) return true;
            }
            return false;
        }


    }
}
