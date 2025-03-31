using System.Collections.Generic;

namespace DomCompiler
{
    public sealed class EntryComparer : IComparer<Entry>
    {
        public static readonly EntryComparer constant = new EntryComparer();
        private EntryComparer() { }
        public int Compare(Entry x, Entry y)
        {
            var xId = x.id.GetValueOrDefault();
            var yId = y.id.GetValueOrDefault();
            if (x.raw[0].StartsWith("#new"))
                xId = int.MinValue + xId;
            if (y.raw[0].StartsWith("#new"))
                yId = int.MinValue + yId;

            return  xId.CompareTo(yId);
        }

        public static int IdScore(Entry e)
        {
            if (e.raw[0].StartsWith("#new"))
                return int.MinValue + e.id.GetValueOrDefault();
            else
                return e.id.GetValueOrDefault();
        }
    }
}
