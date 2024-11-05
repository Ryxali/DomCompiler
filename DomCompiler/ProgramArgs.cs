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
                    throw new ArgumentException("Need at least an output file path");
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
                        throw new DirectoryNotFoundException($"Invalid path [1]: '{work}'");
                    }
                    goto case 1;
            }
        }

        public static ProgramArgs Parse(string[] args) => new ProgramArgs(args);
    }
}
