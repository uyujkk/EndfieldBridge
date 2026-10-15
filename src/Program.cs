// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 EndfieldBridge contributors.
// Modified 2026-10-15: AGPL distribution and license UI; see NOTICE.md for upstream credits.
using System.Diagnostics;
using System.Security.Principal;
using System.Text.Json;

namespace EndfieldBridge;

internal static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        // Presence guard lets Setup refuse replacement while any bridge process is alive.
        using var installationGuard = new Mutex(false, @"Local\EndfieldBridge.Running");
        ApplicationConfiguration.Initialize();
        try
        {
            Locale.Language=Storage.Load().Language;
            if(args.Length==2 && args[0]=="--probe")
            {
                Win32.NtQueryInformationProcess(Process.GetCurrentProcess().Handle,0,out var info,System.Runtime.InteropServices.Marshal.SizeOf<Win32.BasicInfo>(),out _);
                File.WriteAllText(args[1],info.parent.ToInt64().ToString());return 0;
            }
            if(args.Length==1 && args[0]=="--self-test") return SelfTest();
            if(args.Length>=2 && args[0]=="--render-preview")
            {
                using var form=new MainForm(false);
                var lang=args.FirstOrDefault(a=>a.StartsWith("--language="))?.Split('=',2)[1]; if(lang!=null)form.PreviewLanguage(lang);
                var page=args.FirstOrDefault(a=>a.StartsWith("--page="))?.Split('=',2)[1];form.PreviewPage(page=="setup"?1:page=="risk"?2:0);
                form.Show();Application.DoEvents();
                using var bitmap=new Bitmap(form.Width,form.Height);form.DrawToBitmap(bitmap,form.ClientRectangle with { Width=form.Width,Height=form.Height });bitmap.Save(args[1]);return 0;
            }
            if(args.Length==1 && args[0]=="--help") {File.WriteAllText(Path.Combine(Storage.Root,"command-help.txt"),"EndfieldBridge.exe [--launch | --game-only | --self-test]\n无参数：配置界面\n--launch：按保存模式启动，供 Mod 管理器调用\n--game-only：仅启动游戏，供 XXMI 自定义命令调用\n");return 0;}
            if(args.Length==1 && args[0]=="--configure-loader")
            {
                try
                {
                    var profile=Storage.Load();string imported=Integration.Import(profile);ValidateGame(profile);ResolveParent(profile,Storage.Log);
                    string backup=Integration.Apply(profile);profile.WithMods=true;Storage.Save(profile);Integration.Export();
                    File.WriteAllText(Path.Combine(Storage.Root,"loader-adaptation.txt"),imported+"\r\n备份："+backup+"\r\n"+Integration.Inspect(profile));return 0;
                }
                catch(Exception ex){File.WriteAllText(Path.Combine(Storage.Root,"loader-adaptation.txt"),ex.ToString());return 1;}
            }
            bool fromXxmi=args.SequenceEqual(new[]{"--game-only","--from-xxmi"});
            bool gameOnly=fromXxmi||args.SequenceEqual(new[]{"--game-only"});
            if(args.Length>0&&!gameOnly&&!args.SequenceEqual(new[]{"--launch"}))throw new ArgumentException("不支持的命令行参数。");
            if(gameOnly)
            {
                if(fromXxmi&&!IsAdmin())throw new InvalidOperationException("请以管理员身份运行 XXMI 或通过桥接主界面启动 Mod，避免在加载过程中提权超时。");
                if(!IsAdmin()){Elevate("--game-only");return 0;}
                using var mutex=new Mutex(false,@"Local\EndfieldBridge.GameSession");
                if(!mutex.WaitOne(0))throw new InvalidOperationException("桥接启动会话已在运行。");
                try
                {
                    var profile=Storage.Load();
                    if(fromXxmi){Storage.Log(Integration.Import(profile,strict:true));Integration.ValidateMode(profile);}
                    return LaunchGame(profile,Storage.Log);
                }
                finally{mutex.ReleaseMutex();}
            }            if(args.Contains("--launch")&&!IsAdmin()) {Elevate("--launch");return 0;}
            Application.Run(new MainForm(args.Contains("--launch")));return 0;
        }
        catch(Exception ex) {Storage.Log(ex.ToString());MessageBox.Show(Locale.Text(ex.Message),Locale.Choose("终末地启动桥接","Endfield Bridge"),MessageBoxButtons.OK,MessageBoxIcon.Error);return 1;}
    }
    internal static bool IsAdmin() => new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
    internal static void Elevate(string args) => Process.Start(new ProcessStartInfo(Environment.ProcessPath!,args){UseShellExecute=true,Verb="runas",WorkingDirectory=Storage.Root});
    internal static void ValidateGame(Profile p)
    {
        if(!File.Exists(p.Game)||!Path.GetFileName(p.Game).Equals("Endfield.exe",StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("请选择存在的 Endfield.exe。");
        if(p.GameArguments.Contains('\r')||p.GameArguments.Contains('\n')||p.GameArguments.Contains('\0')) throw new InvalidDataException("启动参数不能包含换行或空字符。");
    }
    internal static string ResolveParent(Profile p,Action<string> log)
    {
        if(!string.IsNullOrWhiteSpace(p.Parent)) {Win32.Verify(p.Parent);return p.Parent;}
        var failures=new List<string>();
        foreach(var candidate in Storage.Parents(p.Game)) {try{Win32.Verify(candidate);log("检测到官方文件："+candidate);return candidate;}catch(Exception ex){failures.Add(Path.GetFileName(Path.GetDirectoryName(candidate))+": "+ex.Message);}}
        throw new InvalidDataException("未找到签名有效的官方 Games.exe。"+(failures.Count>0?"\r\n"+string.Join("\r\n",failures):"请手动选择官方文件。"));
    }
    internal static int LaunchGame(Profile p,Action<string> log)
    {
        using var sessionLock=File.Open(Path.Combine(Storage.Root,"game-session.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        ValidateGame(p);
        if(Process.GetProcessesByName("Endfield").Length>0) throw new InvalidOperationException("检测到终末地已在运行，请先退出，不会结束现有进程。");
        string parent=ResolveParent(p,log);
        using var session=Session.Create(parent,p.Game,p.GameArguments,log);
        int code=session.Wait();log("游戏进程已退出，退出码 "+code);return code;
    }
    static int SelfTest()
    {
        var report=new List<string>();
        void Check(bool pass,string message) {report.Add((pass?"PASS ":"FAIL ")+message);if(!pass)throw new InvalidOperationException(message);}
        try
        {
            Check(Win32.Quote("")=="\"\"","空参数转义");
            Check(Win32.Quote("x\\")=="\"x\\\\\"","尾部反斜杠转义");
            Check(Win32.Quote("a\"b")=="\"a\\\"b\"","内嵌引号转义");
            bool rejected=false;try{Win32.Verify(Environment.ProcessPath!);}catch(InvalidDataException){rejected=true;}Check(rejected,"拒绝非官方父进程");
                        string fixture=Path.Combine(Storage.Root,"test-fixture");Directory.CreateDirectory(fixture);
            string mockExe=Path.Combine(fixture,"XXMI Launcher.exe");File.WriteAllText(mockExe,"test fixture - never executed");
            string mockConfig=Path.Combine(fixture,"XXMI Launcher Config.json");
            string original="{\"Launcher\":{\"active_importer\":\"ZZMI\"},\"Importers\":{\"EFMI\":{\"Importer\":{\"game_launch\":\"MANUAL\",\"custom_launch\":\"previous\",\"custom_launch_signature\":\"keep\",\"xxmi_dll_inject_mode\":\"DIRECT\",\"launch_options\":\"preserve me\"}},\"ZZMI\":{\"preserve\":true}}}";
            string mockGame=Path.Combine(fixture,"Game");Directory.CreateDirectory(Path.Combine(mockGame,"Endfield_Data"));string mockGameExe=Path.Combine(mockGame,"Endfield.exe");File.WriteAllText(mockGameExe,"Test fixture - never executed");
            File.WriteAllText(mockConfig,original);var mock=new Profile{Xxmi=mockExe,Game=mockGameExe};string mockState=Path.Combine(fixture,"backup-state.json");
            Integration.Apply(mock,mockState);Check(Integration.IsConfigured(mock),"模拟配置：写入 CUSTOM 和桥接命令");
                        var changed=Integration.Read(mockConfig);
            Check(Integration.Efmi(changed)["start_method"]?.ToString()=="NATIVE" && Integration.Efmi(changed)["game_process_exe_enabled"]?.GetValue<bool>()==true && Integration.Efmi(changed)["game_process_exe"]?.ToString()=="Endfield.exe","模拟配置：明确使用 NATIVE 并识别 Endfield.exe");
            Check(Directory.Exists(Integration.Efmi(changed)["game_folder"]?.ToString()),"模拟配置：游戏目录指向真实目录而非快捷方式");
            var edited=changed.DeepClone().AsObject();var editedEf=Integration.Efmi(edited);editedEf["game_folder"]="missing-shortcut.lnk";Storage.Atomic(mockConfig,edited.ToJsonString());
            using(var testKey=System.Security.Cryptography.ECDsa.Create(System.Security.Cryptography.ECCurve.NamedCurves.nistP384))
            {
                string pubDir=Path.Combine(fixture,"Resources","Security");Directory.CreateDirectory(pubDir);
                File.WriteAllText(Path.Combine(pubDir,"public_key.der"),Convert.ToBase64String(testKey.ExportSubjectPublicKeyInfo()));
                string signed=Convert.ToBase64String(testKey.SignData(System.Text.Encoding.UTF8.GetBytes(Integration.Command),System.Security.Cryptography.HashAlgorithmName.SHA256,System.Security.Cryptography.DSASignatureFormat.Rfc3279DerSequence));
                editedEf["custom_launch_signature"]=signed;Storage.Atomic(mockConfig,edited.ToJsonString());
                Check(Integration.IsCommandAuthorized(mock),"模拟授权：使用公钥验证 ECDSA SHA256 命令签名");
                editedEf["custom_launch"]=Integration.Command+" altered";Storage.Atomic(mockConfig,edited.ToJsonString());
                Check(!Integration.IsCommandAuthorized(mock),"模拟授权：修改命令后原签名无效");
                editedEf["custom_launch"]=Integration.Command;Storage.Atomic(mockConfig,edited.ToJsonString());
                Integration.Apply(mock,mockState);
                Check(Integration.Efmi(Integration.Read(mockConfig))["custom_launch_signature"]?.ToString()==signed && Integration.IsCommandAuthorized(mock),"修复目录时保留同一命令的已有授权");
                Storage.Atomic(mockConfig,edited.ToJsonString());
            }
            string validBefore=mock.Game;string note=Integration.Import(mock);Check(mock.Game==validBefore && note.Contains("保留"),"错误 EFMI 目录不会覆盖工具的有效游戏路径");
            bool strictRejected=false;try{Integration.Import(mock,strict:true);}catch(InvalidDataException){strictRejected=true;}Check(strictRejected,"加载器调用时拒绝无效游戏目录");
            string baselineState=File.ReadAllText(mockState);Integration.Apply(mock,mockState);Check(File.ReadAllText(mockState)==baselineState,"修复同一桥接配置时保留首次恢复点");
            changed=Integration.Read(mockConfig);edited=changed.DeepClone().AsObject();editedEf=Integration.Efmi(edited);editedEf["d3d11_mode_cmd_args"]="-force-d3d11";editedEf["use_launch_options"]=true;editedEf["launch_options"]="-screen-width 1920";Storage.Atomic(mockConfig,edited.ToJsonString());
            Integration.Import(mock,strict:true);Check(mock.GameArguments=="-force-d3d11 -screen-width 1920","从 EFMI 动态读取图形参数和用户参数");
            editedEf["use_launch_options"]=false;Storage.Atomic(mockConfig,edited.ToJsonString());Integration.Import(mock,strict:true);Check(mock.GameArguments=="-force-d3d11","关闭 use_launch_options 后不转发用户参数");
            Storage.Atomic(mockConfig,changed.ToJsonString());Check(changed["Launcher"]?["active_importer"]?.ToString()=="ZZMI" && changed["Importers"]?["ZZMI"]?["preserve"]?.GetValue<bool>()==true,"模拟配置：其他仓库和活动导入器保留");
            Check(Integration.Efmi(changed)["launch_options"]?.ToString()=="preserve me","模拟配置：其他 EFMI 字段保留");
            bool repeated=false;try{Integration.Apply(mock,mockState);}catch(InvalidOperationException){repeated=true;}Check(repeated,"模拟配置：重复适配不会覆盖首次备份");
            Integration.Restore(mockState);Check(System.Text.Json.Nodes.JsonNode.DeepEquals(Integration.Read(mockConfig),System.Text.Json.Nodes.JsonNode.Parse(original)),"模拟配置：恢复后与原配置一致");
            var p=Storage.Load();if(Integration.ValidGame(p.Game)){string parent=ResolveParent(p,s=>report.Add(s));Check(File.Exists(parent),"找到并验证官方 Games.exe");}else report.Add("SKIP Official signature integration check: no game configured.");
            // The probe starts only this executable as its own suspended parent and child.
            string probe=Path.Combine(Storage.Root,"parent-probe.txt");if(File.Exists(probe))File.Delete(probe);
            Process? probeParent=null;
            using(var session=Session.Create(Environment.ProcessPath!,Environment.ProcessPath!,"--probe "+Win32.Quote(probe),s=>report.Add(s),selfProbe:true))
            {
                probeParent=Process.GetProcessById((int)session.ParentId);
                Check(session.Wait()==0,"无游戏探针退出成功");
                Check(File.ReadAllText(probe).Trim()==session.ParentId.ToString(),"Windows 实际父进程 ID 与指定探针进程一致");
            }
            using(probeParent) {Check(probeParent!.WaitForExit(2000),"退出后仅清理本工具创建的挂起父进程");}
            File.Delete(probe);
            Locale.Language="en-US";Check(Locale.Choose("中文","English")=="English" && Locale.Text("请选择存在的 Endfield.exe。")=="Select an existing Endfield.exe.","English labels and diagnostics");
            var languageRoundTrip=JsonSerializer.Deserialize<Profile>(JsonSerializer.Serialize(new Profile{Language="en-US"}));Check(languageRoundTrip?.Language=="en-US","Language setting serialization");
            using(var ui=new MainForm(false))
            {
                ui.Show();ui.PreviewLanguage("en-US");ui.PreviewPage(1);ui.PreviewPage(2);ui.PreviewPage(0);Application.DoEvents();
                Check(ui.Text.StartsWith("Endfield Bridge",StringComparison.Ordinal),"English UI and repeated page navigation");
                ui.PreviewLanguage("zh-CN");Check(ui.Text.StartsWith("终末地启动桥接",StringComparison.Ordinal),"Switch back to Chinese UI");ui.Close();
            }
            Locale.Language="zh-CN";
            var fake=JsonNodeForTest(); Check(Integration.Efmi(fake)["game_launch"]?.ToString()=="MANUAL","读取 EFMI 接口结构");
            report.Add("未启动游戏、官方启动器或 Mod 注入；未改动真实 XXMI 或管理器配置。官方父进程创建仍需 UAC 后实测。");return 0;
        }
        catch(Exception ex){report.Add("FAIL "+ex);return 1;}
        finally {File.WriteAllLines(Path.Combine(Storage.Root,"self-test.txt"),report);}
    }
    static System.Text.Json.Nodes.JsonObject JsonNodeForTest()=>System.Text.Json.Nodes.JsonNode.Parse("{\"Importers\":{\"EFMI\":{\"Importer\":{\"game_launch\":\"MANUAL\"}}}}")!.AsObject();
}
