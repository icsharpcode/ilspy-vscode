using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

using static ProjectVersion;

public static class ProjectVersionWriter
{
    public static UTF8Encoding UTF8WithoutBOM => new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    public static void WriteToVsProject(params string[] files)
    {
        WriteVersionToFiles("<Version>$$$</Version>", Version.ToString(), files);
        WriteVersionToFiles("<AssemblyVersion>$$$</AssemblyVersion>", Version.ToString(), files);
        WriteVersionToFiles("<FileVersion>$$$</FileVersion>", Version.ToString(), files);
    }

    public static void WriteToPackageJson(params string[] files)
    {
        WriteVersionToFiles("\"version\": \"$$$\",", Version.ToString(3), files);
    }

    public static void WriteVersionToFiles(string template, string version, params string[] files)
    {
        foreach (var file in files)
        {
            string origText = File.ReadAllText(file, Encoding.UTF8);
            string newText = Regex.Replace(origText,
                template.Replace("$$$", "(.*)"),
                template.Replace("$$$", version));
            File.WriteAllText(file, newText, UTF8WithoutBOM);
        }
    }
}