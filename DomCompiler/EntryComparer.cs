using System.Collections.Generic;

namespace DomCompiler
{
    public sealed class EntryComparer : IComparer<Entry>
    {
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
    }
}
