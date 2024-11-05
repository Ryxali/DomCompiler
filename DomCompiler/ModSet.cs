using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace DomCompiler
{
    public class ModSet
    {
        private readonly Dictionary<EntryType, List<Entry>> data = new Dictionary<EntryType, List<Entry>>();
        private readonly HashSet<string> imagePaths = new HashSet<string>();

        private readonly EntryComparer entryComparer = new EntryComparer();

        private readonly Regex entryMatch = new Regex(@"^#\w.+");
        private readonly Regex commandMatch = new Regex(@"#\S+");
        private readonly Regex sansComments = new Regex(@"^#\w.+\S(?=\s*--)");
        private readonly Regex endMatch = new Regex(@"^#end");
        private readonly Regex idMatch = new Regex(@"(?<=#\w\S*\s)\S+");
        private readonly Regex imageMatch = new Regex(@"(?<=#((x?spr\d?)|(icon)|(flag))\s*"")[^""]+(?="")");
        private readonly Regex idReplaceMatch = new Regex(@"(#\w\S*\s)($\d+)(.*)");

        private readonly Regex isNewSelectCopyMatch = new Regex(@"(?<=#((new)|(select)|(copy))\S+\s+)\$\S+");
        private readonly Regex weaponIdMatch = new Regex(@"(?<=#((secondaryeffect\S*)|(danceweapon)|(weapon))\s+)\$\S+");
        private readonly Regex armorIdMatch = new Regex(@"(?<=#armor\s+)\$\S+");
        private readonly Regex monsterIdMatch = new Regex(@"(?<=#((monpresentrec)|(damage)|(ownsmonrec)|(\S*shape\S*)|(twiceborn)|(lich)|(animated)|(\S*sum\S*)|(templetrainer)|(slaver)|(assassin)|(\S*mnr\S*)|(\S*mon\S*)|(\S*com\S*)|(\S*unit\S*)|(\S*rec\S*)|(\S*scout\S*)|(\S*hero\S*)|(\S*god\S*)|(guardspirit))\s+)\$\S+");
        private readonly Regex spellIdMatch = new Regex(@"(?<=#(\S*spell\S*)\s+)\$\S+");
        private readonly Regex nationIdMatch = new Regex(@"(?<=#((nat)|(restricted)|(\S*nation\S*)|(newtemplate))\s+)\$\S+");
        private readonly Regex codeMatch = new Regex(@"(?<=#\S*code\S*\s+)\$\S+");

        private readonly StringBuilder parserBuffer = new StringBuilder();


        // #newmonster $3
        // #selectmonster $3
        // #copyspr $2
        // #danceweapon $1214
        // #weapon $1214
        private readonly StartIndices startIndices;


        public ModSet(StartIndices startIndices)
        {
            foreach (EntryType type in Enum.GetValues(typeof(EntryType)))
            {
                data.Add(type, new List<Entry>());
            }
            this.startIndices = startIndices;
        }

        public List<Entry> Get(EntryType type) => data[type];

        public void Sort()
        {
            data[EntryType.Monster].Sort(entryComparer);
            data[EntryType.Armor].Sort(entryComparer);
            data[EntryType.Weapon].Sort(entryComparer);
            data[EntryType.Nation].Sort(entryComparer);
            data[EntryType.Mercenary].Sort(entryComparer);
            data[EntryType.Spell].Sort(entryComparer);
            data[EntryType.Poptype].Sort(entryComparer);
            data[EntryType.Sound].Sort(entryComparer);
            data[EntryType.Ai].Sort(entryComparer);
        }

        public void BindIds()
        {
            foreach (var a in data.Values)
                foreach (var b in a)
                    ResolveIds(b);
        }

        private void ResolveIds(Entry entry)
        {
            for (int i = 0; i < entry.raw.Length; i++)
            {
                var r = entry.raw[i];
                entry.raw[i] = isNewSelectCopyMatch.Replace(entry.raw[i], ev => ParseId(ev.Value, entry.type).ToString());
                entry.raw[i] = weaponIdMatch.Replace(entry.raw[i], ev => ParseId(ev.Value, EntryType.Weapon).ToString());
                entry.raw[i] = armorIdMatch.Replace(entry.raw[i], ev => ParseId(ev.Value, EntryType.Armor).ToString());
                entry.raw[i] = monsterIdMatch.Replace(entry.raw[i], ev => ParseId(ev.Value, EntryType.Monster).ToString());
                entry.raw[i] = spellIdMatch.Replace(entry.raw[i], ev => ParseId(ev.Value, EntryType.Spell).ToString());
                entry.raw[i] = nationIdMatch.Replace(entry.raw[i], ev => ParseId(ev.Value, EntryType.Nation).ToString());
                entry.raw[i] = codeMatch.Replace(entry.raw[i], ev => ParseId(ev.Value, EntryType.Event).ToString());
                // Console.WriteLine($"{r} => {entry.raw[i]}");
            }
        }

        public void Parse(string path)
        {
            using (var reader = new StreamReader(File.OpenRead(path)))
            {
                var sb = new StringBuilder();
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (entryMatch.IsMatch(line))
                    {
                        var entry = ParseEntry(line, reader);
                        Get(entry.type).Add(entry);
                    }
                }
            }
        }

        public IEnumerable<string> GetImagePaths() => imagePaths;

        public void Write(StreamWriter stream)
        {
            foreach (EntryType entryType in Enum.GetValues(typeof(EntryType)))
            {
                if (entryType != EntryType.Meta)
                    stream.WriteLine($"-- {entryType}");

                var entries = Get(entryType);
                foreach (var entry in entries)
                {
                    foreach (var line in entry.raw)
                    {
                        stream.WriteLine(line);
                    }
                    if (entryType != EntryType.General && entryType != EntryType.Meta)
                        stream.WriteLine();
                }
                if (entryType == EntryType.Meta)
                    stream.WriteLine();
            }
        }

        private int ParseId(string id, EntryType type)
        {
            if (id.StartsWith('$'))
            {
                int idV = int.Parse(id.Substring(1));
                return type switch
                {
                    EntryType.Weapon => idV + startIndices.startWeaponIndex,
                    EntryType.Armor => idV + startIndices.startArmorIndex,
                    EntryType.Monster => idV + startIndices.startMonsterIndex,
                    EntryType.Spell => idV + startIndices.startSpellIndex,
                    EntryType.Nation => idV + startIndices.startNationIndex,
                    EntryType.Event => startIndices.startEventCodeIndex - idV,
                    _ => idV
                };
            }
            else
            {
                return int.Parse(id);
            }
        }

        private Entry ParseEntry(string entry, StreamReader reader)
        {
            var raw = new List<string>();
            raw.Add(entry);
            var command = commandMatch.Match(entry).Value;

            const string Entry = "_";
            EntryType type = default;
            int? id = null;
            switch (command)
            {
                case "#icon":
                    {
                        var img = imageMatch.Match(entry);
                        if (img.Success && !imagePaths.Contains(img.Value))
                        {
                            if (!File.Exists(img.Value))
                                Console.WriteLine($"[ERR]: PathNotFound for {entry}");
                            imagePaths.Add(img.Value);
                        }
                    }
                    goto case "#domversion";
                case "#modname":
                case "#description":
                case "#version":
                case "#domversion":
                    type = EntryType.Meta;
                    break;
                case "#selectsound":
                    type = EntryType.Sound;
                    goto case Entry;
                case "#selectweapon":
                case "#newweapon":
                    type = EntryType.Weapon;
                    goto case Entry;
                case "#selectarmor":
                case "#newarmor":
                    type = EntryType.Armor;
                    goto case Entry;
                case "#selectmonster":
                case "#newmonster":
                    type = EntryType.Monster;
                    goto case Entry;
                case "#selectnametype":
                    type = EntryType.Name;
                    goto case Entry;
                case "#selectbless":
                    type = EntryType.Blessing;
                    goto case Entry;
                case "#selectsite":
                case "#newsite":
                    type = EntryType.Site;
                    goto case Entry;
                case "#selectnation":
                case "#newnation":
                    type = EntryType.Nation;
                    goto case Entry;
                case "#selectspell":
                case "#newspell":
                    type = EntryType.Spell;
                    goto case Entry;
                case "#selectitem":
                case "#newitem":
                    type = EntryType.Item;
                    goto case Entry;
                case "#selectpoptype":
                    type = EntryType.Poptype;
                    goto case Entry;
                case "#newmerc":
                    type = EntryType.Mercenary;
                    goto case Entry;
                case "#newevent":
                    type = EntryType.Event;
                    goto case Entry;
                case "#newtemplate":
                    type = EntryType.Ai;
                    goto case Entry;
                case Entry:
                    var idStr = idMatch.Match(entry);
                    if(idStr.Success)
                        id = ParseId(idStr.Value, type);
                    while (!reader.EndOfStream)
                    {
                        parserBuffer.Clear();
                        int quotes = 0;
                        do
                        {
                            var read = reader.ReadLine();
                            var pure = sansComments.Match(read);
                            quotes += Regex.Matches(pure.Success ? pure.Value : read, @"""").Count;
                            if (parserBuffer.Length == 0)
                                parserBuffer.Append(read);
                            else
                                parserBuffer.AppendLine(read);
                        }
                        while (quotes % 2 == 1);

                        var result = parserBuffer.ToString();

                        if (entryMatch.IsMatch(result))
                        {
                            raw.Add(result);
                            var img = imageMatch.Match(result);
                            if (img.Success && !imagePaths.Contains(img.Value))
                            {

                                if (!File.Exists(img.Value))
                                    Console.WriteLine($"[ERR]: PathNotFound for {entry}");
                                imagePaths.Add(img.Value);
                            }
                        }
                        if (endMatch.IsMatch(result))
                            break;
                    }
                    break;
                default:
                    type = EntryType.General;
                    break;
            }

            return new Entry
            {
                type = type,
                raw = raw.ToArray(),
                id = id
            };
        }
    }
}
