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

namespace Cloc4Notion
{
    /// <summary>
    /// FindContentWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class FindContentWindow : Window
    {
        private List<Page> _foundPages = new List<Page>();
        private MainWindow _mainWindow;

        public FindContentWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
        }

        public void FindContentWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var view = new GridView();
            view.Columns.Add(new GridViewColumn { Header = "Name", DisplayMemberBinding = new Binding("Name"), Width = 300 });
            view.Columns.Add(new GridViewColumn { Header = "Count", DisplayMemberBinding = new Binding("Count"), Width = 100 });

            listView.View = view;
            ChangeTheme(MainWindow.IsLight);
        }

        private void findButton_Click(object sender, RoutedEventArgs e)
        {
            _foundPages.Clear();
            listView.Items.Clear();
            string key = textBox.Text;

            Search(key, MainWindow.CurrentLoadedPage);            

            foreach (Page page in _foundPages)
            {
                ListViewItem item = new ListViewItem();
                item.Tag = page.FullName;
                item.Content = page.Name;
                int count = Regex.Matches(page.PlainContent, Regex.Escape(key)).Count;
                object a = new { Name = page.Name, Count = count, Tag = page.FullName };

                listView.Items.Add(a);
            }
        }

        private void Search(string s, Page page)
        {
            if (page == null)
            {
                MessageBox.Show("Please search after loading Notion Page or Obsidian vault!", this.Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            foreach (Page subPage in page.SubPages)
            {
                if (subPage.PlainContent.Contains(s)) _foundPages.Add(subPage);
                Search(s, subPage);
            }
        }

        private void listView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count <= 0) return;

            //var item = e.AddedItems[0] as ListViewItem;
            var item = e.AddedItems[0];

            if (item != null)
            {
                var item2 = ConvertFromObjectToDictionary(item);
                MainWindow.CurrentPage = _mainWindow.GetSelectedPage((string)item2["Tag"]);

                //MainWindow.CurrentPage = _mainWindow.GetSelectedPage((string)item.Tag);
                _mainWindow.ApplyCurrentPageCountsUI();
                _mainWindow.Focus();
            }
        }

        public static Dictionary<string, object> ConvertFromObjectToDictionary(object arg)
        {
            return arg.GetType().GetProperties().ToDictionary(property => property.Name, property => property.GetValue(arg));
        }

        public void ChangeTheme(bool isLight)
        {
            var b = MainWindow.CurrentBackgroundBrush;
            var f = MainWindow.CurrentForegroundBrush;

            mainGrid.Background = b;

            textBox.Background = b;
            textBox.BorderBrush = f;
            textBox.Foreground = f;

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
    }
}
