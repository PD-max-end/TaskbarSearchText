using Microsoft.Win32;

namespace TaskbarSearchText;

/// <summary>
/// 集中处理 Windows Search 文字、首次修改备份和默认值恢复。
/// </summary>
internal static class RegistryHelper
{
    // Windows Search 自定义文字所在的注册表父键。
    private const string SearchRegistryPath =
        @"Software\Microsoft\Windows\CurrentVersion\Search";

    // Windows 不同版本可能读取的搜索框文字值名称。
    private const string SearchBoxTextValueName = "SearchBoxText";
    private const string SearchEditBoxTextValueName = "SearchEditBoxText";

    // 兼容旧版本使用的同名子键路径。
    private const string SearchBoxTextRegistryPath =
        SearchRegistryPath + @"\SearchBoxText";
    private const string SearchEditBoxTextRegistryPath =
        SearchRegistryPath + @"\SearchEditBoxText";

    // 本程序保存首次修改前文字的注册表位置。
    private const string BackupRegistryPath = @"Software\TaskbarSearchText";
    private const string BackupTextValueName = "BackupText";

    // 历史记录注册表位置和数量限制。
    private const string HistoryRegistryPath = BackupRegistryPath + @"\History";
    private const string HistoryValuePrefix = "History";
    private const int MaxHistoryCount = 10;

    /// <summary>
    /// 自动读取当前搜索框文字，优先读取较新的 SearchEditBoxText。
    /// </summary>
    public static string? GetCurrentSearchText()
    {
        return GetNonEmptyTextValue(SearchRegistryPath, SearchEditBoxTextValueName)
            ?? GetNonEmptyTextValue(SearchRegistryPath, SearchBoxTextValueName)
            ?? GetNonEmptyTextValue(SearchEditBoxTextRegistryPath, SearchEditBoxTextValueName)
            ?? GetNonEmptyTextValue(SearchBoxTextRegistryPath, SearchBoxTextValueName);
    }

    /// <summary>
    /// 首次应用前保存原始文字，然后同时写入两个搜索框文字值。
    /// </summary>
    public static void ApplySearchText(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        // 只保存第一次修改前的文字，后续修改不覆盖原始备份。
        SaveBackupIfNeeded(GetCurrentSearchText());
        WriteSearchText(text);

        // 写入后立即回读校验，仅记录真正写入成功的文字。
        if (VerifySearchText(text))
        {
            AddHistory(text);
        }
    }

