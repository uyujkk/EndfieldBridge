// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 EndfieldBridge contributors.
// Modified 2026-10-15: AGPL distribution and license UI; see NOTICE.md for upstream credits.
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Win32;

namespace EndfieldBridge;

public sealed class Profile
{
    public string Game { get; set; } = "";
    public string Parent { get; set; } = "";
    public string Xxmi { get; set; } = "";
    public string GameArguments { get; set; } = "-force-d3d11";
    public string Language { get; set; } = "zh-CN";
    public bool WithMods { get; set; }
}

public static class Storage
{
    public static string Root => AppContext.BaseDirectory;
    public static string Data => Path.Combine(Root, "data");
    public static string Config => Path.Combine(Data, "profile.json");
    public static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    public static Profile Load()
    {
        MigrateLegacyData(Root);
        return File.Exists(Config) ? JsonSerializer.Deserialize<Profile>(File.ReadAllText(Config)) ?? throw new InvalidDataException("配置为空") : new();
    }
    internal static void MigrateLegacyData(string root)
    {
        string data=Path.Combine(root,"data"); Directory.CreateDirectory(data);
        // Move only known bridge state; never overwrite a newer data/ file.
        foreach(string name in new[]{"profile.json","integration-backup.json","bridge.log","loader-adaptation.txt","command-help.txt","self-test.txt","game-session.lock"})
        {
            string old=Path.Combine(root,name), current=Path.Combine(data,name);
            if(!File.Exists(old)||File.Exists(current)) continue;
            try { File.Move(old,current); }
            catch(IOException) { if(!File.Exists(current)) throw; }
        }
    }
    public static void Save(Profile p) { Directory.CreateDirectory(Data); Atomic(Config, JsonSerializer.Serialize(p, JsonOptions)); }
    public static void Atomic(string path, string text)
    {
        string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllText(temp, text, new System.Text.UTF8Encoding(false)); File.Move(temp, path, true); }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
    public static void Log(string s)
    {
        try { Directory.CreateDirectory(Data); File.AppendAllText(Path.Combine(Data,"bridge.log"), $"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}] {Locale.Text(s)}{Environment.NewLine}"); } catch { }
    }
    public static IEnumerable<string> Parents(string game)
    {
        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try { var r = Directory.GetParent(Path.GetDirectoryName(game)!)?.Parent?.FullName; if(r!=null) roots.Add(r); } catch { }
        using var cr = Registry.ClassesRoot;
        foreach (var name in cr.GetSubKeyNames().Where(n=>n.StartsWith("hypergryphlauncher",StringComparison.OrdinalIgnoreCase)))
        {
            using var key = cr.OpenSubKey(name + @"\shell\open\command");
            string cmd = key?.GetValue(null)?.ToString() ?? "";
            if(cmd.StartsWith('"')) { int end=cmd.IndexOf('"',1); if(end>1) { var r=Path.GetDirectoryName(cmd[1..end]); if(r!=null) roots.Add(r); } }
        }
        foreach(var root in roots)
        {
            if(!Directory.Exists(root)) continue;
            var candidates = Directory.GetDirectories(root).Select(d=>new { Path=Path.Combine(d,"Games.exe"), V=Version.TryParse(Path.GetFileName(d),out var v)?v:new Version(0,0) }).OrderByDescending(x=>x.V);
            foreach(var c in candidates) if(File.Exists(c.Path)) yield return c.Path;
        }
    }
}

