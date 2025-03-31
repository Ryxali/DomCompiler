using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace DomCompiler
{

    class Program
    {
        /// <summary>
        /// Arguments:
        /// <list type="bullet">
        /// <item>[0] path</item>
        /// <item>[1] output</item>
        /// </list>
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            if (args.Length > 0 && string.Equals(args[0], "--help"))
            {
                Console.WriteLine("-- Usage --");
                Console.WriteLine("DomCompiler arg0 [arg1] [--windex | --start-weapon-index] [--aindex | --start-armor-index] [--mindex | --start-monster-index] [--sindex | --start-spell-index] [--nindex | --start-nation-index] [--eindex | --start-eventcode-index] [--enindex | --start-enchnbr-index] [--ecindex | --start-eventcode-index]");
                Console.WriteLine("Parses all .dme files in the working directory and outputs them to a single .dm file, copying all used art assets in the process.");
                Console.WriteLine("arg0: path to the output file, as an absolute path or relative to arg1.");
                Console.WriteLine("arg1 (optional): path to working directory. Will use the default working directory if unspecified.");
                Console.WriteLine("--windex | --start-weapon-index (optional): start index to use for relative ids for weapons (1000-3999). Defaults to minimum value. Should always be a bit less than the maximum allowed value to not escape bounds when generating.");
                Console.WriteLine("--aindex | --start-armor-index (optional): start index to use for relative ids for armors (300-999). Defaults to minimum value. Should always be a bit less than the maximum allowed value to not escape bounds when generating.");
                Console.WriteLine("--mindex | --start-monster-index (optional): start index to use for relative ids for monsters (5000-8999). Defaults to minimum value. Should always be a bit less than the maximum allowed value to not escape bounds when generating.");
                Console.WriteLine("--spindex | --start-spell-index (optional): start index to use for relative ids for spells (1300-3999). Defaults to minimum value. Should always be a bit less than the maximum allowed value to not escape bounds when generating.");
                Console.WriteLine("--nindex | --start-nation-index (optional): start index to use for relative ids for spells (159-499). Defaults to minimum value. Should always be a bit less than the maximum allowed value to not escape bounds when generating.");
                Console.WriteLine("--siindex | --start-site-index (optional): start index to use for relative ids for sites (1700-3999). Defaults to 1700");
                Console.WriteLine("--enindex | --start-enchnbr-index (optional): start index to use for relative ids for enchantment numbers (200-9999). Defaults to 200");
                Console.WriteLine("--ecindex | --start-eventcode-index (optional): start index to use for relative ids for spells (-300--5000). Defaults to -300");
            }
            else if (ProgramArgs.TryParse(args, out var programArgs))
            {
                Execute(programArgs);
            }
            else
            {
                Console.WriteLine("Call the command with --help for information how to use.");
            }
        }

        private static void Execute(ProgramArgs programArgs)
        {
            Directory.SetCurrentDirectory(programArgs.workingDirectory);

            Console.WriteLine(Path.GetFullPath(programArgs.workingDirectory));
            Console.WriteLine(programArgs.outputPath);
            var outputFolder = Path.GetDirectoryName(programArgs.outputPath);
            if (Directory.Exists(outputFolder))
            {
                foreach (var entry in Directory.GetDirectories(outputFolder))
                    Directory.Delete(entry, true);
                foreach (var entry in Directory.GetFiles(outputFolder))
                    File.Delete(entry);
            }
            Directory.CreateDirectory(outputFolder);

            var modset = new ModSet(programArgs.startIndices);

            foreach (var file in Directory.GetFiles(programArgs.workingDirectory, "*.dme", SearchOption.AllDirectories))
            {
                Console.WriteLine($"Parse '{file}'");
                modset.Parse(file, programArgs.workingDirectory);
            }
            modset.BindIds();
            modset.Sort();

            using (var stream = new StreamWriter(File.OpenWrite(programArgs.outputPath), System.Text.Encoding.UTF8))
            {
                modset.Write(stream);
            }

            Console.WriteLine(outputFolder);
            // copy images
            foreach (var path in modset.GetImagePaths())
            {
                var destination = Path.Combine(outputFolder, path);
                var folder = Path.GetDirectoryName(destination);
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);
                File.Copy(path, destination);
            }

            Console.WriteLine($"Exported to '{Path.GetFullPath(programArgs.outputPath)}'");
        }

    }
}
