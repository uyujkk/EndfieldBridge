using System.Diagnostics;

namespace EndfieldBridge;

internal sealed class MainForm : Form
{
    readonly TextBox game = new(), parent = new(), xxmi = new(), arguments = new();
    readonly CheckBox mods = new() { AutoSize = true };
    readonly RichTextBox log = new(), integration = new(), risk = new();
    readonly Label status = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
    readonly Button launch = new();
    readonly Button language = new() { Width = 138, Height = 34, FlatStyle = FlatStyle.Flat };
    readonly List<Action> bindings = new();
    readonly List<Control> lockable = new();
    readonly List<(DateTime Time, string Zh, string? En)> entries = new();
    readonly Panel workspace = new() { Dock = DockStyle.Fill };
    readonly Button[] navigation = new Button[3];
    readonly Control[] pages = new Control[3];
    readonly ToolTip hints = new();
    bool busy, initializing = true;
    int page;
    readonly bool auto;
    string statusZh = "就绪 · 尚未启动游戏", statusEn = "Ready · Game not started";
    static readonly Color Bg = Color.FromArgb(20, 25, 33), Surface = Color.FromArgb(29, 36, 47), Field = Color.FromArgb(37, 45, 58), Ink = Color.FromArgb(230, 235, 243), Muted = Color.FromArgb(161, 174, 192), Accent = Color.FromArgb(238, 196, 99), Line = Color.FromArgb(57, 69, 87);

