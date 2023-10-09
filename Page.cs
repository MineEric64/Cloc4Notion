using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Cloc4Notion
{
    public class Page
    {
        public string Name { get; set; }
        public string Content { get; set; } = string.Empty;
        public Counts Count { get; set; } = new Counts();

        public List<Page> SubPages { get; set; } = new List<Page>();
        public string Parent { get; set; } = string.Empty;
        public string FullName { get; }

        public Page(string name, string content, string parent = "")
        {
            Name = name;
            Content = content;
            Parent = parent;
            FullName = string.IsNullOrWhiteSpace(Parent) ? Name : $"{Parent}/{Name}";

            using (StringReader reader = new StringReader(content)) {
                while (true)
                {
                    string line = reader.ReadLine();

                    if (line == null) break;
                    if (string.IsNullOrWhiteSpace(line)) Count.Blank++;
                    else
                    {
                        bool picture = line.StartsWith("![");
                        bool link = line.StartsWith("[");
                        bool title = line.StartsWith("# ");

                        if (!picture && !link && !title)
                        {
                            Count.Character += line.Length;
                            Count.Word += line.Split(' ').Length;
                            Count.Line++;
                        }
                        else if (picture)
                        {
                            Count.Picture++;
                        }
                    }
                }

                Count.Page++;
            }
        }
        
        public void Add(Page page)
        {
            SubPages.Add(page);
        }

        public Counts CountAllSubPages()
        {
            Counts count = Count;

            foreach (Page page in SubPages)
            {
                Counts subCount = page.CountAllSubPages();
                count = count.Add(subCount);
            }

            return count;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
