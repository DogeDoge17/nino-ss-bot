using cli_bot;
using Quill;
using Quill.Pages;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Path = cli_bot.Path;


internal class Program
{
    static List<Path> _imagePaths = [];
    static List<int> _allowedImages = new();

    static List<Path> GetAllFilesRecursive(string rootFolder)
    {
        List<Path> files = new(3000);
        void Search(string folder)
        {
            foreach (var file in Directory.GetFiles(folder))
            {
                files.Add(file);
            }
            foreach (var dir in Directory.GetDirectories(folder))
            {
                Search(dir);
            }
        }
        Search(rootFolder);
        return files;
    }

    private static void Main(string[] args)
    {
        _imagePaths = GetAllFilesRecursive(Path.Assembly / "Nino Nakano Screenshots");

        RefreshWords();

        DriverCreation.SetBrowserType(BrowserType.Firefox);
        DriverCreation.options.headless = true;
        TwitterBot nino = new(TimeSpan.FromMinutes(60)) { DisplayName = "Nino Nakano" };
        nino.RunAction += Run;
        nino.Start();
    }

    static void RefreshWords(int exclude = -1)
    {
        string saidPath = Path.Assembly / "said.txt";

        if (!File.Exists(saidPath))
        {
            File.WriteAllText(saidPath, "");
        }

        string[] lines = File.ReadAllLines(saidPath);

        HashSet<int> badWords = new(lines.Length);

        foreach (string line in lines)
        {
            if (line.Trim() != string.Empty)
            {
                badWords.Add(Int32.Parse(line));
            }
        }

        _allowedImages = new();

        bool safed = false;
    safety:
        if (badWords.Count == _imagePaths.Count || safed)
        {
            badWords = new();
            SaveBlacklist();
        }

        for (int i = 0; i < _imagePaths.Count; i++)
        {
            if (badWords.Contains(i) || i == exclude)
                continue;
            _allowedImages.Add(i);
        }

        if (_allowedImages.Count == 0)
        {
            safed = true;
            goto safety;
        }
    }

    static void SaveBlacklist()
    {
        HashSet<int> allowed = new(_allowedImages);

        StringBuilder outp = new StringBuilder();

        for (int i = 0; i < _imagePaths.Count; i++)
        {
            if (!allowed.Contains(i))
                outp.AppendLine(i.ToString());
        }

        File.WriteAllText(Path.Assembly / "said.txt", outp.ToString());
    }

    public static string GenerateContent(Path path)
    {
        if (path.Contains("Movie" + Path.GoodSep))
            return "Movie";

        if (path.Contains("Extra Art"))
            return "Extra Art";

        if (path.Contains("Extra") || path.Contains("Cover"))
        {
            if (path.Contains("Manga"))
                return path.ParentPath.ParentPath.FileName;
        }

        if (path.Contains("OVA") && path.Contains("Episode"))
        {
            int num = path.Contains("OVA 1") ? 1 : 2;

            return $"OVA {num} {path.ParentPath.FileName.Replace("Episode ", "E")}";
        }

        return path.ParentPath.FileName;
    }

    [DoesNotReturn]
    private static void TestContentNames()
    {
        HashSet<string> said = new();
        string digestiblePath = Path.Assembly / "name_output_digestible.txt";
        string fullPath = Path.Assembly / "name_output_full.txt";

        using (var writerDigestible = new StreamWriter(digestiblePath.ToString(), false))
        using (var writerFull = new StreamWriter(fullPath.ToString(), false))
        {
            for (int i = 0; i < _imagePaths.Count; i++)
            {
                Path trimmedPath = _imagePaths[i].RelativeTo(Path.Assembly);
                string content = GenerateContent(trimmedPath);

                writerFull.WriteLine($"{trimmedPath} => {content}");

                if (said.Contains(content))
                    continue;
                said.Add(content);
                writerDigestible.WriteLine($"{trimmedPath} => {content}");
            }
        }
        Environment.Exit(0);
    }

    static void Run(ComposePage composer, string[] args)
    {
        try
        {
            int rndTweet = Random.Shared.Next(0, _allowedImages.Count);
            Path imagePath = _imagePaths[_allowedImages[rndTweet]];

            Output.WriteLine($"Tweeting \"{imagePath}\"");
            composer.Tweet(GenerateContent(imagePath.RelativeTo(Path.Assembly)), imagePath);
            RefreshWords(rndTweet);
            SaveBlacklist();
            Output.WriteLine($"Finished");

        }
        catch (Exception ex)
        {
            Output.WriteLine(ex.Message);
        }
    }

}