    public MainForm(bool autoLaunch)
    {
        auto = autoLaunch;
        ClientSize = new Size(1100, 850); MinimumSize = new Size(1000, 820); StartPosition = FormStartPosition.CenterScreen;
        BackColor = Bg; ForeColor = Ink; Font = new Font("Microsoft YaHei UI", 9); AutoScaleMode = AutoScaleMode.None;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24, 16, 24, 16), ColumnCount = 1, RowCount = 6 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 88)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 302)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        Controls.Add(layout);

        var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Margin = Padding.Empty };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 162));
        var heading = new Panel { Dock = DockStyle.Fill };
        var title = new Label { Text = "ENDFIELD BRIDGE", Font = new Font("Segoe UI", 20, FontStyle.Bold), ForeColor = Accent, AutoSize = true, Location = new Point(0, 0) };
        var subtitle = new Label { AutoSize = true, ForeColor = Muted, Location = new Point(2, 54) }; Bind(subtitle, "实验版 0.3.0  /  Integrated Mod Manager 启动扩展", "Experimental 0.3.0  /  Integrated Mod Manager launch companion");
        heading.Controls.Add(title); heading.Controls.Add(subtitle); header.Controls.Add(heading);
                 language.Anchor = AnchorStyles.Right; language.BackColor = Surface; language.ForeColor = Ink; header.Controls.Add(language); layout.Controls.Add(header);
        language.Click += (_, _) => ChangeLanguage(); lockable.Add(language);

        var banner = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(58, 44, 29), Padding = new Padding(12, 8, 12, 8), RowCount = 2, ColumnCount = 2, Margin = new Padding(0, 4, 0, 10) };
        banner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); banner.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 146));
        banner.RowStyles.Add(new RowStyle(SizeType.Absolute, 25)); banner.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var riskTitle = new Label { Dock = DockStyle.Fill, ForeColor = Accent, Font = new Font(Font, FontStyle.Bold) }; Bind(riskTitle, "账号风险 · 本工具未经游戏官方认可", "ACCOUNT RISK · Unofficial software"); banner.Controls.Add(riskTitle, 0, 0);
        var riskSummary = new Label { Dock = DockStyle.Fill, ForeColor = Ink }; Bind(riskSummary, "第三方启动与 Mod 注入可能导致账号处罚或封禁。\r\n启动成功、签名通过均不代表官方认可或账号安全。", "Third-party launching and Mod injection may lead to account penalties or bans.\r\nNo official approval or account-safety guarantee."); banner.Controls.Add(riskSummary, 0, 1);
        var details = Button("查看风险", "Read risks", () => { ShowPage(2); return Task.CompletedTask; }); banner.Controls.Add(details, 1, 0); banner.SetRowSpan(details, 2); layout.Controls.Add(banner);

        var settings = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Surface, Padding = new Padding(14, 10, 14, 4), RowCount = 6, ColumnCount = 1, Margin = Padding.Empty };
        foreach (int h in new[] { 48, 48, 48, 48, 36, 52 }) settings.RowStyles.Add(new RowStyle(SizeType.Absolute, h));
        settings.Controls.Add(PathRow("游戏程序", "Game executable", game, "Endfield.exe"));
        settings.Controls.Add(PathRow("官方父进程", "Official launcher", parent, "Games.exe", true));
        settings.Controls.Add(PathRow("XXMI 启动器", "XXMI launcher", xxmi, "XXMI Launcher.exe"));
        settings.Controls.Add(PathRow("游戏参数", "Game arguments", arguments, null));
        Bind(mods, "使用 XXMI / EFMI 加载 Mod（需先适配）", "Load Mods with XXMI / EFMI (setup required)"); mods.Anchor = AnchorStyles.Left; settings.Controls.Add(mods); lockable.Add(mods);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Margin = Padding.Empty };
        actions.Controls.Add(Button("检测配置", "Validate", ValidateAsync));
        actions.Controls.Add(Button("读取 EFMI", "Import EFMI", () => { var p = Current(); Write(Integration.Import(p)); Set(p); RefreshIntegration(); return Task.CompletedTask; }));
        actions.Controls.Add(Button("保存设置", "Save settings", () => { Save(); Write("设置已保存。", "Settings saved."); return Task.CompletedTask; }));
        actions.Controls.Add(Button("打开日志", "Open log", () => { var p = Path.Combine(Storage.Root, "bridge.log"); if (!File.Exists(p)) Storage.Log("Log opened"); Process.Start(new ProcessStartInfo("notepad.exe", Win32.Quote(p)) { UseShellExecute = true }); return Task.CompletedTask; }));
        settings.Controls.Add(actions); layout.Controls.Add(settings);

        var nav = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Padding = new Padding(0, 6, 0, 0), Margin = Padding.Empty };
        string[] z = ["运行日志", "加载器适配", "风险与使用边界"], e = ["Activity", "Loader setup", "Risks / limits"];
        for (int i = 0; i < 3; i++) { int index = i; navigation[i] = Button(z[i], e[i], () => { ShowPage(index); return Task.CompletedTask; }); nav.Controls.Add(navigation[i]); }
        layout.Controls.Add(nav);
        StyleOutput(log); StyleOutput(integration); StyleOutput(risk);
        var integrationPage = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Margin = Padding.Empty };
        integrationPage.RowStyles.Add(new RowStyle(SizeType.Absolute, 52)); integrationPage.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); integrationPage.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        var help = new Label { Dock = DockStyle.Fill, ForeColor = Muted, AutoEllipsis = true }; Bind(help, "适配会备份 XXMI 配置并只修改 EFMI 启动字段。首次授权请核对路径，再在 XXMI 中选“保持”。\r\n管理器入口只负责启动，不会修改管理器的仓库设置。", "Setup backs up XXMI and changes only EFMI launch fields. For first-time authorization, verify the command and choose Keep in XXMI.\r\nThe manager entry does not edit your manager repositories."); integrationPage.Controls.Add(help); integrationPage.Controls.Add(integration);
        var tools = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Margin = Padding.Empty };
        tools.Controls.Add(Button("备份并适配 EFMI", "Back up + set up", () => { var p = Current(); Write(Integration.Import(p)); Set(p); Save(); if (Integration.IsConfigured(p)) { Write("适配已完成，原始备份未覆盖。", "Setup is already complete; the original backup was kept."); return Task.CompletedTask; } var b = Integration.Apply(p); mods.Checked = true; Save(); RefreshIntegration(); Write("备份位置：" + b, "Backup: " + b); Write(Integration.AuthorizationStatus(p)); return Task.CompletedTask; }));
        tools.Controls.Add(Button("恢复 EFMI", "Restore EFMI", () => { Integration.Restore(); mods.Checked = false; Save(); RefreshIntegration(); Write("已恢复原启动字段，其他设置保留。", "Original launch fields restored; other settings kept."); return Task.CompletedTask; }));
        tools.Controls.Add(Button("复制管理器入口", "Copy manager entry", () => { Integration.Export(); Clipboard.SetText(Path.Combine(Storage.Root, "StartFromModManager.cmd")); Write("入口已复制。在管理器的终末地仓库中，将启动器路径选择为此 CMD。", "Entry copied. Select this CMD as the launcher path in your manager's Endfield repository."); return Task.CompletedTask; })); integrationPage.Controls.Add(tools);
        pages[0] = log; pages[1] = integrationPage; pages[2] = risk; workspace.Controls.AddRange(pages); layout.Controls.Add(workspace);

        var footer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(0, 12, 0, 0), Margin = Padding.Empty };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210)); status.ForeColor = Accent; footer.Controls.Add(status);
        launch.Dock = DockStyle.Fill; launch.FlatStyle = FlatStyle.Flat; launch.FlatAppearance.BorderSize = 0; launch.BackColor = Accent; launch.ForeColor = Bg; launch.Font = new Font(Font.FontFamily, 12, FontStyle.Bold); Bind(launch, "启动游戏", "Launch game"); launch.Click += async (_, _) => await LaunchAsync(); footer.Controls.Add(launch); layout.Controls.Add(footer);
        try { var p = Storage.Load(); Locale.Language = p.Language == "en-US" ? "en-US" : "zh-CN"; Set(p); }
        catch (Exception ex) { Write("配置读取失败：" + ex.Message, "Failed to read profile: " + Locale.Text(ex.Message)); launch.Enabled = false; }
        initializing = false; ApplyLanguage(); ShowPage(0);
        Write("仅验证配置，不会自动启动游戏。账号处罚风险无法由技术测试排除。", "Configuration checks do not start the game. Technical tests cannot rule out account penalties.");
        FormClosing += (_, ev) => { if (busy) { ev.Cancel = true; Write("会话进行中，请先正常退出游戏。", "A session is active. Exit the game normally before closing."); } };
        Shown += async (_, _) => { if (auto) await LaunchAsync(); };
    }

    void Bind(Control c, string zh, string en) => bindings.Add(() => c.Text = Locale.Choose(zh, en));
    Button Button(string zh, string en, Func<Task> action)
    {
        var b = new Button { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(102, 34), FlatStyle = FlatStyle.Flat, BackColor = Surface, ForeColor = Ink, Padding = new Padding(12, 3, 12, 3), Margin = new Padding(0, 4, 10, 4) };
        b.FlatAppearance.BorderColor = Line; Bind(b, zh, en); lockable.Add(b);
        b.Click += async (_, _) => { try { await action(); } catch (Exception ex) { Write(ex.Message); MessageBox.Show(this, Locale.Text(ex.Message), Locale.Choose("操作未完成", "Operation incomplete"), MessageBoxButtons.OK, MessageBoxIcon.Warning); } }; return b;
    }
    Control PathRow(string zh, string en, TextBox box, string? file, bool optional = false)
    {
        var row = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Margin = Padding.Empty };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 164)); row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, file == null ? 0 : 96));
        var label = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }; Bind(label, zh, en); row.Controls.Add(label);
        box.Dock = DockStyle.Fill; box.BackColor = Field; box.ForeColor = Ink; box.BorderStyle = BorderStyle.FixedSingle; box.Margin = new Padding(0, 9, 8, 8); row.Controls.Add(box); lockable.Add(box);
        if (optional) bindings.Add(() => box.PlaceholderText = Locale.Choose("留空自动查找官方 Games.exe", "Leave empty to find official Games.exe"));
        if (file != null) { var pick = Button("浏览", "Browse", () => { using var dlg = new OpenFileDialog { Filter = $"{file}|{file}|EXE|*.exe", CheckFileExists = true }; if (dlg.ShowDialog(this) == DialogResult.OK) { box.Text = dlg.FileName; RefreshIntegration(); } return Task.CompletedTask; }); pick.AutoSize = false; pick.MinimumSize = Size.Empty; pick.Dock = DockStyle.Fill; pick.Margin = new Padding(0, 7, 0, 7); pick.Padding = Padding.Empty; row.Controls.Add(pick); }
        return row;
    }
    static void StyleOutput(RichTextBox box) { box.Dock = DockStyle.Fill; box.ReadOnly = true; box.BackColor = Surface; box.ForeColor = Ink; box.BorderStyle = BorderStyle.None; box.Font = new Font("Consolas", 9); box.ScrollBars = RichTextBoxScrollBars.Vertical; box.DetectUrls = false; box.Margin = new Padding(8); }
    Profile Current() => new() { Game = game.Text.Trim(), Parent = parent.Text.Trim(), Xxmi = xxmi.Text.Trim(), GameArguments = arguments.Text.Trim(), WithMods = mods.Checked, Language = Locale.Language };
    void Set(Profile p) { game.Text = p.Game; parent.Text = p.Parent; xxmi.Text = p.Xxmi; arguments.Text = p.GameArguments; mods.Checked = p.WithMods; }
    void Save() { var p = Current(); Program.ValidateGame(p); Storage.Save(p); Integration.Export(); }
    void ChangeLanguage()
    {
        if (initializing) return;
        Locale.Language = Locale.English ? "zh-CN" : "en-US";
        ApplyLanguage();
        try { Storage.Save(Current()); } catch (Exception ex) { Write(ex.Message); }
    }
    void ApplyLanguage()
    {
        Text = Locale.Choose("终末地启动桥接", "Endfield Bridge") + " · Integrated Mod Manager";
        language.Text = Locale.English ? "简体中文" : "English"; foreach (var bind in bindings) bind(); RefreshIntegration(); risk.Text = RiskText(); RenderLog(); UpdateStatus();
        hints.SetToolTip(status, status.Text);
    }
    void RefreshIntegration() { if(string.IsNullOrWhiteSpace(xxmi.Text)){integration.Text=Locale.Choose("先选择 XXMI Launcher.exe，再读取配置或进行适配。","Select XXMI Launcher.exe to import its settings or set up integration.");return;} try { integration.Text = Locale.Text(Integration.Inspect(Current()) + "\r\n\r\n管理器入口：" + Path.Combine(Storage.Root, "StartFromModManager.cmd")); } catch (Exception ex) { integration.Text = Locale.Text(ex.Message); } }
    void ShowPage(int value)
    {
        page = value;
        for (int i = 0; i < pages.Length; i++) pages[i].Visible = i == value;
        pages[value].BringToFront();
        for (int i = 0; i < navigation.Length; i++) { navigation[i].BackColor = i == value ? Field : Bg; navigation[i].ForeColor = i == value ? Accent : Muted; }
    }
    internal void PreviewPage(int value) => ShowPage(value);
    internal void PreviewLanguage(string value) { initializing = true; Locale.Language = value == "en-US" ? "en-US" : "zh-CN"; initializing = false; ApplyLanguage(); }
    void Write(string zh, string? en = null)
    {
        if (IsDisposed) return; if (InvokeRequired) { BeginInvoke(() => Write(zh, en)); return; }
        entries.Add((DateTime.Now, zh, en)); if (entries.Count > 500) entries.RemoveAt(0); RenderLog(); Storage.Log(Locale.English && en != null ? en : Locale.Text(zh));
    }
    void RenderLog() { log.Text = string.Join("\r\n", entries.Select(x => $"[{x.Time:HH:mm:ss}] {(Locale.English && x.En != null ? x.En : Locale.Text(x.Zh))}")); log.SelectionStart = log.TextLength; log.ScrollToCaret(); }
    void Status(string zh, string en) { statusZh = zh; statusEn = en; UpdateStatus(); }
    void UpdateStatus() { status.Text = Locale.Choose(statusZh, statusEn); hints.SetToolTip(status, status.Text); }
    void Busy(bool value) { busy = value; launch.Enabled = !value; foreach (var c in lockable) c.Enabled = !value; }
    async Task ValidateAsync()
    {
        var p = Current(); Busy(true); Status("正在检查配置和签名…", "Checking configuration and signatures…");
        try
        {
            await Task.Run(() => { Program.ValidateGame(p); Program.ResolveParent(p, s => Write(s)); if (p.WithMods) { if (!File.Exists(p.Xxmi)) throw new FileNotFoundException("XXMI 程序不存在。"); Integration.ValidateMode(p); } });
            Write("配置和官方文件签名检查通过。", "Configuration and official file signature checks passed.");
            if (p.WithMods) { Write(Integration.AuthorizationStatus(p)); bool valid = Integration.IsCommandAuthorized(p); Status(valid ? "配置与授权通过 · 不代表账号安全" : "配置就绪 · 等待 XXMI 授权", valid ? "Configuration authorized · Account safety not guaranteed" : "Configuration ready · XXMI authorization pending"); }
            else Status("配置通过 · 尚未启动游戏", "Configuration checked · Game not started");
        }
        catch (Exception ex) { Write(ex.Message); Status("检查未通过，详见日志", "Validation failed · See activity"); }
        finally { Busy(false); }
    }
    async Task LaunchAsync()
    {
        if (busy) return;
        try
        {
            Save(); var p = Current();
            if (!Program.IsAdmin()) { Program.Elevate("--launch"); Close(); return; }
            if (Process.GetProcessesByName("Endfield").Length > 0) throw new InvalidOperationException("游戏已经运行，请先退出。");
            if (p.WithMods)
            {
                Integration.ValidateMode(p); Write(Integration.Import(p, strict: true)); Storage.Save(p);
                if (!File.Exists(p.Xxmi)) throw new FileNotFoundException("XXMI 程序不存在。");
                if (Process.GetProcessesByName("XXMI Launcher").Length > 0) throw new InvalidOperationException("请退出已运行的 XXMI 后再使用桥接启动，避免请求被现有窗口接管。");
                Busy(true); Status("已请求 XXMI / EFMI 启动", "XXMI / EFMI launch requested");
                Write("调用 XXMI --nogui --xxmi EFMI。首次弹窗核对路径后选“保持”；“重置”会清空命令。", "Calling XXMI --nogui --xxmi EFMI. Verify the command and choose Keep if prompted; Reset clears it.");
                using var proc = Process.Start(new ProcessStartInfo(p.Xxmi, "--nogui --xxmi EFMI") { UseShellExecute = false, WorkingDirectory = Path.GetDirectoryName(Integration.ConfigPath(p.Xxmi))! }) ?? throw new IOException("无法创建 XXMI 进程");
                await proc.WaitForExitAsync(); Write("XXMI 已结束，退出码 " + proc.ExitCode + "。这不能证明 Mod 成功或账号安全。", "XXMI exited with code " + proc.ExitCode + ". This does not confirm Mod loading or account safety."); Status("XXMI 调用结束 · 请核对实际游戏", "XXMI request finished · Check the game");
            }
            else { Busy(true); Status("游戏会话进行中", "Game session active"); int code = await Task.Run(() => Program.LaunchGame(p, s => Write(s))); Status("游戏已退出 · " + code, "Game exited · " + code); }
        }
        catch (Exception ex) { Write(ex.Message); Status("启动未完成，详见日志", "Launch incomplete · See activity"); MessageBox.Show(this, Locale.Text(ex.Message), Locale.Choose("启动未完成", "Launch incomplete"), MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        finally { Busy(false); }
    }
    static string RiskText() => Locale.Choose(
        "账号与使用风险\r\n\r\n本项目未经鹰角、GRYPHLINE 或 XXMI 官方认可，不保证不会封号。\r\n父进程桥接改变正常启动链；通过 XXMI / EFMI 加载 Mod 涉及第三方 DLL 注入。即使只修改外观，也不能据此认定获得官方许可。\r\n\r\n签名验证、启动成功和注入成功都是技术结果，不是游戏运营方的授权。我们无法给出可靠封号概率，也无法判断账号是否被标记。\r\n\r\n如果不能接受额外账号风险，请使用官方启动器且不加载第三方 Mod。\r\n\r\n权限与数据\r\n启动游戏可能要求管理员权限。配置、日志和备份保存在程序目录，日志可能包含本机路径。桥接不读取账号密码，不上传日志，不读取 XXMI 私钥。\r\n\r\n恢复只影响本地配置，不会撤销运营方可能已有的记录。正常退出会清理本工具创建的挂起父进程；强制终止可能留下该进程。\r\n\r\n完整说明及官方规则链接见仓库 RISK.zh-CN.md。",
        "ACCOUNT AND USAGE RISKS\r\n\r\nThis project is not endorsed by Hypergryph, GRYPHLINE, or XXMI. There is no ban-free guarantee.\r\nParent-process bridging changes the normal launch chain. XXMI / EFMI Mods involve third-party DLL injection. Cosmetic-only changes are not proof of official permission.\r\n\r\nSignature checks, successful launches and injection logs are technical results, not authorization from the game operator. We cannot estimate a reliable ban probability or tell whether an account has been flagged.\r\n\r\nIf you cannot accept additional account risk, use the official launcher without third-party Mods.\r\n\r\nPRIVILEGES AND DATA\r\nLaunching may require administrator privileges. Configuration, logs and backups stay in the application folder; logs may contain local paths. The bridge does not read account passwords, upload logs, or read XXMI private keys.\r\n\r\nRestoring local configuration does not undo records the operator may already hold. Normal shutdown cleans up the suspended parent created by this tool; forced termination may leave it running.\r\n\r\nSee RISK.md in the repository for full details and official policy links.");
}
