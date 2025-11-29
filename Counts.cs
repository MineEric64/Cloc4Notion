using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cloc4Notion
{
    public class Counts
    {
        public int Line { get; set; } = 0;
        public int Word { get; set; } = 0;
        public int Character { get; set; } = 0;
        public int Blank { get; set; } = 0;

        public int Page { get; set; } = 0;
        public int Picture { get; set; } = 0;

        public Dictionary<string, int> Dict { get; set; } = new Dictionary<string, int>();

        public Counts Add(Counts count)
        {
            var count2 = new Counts();

            count2.Line = Line + count.Line;
            count2.Word = Word + count.Word;
            count2.Character = Character + count.Character;
            count2.Blank = Blank + count.Blank;

            count2.Page = Page + count.Page;
            count2.Picture = Picture + count.Picture;

            count2.Dict = new Dictionary<string, int>(Dict);
            foreach (string key in count.Dict.Keys)
            {
                if (count2.Dict.ContainsKey(key)) count2.Dict[key] += count.Dict[key];
                else count2.Dict.Add(key, count.Dict[key]);
            }

            return count2;
        }

        public override string ToString()
        {
            return $"Line: {Line}\n" +
                $"Word: {Word}\n" +
                $"Character: {Character}\n" +
                $"Blank: {Blank}\n" +
                $"Page: {Page}\n" +
                $"Picture: {Picture}\n" +
                $"Dict Count: {Dict.Count}";
        }
    }
}
