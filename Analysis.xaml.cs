using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using static System.Windows.Forms.Design.AxImporter;

namespace Cloc4Notion
{
    /// <summary>
    /// FindContentWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class Analysis : Window
    {
        private Dictionary<string, int> _dict = new Dictionary<string, int>();
        private MainWindow _mainWindow;
        private bool _isWhole = false;
        private bool _isSub = true;

        public Analysis(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
        }

        public void Analysis_Loaded(object sender, RoutedEventArgs e) {
            var view = new GridView();

            view.Columns.Add(new GridViewColumn { Header = "Word", DisplayMemberBinding = new Binding("Word"), Width = 300 });
            view.Columns.Add(new GridViewColumn { Header = "Count", DisplayMemberBinding = new Binding("Count"), Width = 100 });
            listView.View = view;

            ApplyDict();
            ChangeTheme(MainWindow.IsLight);
        }

        /// <summary>
        /// Option (0: Count Desc, 1: Count Asc, 2: Word Asc, 3: Word Desc)
        /// </summary>
        public void ApplyDict(int option = 0)
        {
            listView.Items.Clear();
            _dict.Clear();

            Page page = _isWhole ? MainWindow.CurrentLoadedPage : MainWindow.CurrentPage;
            Counts count = _isSub ? page?.CountAllSubPages() : page?.Count;

            if (count == null)
            {
                MessageBox.Show("Please search after loading Notion Page or Obsidian vault!", this.Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            var kvs = new List<KeyValuePair<string, int>>();

            foreach (var kv in count.Dict)
            {
                _dict.Add(kv.Key, kv.Value);
                kvs.Add(kv);
            }

            if (option == 0) kvs = kvs.OrderByDescending(x => x.Value).ToList();
            else if (option == 1) kvs = kvs.OrderBy(x => x.Value).ToList();
            else if (option == 2) kvs = kvs.OrderBy(x => x.Key).ToList();
            else if (option == 3) kvs = kvs.OrderByDescending(x => x.Key).ToList();

            foreach (var kv in kvs) listView.Items.Add(new { Word = kv.Key, Count = kv.Value });
        }

        private void findButton_Click(object sender, RoutedEventArgs e)
        {
            string key0 = textBox.Text;
            var kvs = new List<KeyValuePair<string, int>>();

            ApplyDict();

            if (string.IsNullOrEmpty(key0)) return;
            listView.Items.Clear();

            bool hasKey = false;
            foreach (var kv in _dict)
            {
                if (kv.Key.Contains(key0))
                {
                    if (key0 == kv.Key)
                    {
                        hasKey = true;
                        continue;
                    }
                    kvs.Add(kv);
                }
            }
            if (hasKey) kvs.Insert(0, new KeyValuePair<string, int>(key0, _dict[key0]));

            int option = 0;
            if (option == 0) kvs = kvs.OrderByDescending(x => x.Value).ToList();
            else if (option == 1) kvs = kvs.OrderBy(x => x.Value).ToList();
            else if (option == 2) kvs = kvs.OrderBy(x => x.Key).ToList();
            else if (option == 3) kvs = kvs.OrderByDescending(x => x.Key).ToList();

            foreach (var kv in kvs) listView.Items.Add(new { Word = kv.Key, Count = kv.Value });
        }

        private void listView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count <= 0) return;

            var item = e.AddedItems[0] as ListViewItem;

            if (item != null)
            {

            }
        }

        public void ChangeTheme(bool isLight)
        {
            var b = MainWindow.CurrentBackgroundBrush;
            var f = MainWindow.CurrentForegroundBrush;

            mainGrid.Background = b;

            textBox.Background = b;
            textBox.BorderBrush = f;
            textBox.Foreground = f;

            head.Foreground = f;
            sub.Foreground = f;

            if (isLight)
            {
                var b2 = new SolidColorBrush(Color.FromArgb(255, 221, 221, 221));
                var b3 = new SolidColorBrush(Color.FromArgb(255, 112, 112, 112));
                
                findButton.Background = b2;
                findButton.BorderBrush = b3;
                findButton.Foreground = f;
            }
            else
            {
                findButton.Background = b;
                findButton.BorderBrush = f;
                findButton.Foreground = f;
            }

            var listViewForeground = new SolidColorBrush(Color.FromArgb(255, 4, 34, 113));

            listView.Background = b;
            listView.BorderBrush = f;
            listView.Foreground = isLight ? listViewForeground : f;
            foreach (object item0 in listView.Items)
            {
                if (item0 is ListViewItem item)
                {
                    item.Foreground = isLight ? listViewForeground : f;
                }
            }
        }

        private void sub_Checked(object sender, RoutedEventArgs e)
        {
            _isSub = true;
        }

        private void sub_Unchecked(object sender, RoutedEventArgs e)
        {
            _isSub = false;
        }

        private void head_Checked(object sender, RoutedEventArgs e)
        {
            _isWhole = true;
        }

        private void head_Unchecked(object sender, RoutedEventArgs e)
        {
            _isWhole = false;
        }
    }
}
