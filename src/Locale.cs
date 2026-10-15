namespace EndfieldBridge;

internal static class Locale
{
    public static string Language { get; set; } = "zh-CN";
    public static bool English => Language == "en-US";
    public static string Choose(string zh, string en) => English ? en : zh;
    // Keep canonical Chinese diagnostics in memory; translate for display without changing paths or persisted settings.
    static readonly (string Zh, string En)[] Messages = new[]
    {
        ("XXMI 命令授权：签名验证通过（仅使用公钥核验）", "XXMI command authorization: signature verified using the public key"),
        ("XXMI 命令授权：尚未通过。首次启动核对路径后，在 XXMI 弹窗选择“保持”；“重置”会清空命令。", "XXMI command authorization: pending. Verify the command, then choose Keep in XXMI. Reset clears the command."),
        ("EFMI 游戏目录无效：已保留本工具的有效游戏路径，仅读取启动参数。适配时会修正 EFMI 目录。", "The EFMI game folder is invalid. The valid bridge game path was kept; only arguments were imported. Setup will repair the folder."),
        ("EFMI 的游戏目录无效。这里必须是包含 Endfield.exe 和 Endfield_Data 的实际目录，不能填写 .lnk 快捷方式。", "Invalid EFMI game folder. Select the folder containing Endfield.exe and Endfield_Data, not a shortcut."),
        ("没有找到 XXMI Launcher Config.json，请选择实际 XXMI Launcher.exe。", "XXMI Launcher Config.json was not found. Select the actual XXMI Launcher.exe."),
        ("EFMI 配置不完整，请使用“备份并适配 EFMI”修复目录、进程名和启动命令。", "EFMI setup is incomplete. Use Back up and set up EFMI to repair its folder, process name and launch command."),
        ("请移除前置命令中的桥接调用：桥接应放在自定义启动命令中，前置等待会阻塞加载流程。", "Remove the bridge from the pre-launch command. Use Custom Launch; a waiting pre-launch command blocks the loader."),
        ("EFMI 注入模式必须为 DIRECT 或 HOOK，当前未启用受支持的 Mod 加载方式。", "EFMI must use DIRECT or HOOK injection mode."),
        ("游戏路径必须包含 Endfield.exe 和同目录 Endfield_Data。", "The game path must contain Endfield.exe alongside Endfield_Data."),
        ("请先退出 XXMI，避免其覆盖配置。不会结束现有 XXMI 进程。", "Exit XXMI before editing its configuration. Existing processes will not be terminated."),
        ("请先退出终末地再修改适配配置。", "Exit Endfield before changing loader setup."),
        ("前置命令包含桥接调用，请先移除，以免阻塞 Mod 加载。", "Remove the bridge from the pre-launch command to avoid blocking Mod loading."),
        ("已适配，不覆盖首次备份。", "Setup is already complete. The original backup was kept."),
        ("当前 EFMI 注入模式不是 DIRECT 或 HOOK。", "EFMI injection mode is neither DIRECT nor HOOK."),
        ("XXMI 配置已被其他程序修改，已停止写入。", "Another program changed the XXMI configuration. No write was performed."),
        ("自定义命令已改变，停止恢复以保留你的新设置。", "The custom command changed. Restore was stopped to preserve your new settings."),
        ("配置已改变，停止恢复。", "The configuration changed. Restore was stopped."),
        ("签名有效，但签名组织不是预期的鹰角官方组织。", "The signature is valid, but the signer is not the expected Hypergryph organization."),
        ("父进程必须是实际存在的官方 Games.exe。", "The parent must be an existing official Games.exe."),
        ("。请用官方启动器修复或更新文件。", ". Repair or update this file with the official launcher."),
        ("官方文件签名验证未通过：", "Official file signature verification failed: "),
        ("请以管理员身份运行 XXMI 或通过桥接主界面启动 Mod，避免在加载过程中提权超时。", "Run XXMI as administrator, or start Mods from the bridge window, to avoid elevation timeouts during loading."),
        ("检测到终末地已在运行，请先退出，不会结束现有进程。", "Endfield is already running. Exit it first; the bridge will not terminate it."),
        ("未找到签名有效的官方 Games.exe。", "No official Games.exe with a valid signature was found. "),
        ("请手动选择官方文件。", "Select the official file manually."),
        ("请选择存在的 Endfield.exe。", "Select an existing Endfield.exe."),
        ("启动参数不能包含换行或空字符。", "Launch arguments cannot contain newlines or null characters."),
        ("桥接启动会话已在运行。", "A bridge launch session is already running."),
        ("不支持的命令行参数。", "Unsupported command-line arguments."),
        ("XXMI 配置不是 JSON 对象", "The XXMI configuration is not a JSON object"),
        ("XXMI 配置缺少 EFMI，未执行写入。", "EFMI is missing from the XXMI configuration. Nothing was written."),
        ("注意：EFMI 游戏目录无效，不能填写快捷方式或桥接程序。", "Warning: invalid EFMI game folder. Do not enter a shortcut or the bridge executable."),
        ("已读取 EFMI 游戏目录和启动参数。", "EFMI game path and launch arguments imported."),
        ("官方签名通过：", "Official signature verified: "),
        ("检测到官方文件：", "Official launcher found: "),
        ("创建挂起官方父进程失败", "Failed to create the suspended official parent"),
        ("查询进程属性空间失败", "Failed to query process attribute storage"),
        ("初始化进程属性失败", "Failed to initialize process attributes"),
        ("设置父进程属性失败", "Failed to set the parent process attribute"),
        ("游戏启动参数过长。", "Game arguments are too long."),
        ("创建目标进程失败", "Failed to create the target process"),
        ("等待目标退出失败", "Failed to wait for the target process"),
        ("读取退出码失败", "Failed to read the exit code"),
        ("警告：挂起父进程清理失败，PID ", "Warning: failed to clean up the suspended parent, PID "),
        ("；这不代表游戏认证或 Mod 加载成功。", "; this does not confirm game authentication or Mod loading."),
        ("已创建挂起官方父进程，PID ", "Suspended official parent created, PID "),
        ("目标进程已创建，PID ", "Target process created, PID "),
        ("游戏进程已退出，退出码 ", "Game process exited with code "),
        ("XXMI 程序不存在。", "XXMI executable was not found."),
        ("游戏已经运行，请先退出。", "The game is already running. Exit it first."),
        ("请退出已运行的 XXMI 后再使用桥接启动，避免请求被现有窗口接管。", "Exit the running XXMI instance before launching through the bridge."),
        ("无法创建 XXMI 进程", "Unable to create the XXMI process"),
        ("配置为空", "The profile is empty"),
        ("XXMI 配置：", "XXMI configuration: "), ("游戏目录：", "Game folder: "),
        ("启动模式：", "Launch mode: "), ("识别进程：", "Game process: "),
        ("；注入模式：", "; injection mode: "), ("自定义命令：", "Custom command: "),
        ("目标命令：", "Expected command: "), ("管理器入口：", "Manager entry: "),
        ("错误：", "Error: ")
    }.OrderByDescending(x => x.Item1.Length).ToArray();

    public static string Text(string value)
    {
        if (!English) return value;
        foreach (var (zh, en) in Messages) value = value.Replace(zh, en, StringComparison.Ordinal);
        return value;
    }
}
