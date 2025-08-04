using MelonLoader;
using MelonLoader.TinyJSON;
using MelonLoader.Utils;
using System.Text;
using UnityEngine;

namespace LocalizationUtilities;

public static class LocalizationManager
{
    internal static HashSet<LocalizationSet> Localizations { get; private set; } = new();

    public static void AddLocalizations(LocalizationSet set)
    {
        set.Validate();
        Localizations.Add(set);
    }

    public static void LoadLocalization(TextAsset asset, string path)
    {
        if (path.ToLower().EndsWith(".json", System.StringComparison.Ordinal))
        {
            LoadJsonLocalization(asset);
        }
        else
        {
            MelonLogger.Warning($"Found localization '{path}' that could not be loaded.");
        }
    }


    private static string GetText(TextAsset textAsset)
    {
        const byte leftCurlyBracket = (byte)'{';
        byte[] bytes = textAsset.bytes;
        int index = Array.IndexOf(bytes, leftCurlyBracket);
        if (index < 0)
        {
            throw new ArgumentException("TextAsset has no Json content.", nameof(textAsset));
        }
        return Encoding.UTF8.GetString(new ReadOnlySpan<byte>(bytes, index, bytes.Length - index));
    }

    public static bool LoadJsonLocalization(TextAsset textAsset)
    {
        string contents = GetText(textAsset);
        return LoadJsonLocalization(contents);
    }

    public static bool LoadJsonLocalization(string contents)
    {
        if (string.IsNullOrWhiteSpace(contents))
            return false;

        string i18nPath = Path.Combine(MelonEnvironment.ModsDirectory, "Localization.json");
        Dictionary<string, Dictionary<string, string>> i18nDict = new();

        if (File.Exists(i18nPath))
        {
            string i18nRaw = File.ReadAllText(i18nPath);
            i18nDict = JSON.Load(i18nRaw).Make<Dictionary<string, Dictionary<string, string>>>();
        }

        ProxyObject newDict = (ProxyObject)JSON.Load(contents);
        Dictionary<string, Dictionary<string, string>> parsedDict = new();

        foreach (KeyValuePair<string, Variant> pair in newDict)
        {
            string key = pair.Key;
            Dictionary<string, string> locEntry = pair.Value.Make<Dictionary<string, string>>();

            if (!i18nDict.ContainsKey(key))
            {
                Dictionary<string, string> newEntry = new();

                if (locEntry.TryGetValue("English", out string eng))
                    newEntry["English"] = eng;

                if (!newEntry.ContainsKey("Simplified Chinese"))
                    newEntry["Simplified Chinese"] = "null";

                i18nDict[key] = newEntry;
            }
            else
            {
                var existingEntry = i18nDict[key];

                if (existingEntry.TryGetValue("Simplified Chinese", out string zh) && !string.IsNullOrWhiteSpace(zh) && zh != "null")
                {
                    locEntry["Simplified Chinese"] = zh;
                }
            }

            parsedDict[key] = locEntry;
        }

        string updatedI18nJson = JSON.Dump(i18nDict, EncodeOptions.PrettyPrint);
        File.WriteAllText(i18nPath, updatedI18nJson);

        List<LocalizationEntry> newEntries = new();
        foreach (var pair in parsedDict)
        {
            newEntries.Add(new LocalizationEntry(pair.Key, pair.Value));
        }

        AddLocalizations(new LocalizationSet(newEntries, true));
        return true;
    }

    /// <summary>
    /// Returns an array of string variables without any leading or trailing whitespace
    /// </summary>
    /// <param name="values">An array of string variables.</param>
    /// <returns>A new array containing the trimmed values.</returns>
    private static string[] Trim(string[] values)
    {
        string[] result = new string[values.Length];

        for (int i = 0; i < values.Length; i++)
        {
            result[i] = values[i].Trim();
        }

        return result;
    }
}