    /// <summary>
    /// 立即回读两个搜索框文字值，任意一个与预期文字一致即视为成功。
    /// </summary>
    public static bool VerifySearchText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        return string.Equals(
                   GetNonEmptyTextValue(SearchRegistryPath, SearchBoxTextValueName),
                   text,
                   StringComparison.Ordinal) ||
               string.Equals(
                   GetNonEmptyTextValue(SearchRegistryPath, SearchEditBoxTextValueName),
                   text,
                   StringComparison.Ordinal) ||
               string.Equals(
                   GetNonEmptyTextValue(SearchBoxTextRegistryPath, SearchBoxTextValueName),
                   text,
                   StringComparison.Ordinal) ||
               string.Equals(
                   GetNonEmptyTextValue(SearchEditBoxTextRegistryPath, SearchEditBoxTextValueName),
                   text,
                   StringComparison.Ordinal);
    }

    /// <summary>
    /// 删除两个自定义文字值；若存在备份，则优先恢复备份文字。
    /// </summary>
    public static string? RestoreDefault()
    {
        // 先读取备份，再删除当前设置，确保恢复过程不受当前值影响。
        string? backupText = GetBackupText();
        DeleteSearchTextValues();

        if (!string.IsNullOrWhiteSpace(backupText))
        {
            WriteSearchText(backupText);
            return backupText;
        }

        // 备份不存在或原始状态为空时，保留删除状态以使用系统默认文字。
        return null;
    }

    /// <summary>
    /// 按从新到旧的顺序读取最多 10 条历史记录。
    /// </summary>
    public static IReadOnlyList<string> GetHistory()
    {
        using RegistryKey? historyKey =
            Registry.CurrentUser.OpenSubKey(HistoryRegistryPath);

        if (historyKey is null)
        {
            return Array.Empty<string>();
        }

        List<(int Index, string Text)> entries = new();

        foreach (string valueName in historyKey.GetValueNames())
        {
            if (!valueName.StartsWith(HistoryValuePrefix, StringComparison.Ordinal) ||
                !int.TryParse(valueName.AsSpan(HistoryValuePrefix.Length), out int index) ||
                historyKey.GetValue(valueName) is not string text ||
                string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            entries.Add((index, text));
        }

        return entries
            .OrderBy(entry => entry.Index)
            .Select(entry => entry.Text)
            .Take(MaxHistoryCount)
            .ToArray();
    }

    /// <summary>
    /// 同时写入 SearchBoxText 和 SearchEditBoxText。
    /// </summary>
    private static void WriteSearchText(string text)
    {
        using RegistryKey searchKey =
            Registry.CurrentUser.CreateSubKey(SearchRegistryPath, true)
            ?? throw new InvalidOperationException("无法创建 Windows Search 注册表项。");

        searchKey.SetValue(SearchBoxTextValueName, text, RegistryValueKind.String);
        searchKey.SetValue(SearchEditBoxTextValueName, text, RegistryValueKind.String);

        WriteLegacyTextValue(SearchBoxTextRegistryPath, SearchBoxTextValueName, text);
        WriteLegacyTextValue(SearchEditBoxTextRegistryPath, SearchEditBoxTextValueName, text);
    }

    /// <summary>
    /// 删除两个搜索框文字值，让 Windows 使用默认显示。
    /// </summary>
    private static void DeleteSearchTextValues()
    {
        using RegistryKey? searchKey =
            Registry.CurrentUser.OpenSubKey(SearchRegistryPath, true);

        if (searchKey is null)
        {
            return;
        }

        searchKey.DeleteValue(SearchBoxTextValueName, false);
        searchKey.DeleteValue(SearchEditBoxTextValueName, false);

        // 删除旧版本创建的同名子键，避免旧设置继续生效。
        Registry.CurrentUser.DeleteSubKeyTree(SearchBoxTextRegistryPath, false);
        Registry.CurrentUser.DeleteSubKeyTree(SearchEditBoxTextRegistryPath, false);
    }

    /// <summary>
    /// 仅在备份尚不存在时保存首次修改前的文字。
    /// </summary>
    private static void SaveBackupIfNeeded(string? currentText)
    {
        using RegistryKey backupKey =
            Registry.CurrentUser.CreateSubKey(BackupRegistryPath, true)
            ?? throw new InvalidOperationException("无法创建备份注册表项。");

        if (backupKey.GetValue(BackupTextValueName) is null)
        {
            backupKey.SetValue(
                BackupTextValueName,
                currentText ?? string.Empty,
                RegistryValueKind.String);
        }
    }

    /// <summary>
    /// 读取首次修改前保存的原始文字。
    /// </summary>
    private static string? GetBackupText()
    {
        using RegistryKey? backupKey =
            Registry.CurrentUser.OpenSubKey(BackupRegistryPath);

        return backupKey?.GetValue(BackupTextValueName) as string;
    }

    /// <summary>
    /// 添加历史记录，去重后仅保留最新 10 条。
    /// </summary>
    private static void AddHistory(string text)
    {
        List<string> history = GetHistory().ToList();
        history.RemoveAll(item => string.Equals(item, text, StringComparison.Ordinal));
        history.Insert(0, text);

        if (history.Count > MaxHistoryCount)
        {
            history.RemoveRange(MaxHistoryCount, history.Count - MaxHistoryCount);
        }

        using RegistryKey historyKey =
            Registry.CurrentUser.CreateSubKey(HistoryRegistryPath, true)
            ?? throw new InvalidOperationException("无法创建历史记录注册表项。");

        foreach (string valueName in historyKey.GetValueNames())
        {
            if (valueName.StartsWith(HistoryValuePrefix, StringComparison.Ordinal))
            {
                historyKey.DeleteValue(valueName, false);
            }
        }

        for (int index = 0; index < history.Count; index++)
        {
            historyKey.SetValue(
                HistoryValuePrefix + (index + 1),
                history[index],
                RegistryValueKind.String);
        }
    }

    /// <summary>
    /// 写入旧版子键路径中的对应字符串值。
    /// </summary>
    private static void WriteLegacyTextValue(string registryPath, string valueName, string text)
    {
        using RegistryKey key =
            Registry.CurrentUser.CreateSubKey(registryPath, true)
            ?? throw new InvalidOperationException("无法创建搜索框文字兼容注册表项。");

        key.SetValue(valueName, text, RegistryValueKind.String);
    }

    /// <summary>
    /// 从指定注册表位置安全读取非空字符串值。
    /// </summary>
    private static string? GetNonEmptyTextValue(string registryPath, string valueName)
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(registryPath);
        string? value =
            key?.GetValue(valueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames)
            as string;

        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
