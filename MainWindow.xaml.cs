using Microsoft.Win32;

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

using Cloc4Notion.Extensions;

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
        public static string TempDirectory => Path.GetTempPath() + "Cloc4Notion";
        public static Page CurrentLoadedPage { get; set; } = null;
        public static Page CurrentPage { get; set; } = null;

        public static bool IncludedSubPages { get; private set; } = true;

        public MainWindow()
        {
            InitializeComponent();
            AppContext.SetSwitch("Switch.System.IO.UseLegacyPathHandling", false);
            AppContext.SetSwitch("Switch.System.IO.BlockLongPaths", false);

            this.Closing += MainWindow_Closing;
            AppDomain.CurrentDomain.UnhandledException += MainWindow_UnhandledException;
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (AlphaDirectory.Exists(TempDirectory)) AlphaDirectory.Delete(TempDirectory, true);
        }

        private void MainWindow_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = (Exception)e.ExceptionObject;
            MessageBox.Show(ex.ToCleanString(), "Cloc4Notion: Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void Load(string path)
        {
            CurrentPage = null;
            CurrentLoadedPage = null;

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
                        if (CurrentLoadedPage == null) CurrentLoadedPage = new Page(name, content);

                        string parent = GetParent(validFileName);
                        string parentNormalized = string.Join("/", GetNormalizedParents(parent));
                        Page currentSubPage = GetSelectedPage(parentNormalized);

                        currentSubPage.Add(new Page(name, content, parentNormalized));
                    }
                }
            }

            CurrentPage = CurrentLoadedPage;
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

        public Page GetSelectedPage(string path)
        {
            Page currentSubPage = CurrentLoadedPage;

            if (!string.IsNullOrWhiteSpace(path))
            {
                string[] parents = path.Split('/');

                foreach (string dir in parents)
                {
                    if (currentSubPage == null) break;

                    var pages = currentSubPage.SubPages.Where(x => x.Name == dir).ToList();

                    if (pages.Count > 0) currentSubPage = pages[0];
                    else currentSubPage = null;
                }
            }

            return currentSubPage;
        }

        private void open_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.Filter = "Zip file (*.zip)|*.zip";
            ofd.Multiselect = false;

            var dialog = ofd.ShowDialog();

            if (dialog.HasValue && dialog.Value)
            {
                string name = Path.GetFileNameWithoutExtension(ofd.FileName);

                Load(ofd.FileName);
                ApplyCurrentPageCountsUI();
                ApplyTree();

                MessageBox.Show($"'{name}' Sucessfully Loaded!", this.Title, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ApplyCurrentPageCountsUI()
        {
            if (CurrentPage == null) return;
            var counts = IncludedSubPages ? CurrentPage.CountAllSubPages() : CurrentPage.Count;

            this.counts_line.Content = $"Line: {counts.Line:n0}";
            this.counts_word.Content = $"Word: {counts.Word:n0}";
            this.counts_character.Content = $"Character: {counts.Character:n0}";
            this.counts_blank.Content = $"Blank: {counts.Blank:n0}";
            this.counts_page.Content = $"Page: {counts.Page:n0}";
            this.counts_picture.Content = $"Picture: {counts.Picture:n0}";
        }

        private void counts_subpage_Checked(object sender, RoutedEventArgs e)
        {
            IncludedSubPages = true;
            ApplyCurrentPageCountsUI();
        }

        private void counts_subpage_Unchecked(object sender, RoutedEventArgs e)
        {
            IncludedSubPages = false;
            ApplyCurrentPageCountsUI();
        }

        private void ApplyTree()
        {
            Page page = CurrentLoadedPage;

            TreeViewItem item = new TreeViewItem();
            item.Header = page.Name;
            item.Tag = page.FullName;
            item.Expanded += new RoutedEventHandler(item_Expanded);   // 노드 확장시 추가

            tree_dir.Items.Add(item);
            GetTreeSubPages(item);
        }

        private void GetTreeSubPages(TreeViewItem itemParent)
        {
            if (itemParent == null) return;
            if (itemParent.Items.Count != 0) return;

            string path = itemParent.Tag as string;
            Page page = GetSelectedPage(path);

            foreach (Page subPage in page.SubPages)
            {
                TreeViewItem item = new TreeViewItem();
                item.Header = subPage.Name;
                item.Tag = subPage.FullName;
                item.Expanded += new RoutedEventHandler(item_Expanded);

                itemParent.Items.Add(item);
            }
        }

        private void item_Expanded(object sender, RoutedEventArgs e)
        {
            TreeViewItem itemParent = sender as TreeViewItem;

            if (itemParent == null) return;
            if (itemParent.Items.Count == 0) return;

            foreach (TreeViewItem item in itemParent.Items)
            {
                GetTreeSubPages(item);
            }
        }

        private void tree_dir_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeViewItem item = e.NewValue as TreeViewItem;
            string path = item.Tag as string;

            CurrentPage = GetSelectedPage(path);
            ApplyCurrentPageCountsUI();
        }
    }
}
