﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace DomCompiler
{
    public sealed class ModSet
    {
        private readonly Dictionary<EntryType, List<Entry>> data = new Dictionary<EntryType, List<Entry>>();
        private readonly HashSet<string> imagePaths = new HashSet<string>();

        private readonly EntryComparer entryComparer = new EntryComparer();

        private readonly Regex entryMatch = new Regex(@"^#\w.+");
        private readonly Regex commandMatch = new Regex(@"#\S+");
        private readonly Regex sansComments = new Regex(@"^#\w.+\S(?=\s*--)");
        private readonly Regex endMatch = new Regex(@"^#end");
        private readonly Regex idMatch = new Regex(@"(?<=##?\w\S*\s)\$?-?\d+");
        private readonly Regex imageMatch = new Regex(@"(?<=#((x?spr\d?)|(icon)|(flag))\s*"")[^""]+(?="")");
        private readonly Regex idReplaceMatch = new Regex(@"(#\w\S*\s)($\d+)(.*)");

        private readonly Regex isNewSelectCopyMatch = new Regex(@"(?<=#((new)|(select)|(copy))\S+\s+)\$\d+");
        private readonly Regex weaponIdMatch = new Regex(@"(?<=#((secondaryeffect\S*)|(danceweapon)|(weapon))\s+)\$\d+");
        private readonly Regex armorIdMatch = new Regex(@"(?<=#armor\s+)\$\d+");
        private readonly Regex monsterIdMatch = new Regex(@"(?<=#((transform)|(monpresentrec)|(damage)|(ownsmonrec)|(\S*shape\S*)|(twiceborn)|(lich)|(animated)|(\S*sum\S*)|(templetrainer)|(slaver)|(assassin)|(\S*mnr\S*)|(\S*mon\S*)|(\S*com\S*)|(\S*unit\S*)|(\S*rec\S*)|(\S*scout\S*)|(\S*hero\S*)|(\S*god\S*)|(guardspirit))\s+)\$\d+");
        private readonly Regex siteIdMatch = new Regex(@"(?<=#(\S*site\S*)\s+)\$\d+");
        private readonly Regex spellIdMatch = new Regex(@"(?<=#(\S*spell\S*)\s+)\$\d+");
        private readonly Regex nationIdMatch = new Regex(@"(?<=#((nat)|(restricted)|(\S*nation\S*)|(\S*owner\S*)|(newtemplate))\s+)\$\d+");
        private readonly Regex enchantmentNumberMatch = new Regex(@"(?<=#\S*ench\S*\s+)\$\d+");
        private readonly Regex codeMatch = new Regex(@"(?<=#\S*code\S*\s+)\$\d+");

        #region Special Commands
        private static readonly Regex globalEnchantment = new Regex(@"(?<=##globalenchantment\s+)\$?-?\d+");
        private static readonly Regex combatSummon = new Regex(@"(?<=##combatsummon\s+)\$?-?\d+");
        private static readonly Regex ritualSummon = new Regex(@"(?<=##ritualsummon\s+)\$?-?\d+");
        private static readonly Regex ritualCommanderSummon = new Regex(@"(?<=##ritualsummoncom\s+)\$?-?\d+");

        #endregion

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
                entry.raw[i] = enchantmentNumberMatch.Replace(entry.raw[i], ev => ParseId(ev.Value, EntryType.EnchantmentNumber).ToString());
                entry.raw[i] = weaponIdMatch.Replace(entry.raw[i], ev => ParseId(ev.Value, EntryType.Weapon).ToString());
                entry.raw[i] = armorIdMatch.Replace(entry.raw[i], ev => ParseId(ev.Value, EntryType.Armor).ToString());
                entry.raw[i] = monsterIdMatch.Replace(entry.raw[i], ev => ParseId(ev.Value, EntryType.Monster).ToString());
                entry.raw[i] = spellIdMatch.Replace(entry.raw[i], ev => ParseId(ev.Value, EntryType.Spell).ToString());
                entry.raw[i] = nationIdMatch.Replace(entry.raw[i], ev => ParseId(ev.Value, EntryType.Nation).ToString());
                entry.raw[i] = siteIdMatch.Replace(entry.raw[i], ev => ParseId(ev.Value, EntryType.Site).ToString());
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

        public bool ParseSpecial(string line, StringBuilder output)
        {
            var isGlobalEnch = globalEnchantment.Match(line);
            if (isGlobalEnch.Success)
            {
                var id = ParseId(idMatch.Match(line).Value, EntryType.EnchantmentNumber);
                output.AppendLine("#effect 10081");
                output.Append("#damage ");
                output.Append(id.ToString());
                return true;
            }
            var isCombatSumm = combatSummon.Match(line);
            if (isCombatSumm.Success)
            {
                var id = ParseId(idMatch.Match(line).Value, EntryType.Monster);
                output.AppendLine("#effect 1");
                output.Append("#damage ");
                output.Append(id.ToString());
                return true;
            }
            var isRitualSumm = ritualSummon.Match(line);
            if (isRitualSumm.Success)
            {
                var id = ParseId(idMatch.Match(line).Value, EntryType.Monster);
                output.AppendLine("#effect 10001");
                output.Append("#damage ");
                output.Append(id.ToString());
                return true;
            }

            var isRitualSummCom = ritualCommanderSummon.Match(line);
            if (isRitualSummCom.Success)
            {
                var id = ParseId(idMatch.Match(line).Value, EntryType.Monster);
                output.AppendLine("#effect 10021");
                output.Append("#damage ");
                output.Append(id.ToString());
                return true;
            }
            return false;
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
                    EntryType.EnchantmentNumber => idV + startIndices.startEnchNbr,
                    EntryType.Site => idV + startIndices.startSiteIndex,
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
            const string EntryWithId = "__";
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
                case "#newevent":
                    type = EntryType.Event;
                    goto case Entry;
                case "#selectsound":
                    type = EntryType.Sound;
                    goto case EntryWithId;
                case "#selectweapon":
                case "#newweapon":
                    type = EntryType.Weapon;
                    goto case EntryWithId;
                case "#selectarmor":
                case "#newarmor":
                    type = EntryType.Armor;
                    goto case EntryWithId;
                case "#selectmonster":
                case "#newmonster":
                    type = EntryType.Monster;
                    goto case EntryWithId;
                case "#selectnametype":
                    type = EntryType.Name;
                    goto case EntryWithId;
                case "#selectbless":
                    type = EntryType.Blessing;
                    goto case EntryWithId;
                case "#selectsite":
                case "#newsite":
                    type = EntryType.Site;
                    goto case EntryWithId;
                case "#selectnation":
                case "#newnation":
                    type = EntryType.Nation;
                    goto case EntryWithId;
                case "#selectspell":
                case "#newspell":
                    type = EntryType.Spell;
                    goto case EntryWithId;
                case "#selectitem":
                case "#newitem":
                    type = EntryType.Item;
                    goto case EntryWithId;
                case "#selectpoptype":
                    type = EntryType.Poptype;
                    goto case EntryWithId;
                case "#newmerc":
                    type = EntryType.Mercenary;
                    goto case EntryWithId;
                case "#newtemplate":
                    type = EntryType.Ai;
                    goto case EntryWithId;
                case EntryWithId:
                    var idStr = idMatch.Match(entry);
                    if(idStr.Success)
                        id = ParseId(idStr.Value, type);
                    goto case Entry;
                case Entry:
                    while (!reader.EndOfStream)
                    {
                        parserBuffer.Clear();
                        int quotes = 0;
                        do
                        {
                            var read = reader.ReadLine();
                            if (!ParseSpecial(read, parserBuffer))
                            {
                                var pure = sansComments.Match(read);
                                quotes += Regex.Matches(pure.Success ? pure.Value : read, @"""").Count;
                                if (parserBuffer.Length == 0 && quotes % 2 == 0)
                                    parserBuffer.Append(read);
                                else
                                    parserBuffer.AppendLine(read);
                            }
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