public static class Integration
{
    static readonly string[] Fields = ["game_launch", "start_method", "game_folder", "game_process_exe_enabled", "game_process_exe", "custom_launch", "custom_launch_signature"];
    public static string ConfigPath(string exe)
    {
        var d = new DirectoryInfo(Path.GetDirectoryName(Path.GetFullPath(exe))!);
        for(int i=0;i<4 && d!=null;i++,d=d.Parent!) { var p=Path.Combine(d.FullName,"XXMI Launcher Config.json"); if(File.Exists(p)) return p; }
        throw new FileNotFoundException("没有找到 XXMI Launcher Config.json，请选择实际 XXMI Launcher.exe。");
    }
    public static JsonObject Read(string path) => JsonNode.Parse(File.ReadAllText(path)) as JsonObject ?? throw new InvalidDataException("XXMI 配置不是 JSON 对象");
    public static JsonObject Efmi(JsonObject doc) => doc["Importers"]?["EFMI"]?["Importer"] as JsonObject ?? throw new InvalidDataException("XXMI 配置缺少 EFMI，未执行写入。");
    public static string Command => Win32.Quote(Path.Combine(Storage.Root,"EndfieldBridge.exe")) + " --game-only --from-xxmi";
    static string LegacyCommand => Win32.Quote(Path.Combine(Storage.Root,"EndfieldBridge.exe")) + " --game-only";
    public static bool ValidGame(string? exe) => !string.IsNullOrWhiteSpace(exe) && File.Exists(exe) && Path.GetFileName(exe).Equals("Endfield.exe",StringComparison.OrdinalIgnoreCase) && Directory.Exists(Path.Combine(Path.GetDirectoryName(exe)!,"Endfield_Data"));
    static string? FolderExe(JsonObject ef)
    {
        string raw=ef["game_folder"]?.GetValue<string>()??"";
        try { string exe=Path.Combine(raw,"Endfield.exe");return ValidGame(exe)?Path.GetFullPath(exe):null; } catch(ArgumentException){return null;}
    }
    // Verification only: never load XXMI's private key or create its trust signatures.
    public static bool IsCommandAuthorized(Profile p)
    {
        try
        {
            var ef=Efmi(Read(ConfigPath(p.Xxmi)));
            string command=ef["custom_launch"]?.GetValue<string>()??"";
            string signature=ef["custom_launch_signature"]?.GetValue<string>()??"";
            if(command!=Command||string.IsNullOrWhiteSpace(signature))return false;
            string pub=Path.Combine(Path.GetDirectoryName(ConfigPath(p.Xxmi))!,"Resources","Security","public_key.der");
            using var key=ECDsa.Create();key.ImportSubjectPublicKeyInfo(Convert.FromBase64String(File.ReadAllText(pub).Trim()),out _);
            return key.VerifyData(Encoding.UTF8.GetBytes(command),Convert.FromBase64String(signature),HashAlgorithmName.SHA256,DSASignatureFormat.Rfc3279DerSequence);
        }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or CryptographicException or FormatException or InvalidOperationException){return false;}
    }
    public static string AuthorizationStatus(Profile p) => IsCommandAuthorized(p)
        ? "XXMI 命令授权：签名验证通过（仅使用公钥核验）"
        : "XXMI 命令授权：尚未通过。首次启动核对路径后，在 XXMI 弹窗选择“保持”；“重置”会清空命令。";
    public static string Inspect(Profile p)
    {
        string path=ConfigPath(p.Xxmi); var ef=Efmi(Read(path));
        string warning=FolderExe(ef)==null?"\r\n注意：EFMI 游戏目录无效，不能填写快捷方式或桥接程序。":"";
        return $"XXMI 配置：{path}\r\n游戏目录：{ef["game_folder"]}\r\n启动模式：{ef["game_launch"]} / {ef["start_method"]}\r\n识别进程：{ef["game_process_exe"]}；注入模式：{ef["xxmi_dll_inject_mode"]}\r\n自定义命令：{ef["custom_launch"]}\r\n目标命令：{Command}\r\n{AuthorizationStatus(p)}{warning}";
    }
    public static string Import(Profile p, bool strict=false)
    {
        var ef=Efmi(Read(ConfigPath(p.Xxmi))); string? exe=FolderExe(ef);
        if(exe==null && (strict || !ValidGame(p.Game))) throw new InvalidDataException("EFMI 的游戏目录无效。这里必须是包含 Endfield.exe 和 Endfield_Data 的实际目录，不能填写 .lnk 快捷方式。");
        if(exe!=null) p.Game=exe;
        p.GameArguments=ef["d3d11_mode_cmd_args"]?.GetValue<string>()??"";
        if(ef["use_launch_options"]?.GetValue<bool>()==true) p.GameArguments += " " + (ef["launch_options"]?.GetValue<string>()??"");
        p.GameArguments=p.GameArguments.Trim();
        Program.ValidateGame(p);
        return exe==null?"EFMI 游戏目录无效：已保留本工具的有效游戏路径，仅读取启动参数。适配时会修正 EFMI 目录。":"已读取 EFMI 游戏目录和启动参数。";
    }
    public static void ValidateMode(Profile p)
    {
        var ef=Efmi(Read(ConfigPath(p.Xxmi))); string inject=ef["xxmi_dll_inject_mode"]?.ToString()??"";
        if(inject!="DIRECT"&&inject!="HOOK")throw new InvalidDataException("EFMI 注入模式必须为 DIRECT 或 HOOK，当前未启用受支持的 Mod 加载方式。");
        if(!IsConfigured(p))throw new InvalidDataException("EFMI 配置不完整，请使用“备份并适配 EFMI”修复目录、进程名和启动命令。");
        if(ef["run_pre_launch_enabled"]?.GetValue<bool>()==true && (ef["run_pre_launch"]?.ToString()??"").Contains("EndfieldBridge",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("请移除前置命令中的桥接调用：桥接应放在自定义启动命令中，前置等待会阻塞加载流程。");
    }
    public static bool IsConfigured(Profile p)
    {
        try
        {
            var ef=Efmi(Read(ConfigPath(p.Xxmi))); string? exe=FolderExe(ef);
            return ef["game_launch"]?.ToString()=="CUSTOM" && ef["start_method"]?.ToString()=="NATIVE"
                && ef["custom_launch"]?.ToString()==Command && ef["game_process_exe_enabled"]?.GetValue<bool>()==true
                && ef["game_process_exe"]?.ToString()=="Endfield.exe" && exe!=null
                && Path.GetFullPath(p.Game).Equals(exe,StringComparison.OrdinalIgnoreCase);
        } catch {return false;}
    }
    public static void CheckClosed()
    {
        if(Process.GetProcessesByName("XXMI Launcher").Length>0) throw new InvalidOperationException("请先退出 XXMI，避免其覆盖配置。不会结束现有 XXMI 进程。");
        if(Process.GetProcessesByName("Endfield").Length>0) throw new InvalidOperationException("请先退出终末地再修改适配配置。");
    }
    public static string Apply(Profile p, string? statePath=null)
    {
        CheckClosed(); Program.ValidateGame(p);
        if(!ValidGame(p.Game))throw new InvalidDataException("游戏路径必须包含 Endfield.exe 和同目录 Endfield_Data。");
        if(IsConfigured(p)) throw new InvalidOperationException("已适配，不覆盖首次备份。");
        string path=ConfigPath(p.Xxmi),original=File.ReadAllText(path);var doc=JsonNode.Parse(original)!.AsObject();var ef=Efmi(doc);
        string mode=ef["xxmi_dll_inject_mode"]?.ToString()??"";
        if(mode!="DIRECT" && mode!="HOOK")throw new InvalidOperationException("当前 EFMI 注入模式不是 DIRECT 或 HOOK。");
        if(ef["run_pre_launch_enabled"]?.GetValue<bool>()==true && (ef["run_pre_launch"]?.ToString()??"").Contains("EndfieldBridge",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("前置命令包含桥接调用，请先移除，以免阻塞 Mod 加载。");
        string backup=path+".endfieldbridge-"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fff")+".bak";
        File.WriteAllText(backup,original,new System.Text.UTF8Encoding(false));
        string stateFile=statePath??Path.Combine(Storage.Data,"integration-backup.json");
        bool preserve=false;
        if(File.Exists(stateFile) && (ef["custom_launch"]?.ToString()==Command||ef["custom_launch"]?.ToString()==LegacyCommand))
        { var old=Read(stateFile);preserve=old["config"]?.ToString()==path; }
        if(!preserve)
        {
            var fields=new JsonObject();foreach(string key in Fields)fields[key]=new JsonObject{["exists"]=ef.ContainsKey(key),["value"]=ef[key]?.DeepClone()};
            var state=new JsonObject{["schema"]=2,["config"]=path,["backup"]=backup,["fields"]=fields};
            Storage.Atomic(stateFile,state.ToJsonString(Storage.JsonOptions));
        }
        string previousCommand=ef["custom_launch"]?.ToString()??"";
        ef["game_launch"]="CUSTOM";ef["start_method"]="NATIVE";ef["game_folder"]=Path.GetDirectoryName(Path.GetFullPath(p.Game));
        ef["game_process_exe_enabled"]=true;ef["game_process_exe"]="Endfield.exe";ef["custom_launch"]=Command;
        if(!string.Equals(previousCommand,Command,StringComparison.Ordinal))ef["custom_launch_signature"]="";
        if(File.ReadAllText(path)!=original)throw new IOException("XXMI 配置已被其他程序修改，已停止写入。");
        Storage.Atomic(path,doc.ToJsonString(Storage.JsonOptions));return backup;
    }
    public static void Restore(string? statePath=null)
    {
        CheckClosed();var state=Read(statePath??Path.Combine(Storage.Data,"integration-backup.json"));string path=state["config"]!.GetValue<string>();string original=File.ReadAllText(path);var doc=JsonNode.Parse(original)!.AsObject();var ef=Efmi(doc);
        if(ef["custom_launch"]?.ToString()!=Command&&ef["custom_launch"]?.ToString()!=LegacyCommand)throw new InvalidOperationException("自定义命令已改变，停止恢复以保留你的新设置。");
        File.Copy(path,path+".before-restore-"+DateTime.Now.ToString("yyyyMMddHHmmssfff")+".bak");
        if(state["fields"] is JsonObject fields)
        {foreach(var field in fields){var saved=field.Value!.AsObject();if(saved["exists"]!.GetValue<bool>())ef[field.Key]=saved["value"]?.DeepClone();else ef.Remove(field.Key);}}
        else foreach(var k in new[]{"game_launch","custom_launch","custom_launch_signature"})ef[k]=state[k]?.DeepClone();
        if(File.ReadAllText(path)!=original)throw new IOException("配置已改变，停止恢复。");
        Storage.Atomic(path,doc.ToJsonString(Storage.JsonOptions));
    }
    public static void Export()
    {
        string body="@echo off\r\n\"%~dp0EndfieldBridge.exe\" --launch\r\nexit /b %errorlevel%\r\n";
        File.WriteAllText(Path.Combine(Storage.Root,"StartFromModManager.cmd"),body,System.Text.Encoding.ASCII);
    }
}
