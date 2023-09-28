using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Path = System.IO.Path;

using AlphaFile = Alphaleonis.Win32.Filesystem.File;
using AlphaFileInfo = Alphaleonis.Win32.Filesystem.FileInfo;
using AlphaDirectory = Alphaleonis.Win32.Filesystem.Directory;

namespace Cloc4Notion
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        string TempDirectory => Path.GetTempPath() + "Cloc4Notion";
        Page CurrentPage { get; set; } = null;

        public MainWindow()
        {
            InitializeComponent();
            AppContext.SetSwitch("Switch.System.IO.UseLegacyPathHandling", false);
            AppContext.SetSwitch("Switch.System.IO.BlockLongPaths", false);

            this.Closing += MainWindow_Closing;

            Load(@"D:\ericseyoun\게임\[Notion Archives]\Notion_Game_Plan_Backuped_5.zip"); //for test
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            AlphaDirectory.Delete(TempDirectory, true);
        }

        public void Load(string path)
        {
            CurrentPage = null;

            using (var unzip = new Unzip(path))
            {
                if (AlphaDirectory.Exists(TempDirectory)) AlphaDirectory.Delete(TempDirectory, true);
                unzip.ExtractToDirectory(TempDirectory);

                foreach (var fileName in unzip.FileNames)
                {
                    string validFileName = GetValidPath(fileName);
                    string name = GetNormalizedPageName(Path.GetFileNameWithoutExtension(validFileName), out bool hashExists);

                    if (hashExists) //it's Page
                    {
                        string content = AlphaFile.ReadAllText(Path.Combine(TempDirectory, fileName));
                        if (CurrentPage == null) CurrentPage = new Page(name, content);

                        string parent = GetParent(validFileName);
                        Page currentSubPage = GetSelectedPage(parent);

                        currentSubPage.Add(new Page(name, content));
                    }
                }

                Debug.WriteLine(CurrentPage.CountAllSubPages());
            }
        }

        public string GetNormalizedPageName(string name, out bool hashExists)
        {
            string converted = name;
            hashExists = false;

            if (name.Length >= 33) //hash: last 32 characters & 1 blank character
            {
                string hashWithBlank = name.Substring(name.Length - 33, 33);
                string hash = hashWithBlank.Substring(1, hashWithBlank.Length - 1);

                hashExists = hashWithBlank.StartsWith(" ");
                hashExists = hashExists && Regex.IsMatch(hash, "^[0-9a-f]{32}$", RegexOptions.Compiled);

                if (hashExists) converted = name.Substring(0, name.Length - 33);
            }

            return converted.Normalize();
        }

        public string GetParent(string path)
        {
            int index = path.LastIndexOf('/');

            if (index == -1) return string.Empty; //root
            return path.Substring(0, index);
        }

        public IEnumerable<string> GetNormalizedParents(string parent)
        {
            if (string.IsNullOrWhiteSpace(parent)) return Enumerable.Empty<string>();

            var parents = new List<string>();
            string[] parentsNotNormalized = parent.Split('/');

            foreach (string parentNotNormalized in parentsNotNormalized)
            {
                string name = GetNormalizedPageName(parentNotNormalized, out _);
                parents.Add(name);
            }

            return parents;
        }

        public static string GetValidPath(string path)
        {
            string[] paths = path.Split(new string[] { "/", @"\" }, StringSplitOptions.None);
            string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            List<string> validPaths = new List<string>();

            for (int i = 0; i < paths.Length; i++)
            {
                string invalidPath = paths[i];

                if (i == 0 && invalidPath.EndsWith(":"))
                {
                    validPaths.Add(invalidPath);
                }
                else
                {
                    string validPath = r.Replace(invalidPath, string.Empty);
                    validPaths.Add(validPath);
                }
            }

            return string.Join(@"/", validPaths);
        }

        public Page GetSelectedPage(string parent)
        {
            List<string> parents = new List<string>(GetNormalizedParents(parent));
            Page currentSubPage = CurrentPage;

            foreach (string dir in parents)
            {
                if (currentSubPage == null) break;

                var pages = currentSubPage.SubPages.Where(x => x.Name == dir).ToList();

                if (pages.Count > 0) currentSubPage = pages[0];
                else currentSubPage = null;
            }

            return currentSubPage;
        }
    }
}
