namespace DomCompiler
{
    public struct Entry
    {
        public EntryType type;
        public string filePath;
        public int fileIndex;
        public int? id;
        public string[] raw;
    }
}
