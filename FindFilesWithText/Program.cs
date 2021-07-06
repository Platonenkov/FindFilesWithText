using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UpdatePackages;
using UpdatePackages.Classes;

namespace FindFilesWithText
{
    class Program
    {
        private static IEnumerable<FindSettings> Settings = new[]
        {
            new FindSettings()
            {
                Sections = new[]
                {
                    new Section()
                    {
                        FileMask = "*.*",
                        Regular = new[]
                        {
                            "Some row"
                        }
                    },
                },
                Directory = ""
            }
        };
        private const string SettingFileName = "FindSettings.json";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Read configuration file");
            if (!File.Exists(SettingFileName))
            {
                "Configuration file not found".ConsoleRed();
                try
                {
                    "Attempt to create a configuration file".ConsoleYellow();
                    await JsonInFile.SaveToFileAsync(SettingFileName, Settings);
                    $"Please enter configuration to the file - {SettingFileName}".PrintMessgeAndWaitEnter();
                    return;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    Console.ReadLine();
                    return;
                }
            }
            IEnumerable<FindSettings> settings;
            try
            {
                var data = await JsonInFile.LoadFromFile<List<FindSettings>>(SettingFileName);
                if (data is null)
                {
                    "Configuration is not correct".ConsoleRed();
                    "press Enter to close programm".PrintMessgeAndWaitEnter();
                    return;
                }

                settings = data;
            }
            catch (Exception e)
            {
                $"Error to read configuration - {e.Message}".ConsoleRed();
                Console.WriteLine(e);
                "press Enter to close programm".PrintMessgeAndWaitEnter();
                return;
            }

            var watcher = new Stopwatch();
            watcher.Start();

            var result = new List<(string,string)>();
            var tasks = settings.Select(
                    setting => Task.Run(() => FindFiles(setting.Directory, setting.Sections))
                       .ContinueWith(
                            t =>
                            {
                                if (t.Result == null) return;
                                lock (result)
                                {
                                    result.AddRange(t.Result);
                                }
                            }))
               .ToList();

            if (tasks.Count == 0)
            {
                Console.WriteLine("NoData");
                Console.ReadKey();
                return;
            }

            Task.WaitAll(tasks.ToArray());
            Console.WriteLine("Finish");
            Console.WriteLine($"Completed in {GetStringTime(watcher.Elapsed)}");
            Console.WriteLine("Press Enter to write data in file result.txt");
            Console.ReadKey();
            await using var sw = new StreamWriter("result.txt", false);
            {
                var grouped = result.GroupBy(r => r.Item2);
                foreach (var group in grouped)
                {
                    Console.WriteLine(group.Key);
                    await sw.WriteLineAsync(group.Key);
                    foreach (var (file, _) in group)
                    {
                        await sw.WriteLineAsync(file);
                        Console.WriteLine(file);
                    }
                }
            }
        }

        private static IEnumerable<(string, string)> FindFiles(string dir, IEnumerable<Section> settings)
        {
            var directory = new DirectoryInfo(dir);
            if (!directory.Exists)
            {
                $"Directory not found {directory.FullName}".ConsoleRed();
                return null;
            }

            var result = new List<(string, string)>();
            var tasks = settings.Select(
                    setting => Task.Run(() => FindFiles(directory, setting))
                       .ContinueWith(
                            t =>
                            {
                                if (t.Result == null) return;
                                lock (result)
                                {
                                    result.AddRange(t.Result);
                                }
                            }))
               .ToList();

            if (tasks.Count == 0)
                return null;


            Task.WaitAll(tasks.ToArray());

            return result;

        }
        private static IEnumerable<(string, string)> FindFiles(DirectoryInfo directory, Section setting)
        {
            if (!directory.Exists)
            {
                "No Directory".ConsoleRed();
                return null;
            }
            Console.WriteLine($"Find package files...");

            var files = directory.EnumerateFiles(setting.FileMask, SearchOption.AllDirectories).ToArray();
            if (files.Length == 0)
            {
                $"No files".ConsoleYellow();
                return null;
            }

            var end = files.Length > 1 ? "s" : "";
            Total += files.Length;

            $"Found {files.Length} file{end}".ConsoleGreen();

            var result = new List<(string, string)>();
            var tasks = files.Select(
                    file => TakeFilesWithSettings(file.FullName, setting.Regular)
                       .ContinueWith(
                            t =>
                            {
                                if (t.Result == null) return;
                                lock (result)
                                {
                                    result.AddRange(t.Result);
                                }
                            }))
               .ToList();

            if (tasks.Count == 0)
                return null;


            Task.WaitAll(tasks.ToArray());

            return result;

        }

        private static int Total;
        private static double Count;
        private static async Task<IEnumerable<(string, string)>> TakeFilesWithSettings(string filePath, IEnumerable<string> settings)
        {
            try
            {
                var text = await File.ReadAllTextAsync(filePath);
                var result = (from setting in settings where text.Contains(setting) select (filePath, setting)).ToList();
                if (result.Count > 0)
                {
                    foreach (var tuple in result)
                        Console.WriteLine($"{tuple.setting} | {tuple.filePath}");


                    return result;
                }

                var percent = ++Count / Total;

                await Task.Run(() => Console.Title = $"{Count} | {percent:P}").ConfigureAwait(false);
                return result.Count > 0 ? result : null;
            }
            catch (Exception e)
            {
                ++Count;
                $"File - {filePath}\n{e.Message}\n".ConsoleRed();
                return null;
            }
        }
        /// <summary>
        /// Get time in string format
        /// </summary>
        private static string GetStringTime(TimeSpan time)
        {
            return time.Days > 0 ? time.ToString(@"d\.hh\:mm\:ss") :
                time.Hours > 0 ? time.ToString(@"hh\:mm\:ss") :
                time.Minutes > 0 ? time.ToString(@"mm\:ss") :
                time.Seconds > 0 ? time.ToString(@"g") : $"{Math.Round(time.TotalMilliseconds, 0)} ms";
        }

    }
}
