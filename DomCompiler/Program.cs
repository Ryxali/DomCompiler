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
        static void Main(string[] args)
        {
            var programArgs = ProgramArgs.Parse(args);
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
                modset.Parse(file);
            }
            modset.BindIds();
            modset.Sort();

            using (var stream = new StreamWriter(File.OpenWrite(programArgs.outputPath)))
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
