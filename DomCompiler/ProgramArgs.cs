using System;
using System.IO;

namespace DomCompiler
{
    public struct StartIndices
    {
        public int startWeaponIndex;
        public int startArmorIndex;
        public int startMonsterIndex;
        public int startSpellIndex;
        public int startNationIndex;
        public int startEventCodeIndex;

    }

    public struct ProgramArgs
    {
        public readonly string outputPath;
        public readonly string workingDirectory;
        public readonly StartIndices startIndices;

        private ProgramArgs(string[] args)
        {

            startIndices = new StartIndices
            {
                startWeaponIndex = 1000,
                startArmorIndex = 300,
                startMonsterIndex = 5000,
                startSpellIndex = 1300,
                startNationIndex = 150,
                startEventCodeIndex = -300
            };

            workingDirectory = Directory.GetCurrentDirectory();
            switch (args.Length)
            {
                case 0:
                    throw new ArgumentException("arg0 is required.");
                case 1:
                    outputPath = args[0];
                    if (!Path.IsPathFullyQualified(outputPath))
                    {
                        outputPath = Path.GetFullPath(Path.Join(workingDirectory, outputPath));
                    }
                    break;
                default:
                    
                    var work = args[1];
                    var current = Directory.GetCurrentDirectory();
                    if (!Path.IsPathFullyQualified(work))
                    {
                        work = Path.GetFullPath(Path.Join(current, work));
                    }
                    if (Directory.Exists(work))
                    {
                        workingDirectory = work;
                    }
                    else
                    {
                        throw new DirectoryNotFoundException($"arg1 Invalid path: '{work}'");
                    }
                    goto case 1;
            }

            for (int argI = 2; argI < args.Length; argI++)
            {
                var arg = args[argI];
                int index;
                switch (arg)
                {
                    case "--start-weapon-index":
                    case "--windex":
                        if (argI + 1 < args.Length && int.TryParse(args[argI+1], out index) && 1000 <= index && index < 4000)
                        {
                            startIndices.startWeaponIndex = index;
                        }
                        else
                        {
                            throw new ArgumentException($"{arg} must be followed by integer value between 1000-3999");
                        }
                        break;
                    case "--start-armor-index":
                    case "--aindex":
                        if (argI + 1 < args.Length && int.TryParse(args[argI + 1], out index) && 300 <= index && index < 1000)
                        {
                            startIndices.startArmorIndex = index;
                        }
                        else
                        {
                            throw new ArgumentException($"{arg} must be followed by integer value between 300-999");
                        }
                        break;
                    case "--start-monster-index":
                    case "--mindex":
                        if (argI + 1 < args.Length && int.TryParse(args[argI + 1], out index) && 5000 <= index && index < 9000)
                        {
                            startIndices.startMonsterIndex = index;
                        }
                        else
                        {
                            throw new ArgumentException($"{arg} must be followed by integer value between 5000-8999");
                        }
                        break;
                    case "--start-spell-index":
                    case "--sindex":
                        if (argI + 1 < args.Length && int.TryParse(args[argI + 1], out index) && 1300 <= index && index < 4000)
                        {
                            startIndices.startSpellIndex = index;
                        }
                        else
                        {
                            throw new ArgumentException($"{arg} must be followed by integer value between 1300-3999");
                        }
                        break;
                    case "--start-nation-index":
                    case "--nindex":
                        if (argI + 1 < args.Length && int.TryParse(args[argI + 1], out index) && 150 <= index && index < 500)
                        {
                            startIndices.startNationIndex = index;
                        }
                        else
                        {
                            throw new ArgumentException($"{arg} must be followed by integer value between 150-499");
                        }
                        break;
                    case "--start-eventcode-index":
                    case "--eindex":
                        if (argI + 1 < args.Length && int.TryParse(args[argI + 1], out index) && -300 >= index && index >= -5000)
                        {
                            startIndices.startEventCodeIndex = index;
                        }
                        else
                        {
                            throw new ArgumentException($"{arg} must be followed by integer value between -300--5000");
                        }
                        break;
                }
            }
        }

        public static bool TryParse(string[] args, out ProgramArgs pArgs)
        {
            try
            {
                pArgs = new ProgramArgs(args);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
#if DEBUG
                Console.WriteLine(e.StackTrace);
#endif
                pArgs = default;
                return false;
            }
        }
    }
}
