// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 EndfieldBridge contributors.
// Modified 2026-10-15: AGPL distribution and license UI; see NOTICE.md for upstream credits.
// Launch-core approach and quoting structure adapted after reviewing endfield-nolauncher
// (fcda770), whose upstream is Better-Endfield (514f6d6). Both declare AGPL-3.0.
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace EndfieldBridge;

internal static class Win32
{
    [StructLayout(LayoutKind.Sequential)] internal struct Startup
    {
        public uint cb; public IntPtr reserved,desktop,title;
        public uint x,y,cx,cy,xCount,yCount,fill,flags;
        public ushort show,reserved2; public IntPtr reservedPtr,input,output,error;
    }
    [StructLayout(LayoutKind.Sequential)] internal struct StartupEx { public Startup si; public IntPtr attributes; }
    [StructLayout(LayoutKind.Sequential)] internal struct ProcessInfo { public IntPtr process,thread; public uint pid,tid; }
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] internal struct TrustFile { public uint size; [MarshalAs(UnmanagedType.LPWStr)] public string path; public IntPtr file,subject; }
    [StructLayout(LayoutKind.Sequential)] internal struct TrustData
    {
        public uint size; public IntPtr policy,sip; public uint ui,revocation,choice; public IntPtr info; public uint action; public IntPtr state,url; public uint flags,context;
    }
    [StructLayout(LayoutKind.Sequential)] internal struct BasicInfo { public IntPtr reserved1,peb,reserved2a,reserved2b,pid,parent; }
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] [return:MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CreateProcessW(string app,StringBuilder command,IntPtr pa,IntPtr ta,bool inherit,uint flags,IntPtr environment,string cwd,ref StartupEx startup,out ProcessInfo pi);
    [DllImport("kernel32.dll",SetLastError=true)] [return:MarshalAs(UnmanagedType.Bool)] internal static extern bool InitializeProcThreadAttributeList(IntPtr list,int count,uint flags,ref nuint size);
    [DllImport("kernel32.dll",SetLastError=true)] [return:MarshalAs(UnmanagedType.Bool)] internal static extern bool UpdateProcThreadAttribute(IntPtr list,uint flags,nuint attribute,IntPtr value,nuint bytes,IntPtr previous,IntPtr returned);
    [DllImport("kernel32.dll")] internal static extern void DeleteProcThreadAttributeList(IntPtr list);
    [DllImport("kernel32.dll",SetLastError=true)] [return:MarshalAs(UnmanagedType.Bool)] internal static extern bool CloseHandle(IntPtr handle);
    [DllImport("kernel32.dll",SetLastError=true)] [return:MarshalAs(UnmanagedType.Bool)] internal static extern bool TerminateProcess(IntPtr process,uint code);
    [DllImport("kernel32.dll",SetLastError=true)] internal static extern uint WaitForSingleObject(IntPtr handle,uint ms);
    [DllImport("kernel32.dll",SetLastError=true)] [return:MarshalAs(UnmanagedType.Bool)] internal static extern bool GetExitCodeProcess(IntPtr handle,out uint code);
    [DllImport("wintrust.dll",ExactSpelling=true,CharSet=CharSet.Unicode)] internal static extern int WinVerifyTrust(IntPtr window,ref Guid action,ref TrustData data);
    [DllImport("ntdll.dll")] internal static extern int NtQueryInformationProcess(IntPtr process,int kind,out BasicInfo info,int size,out int returned);
    internal static Exception Error(string operation) => new Win32Exception(Marshal.GetLastWin32Error(),operation);
    internal static string Quote(string value)
    {
        var b=new StringBuilder("\""); int slash=0;
        foreach(char c in value)
        {
            if(c=='\\') {slash++;continue;}
            if(c=='"') b.Append('\\',slash*2+1).Append('"');
            else b.Append('\\',slash).Append(c);
            slash=0;
        }
        return b.Append('\\',slash*2).Append('"').ToString();
    }
    internal static string Verify(string path)
    {
        if(!File.Exists(path)||!Path.GetFileName(path).Equals("Games.exe",StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("父进程必须是实际存在的官方 Games.exe。");
        // Keep a read-only, non-write-sharing handle open across validation and process creation in Session.
        var file=new TrustFile {size=(uint)Marshal.SizeOf<TrustFile>(),path=path};
        IntPtr mem=Marshal.AllocHGlobal(Marshal.SizeOf<TrustFile>()); Marshal.StructureToPtr(file,mem,false);
        Guid policy=new("00AAC56B-CD44-11D0-8CC2-00C04FC295EE");
        var data=new TrustData {size=(uint)Marshal.SizeOf<TrustData>(),ui=2,choice=1,info=mem,action=1,flags=0x1000};
        try
        {
            int result=WinVerifyTrust(new IntPtr(-1),ref policy,ref data);
            if(result!=0) throw new InvalidDataException($"官方文件签名验证未通过：0x{unchecked((uint)result):X8}。请用官方启动器修复或更新文件。");
#pragma warning disable SYSLIB0057
            using var cert=new X509Certificate2(X509Certificate.CreateFromSignedFile(path));
#pragma warning restore SYSLIB0057
            string dn=cert.SubjectName.Decode(X500DistinguishedNameFlags.UseNewLines);
            const string org="Shanghai Hypergryph Network Technology Co., Ltd.";
            bool found=dn.Split('\n').Select(s=>s.Trim()).Any(s=>s.StartsWith("O=",StringComparison.Ordinal)&&s[2..].Trim('"').Equals(org,StringComparison.OrdinalIgnoreCase));
            if(!found) throw new InvalidDataException("签名有效，但签名组织不是预期的鹰角官方组织。");
            return cert.GetNameInfo(X509NameType.SimpleName,false);
        }
        finally {data.action=2;WinVerifyTrust(new IntPtr(-1),ref policy,ref data);Marshal.DestroyStructure<TrustFile>(mem);Marshal.FreeHGlobal(mem);}
    }
}

internal sealed class OwnedHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public OwnedHandle(IntPtr value):base(true)=>SetHandle(value);
    protected override bool ReleaseHandle()=>Win32.CloseHandle(handle);
}

internal sealed class Session : IDisposable
{
    private OwnedHandle? parent;
    private OwnedHandle? game;
    public uint ParentId {get;private set;}
    public uint GameId {get;private set;}
    public static Session Create(string parentExe,string target,string args,Action<string> log,bool selfProbe=false)
    {
        var session=new Session();
        try
        {
            using var held=File.Open(parentExe,FileMode.Open,FileAccess.Read,FileShare.Read);
                        if(selfProbe)
            {
                if(!string.Equals(parentExe,Environment.ProcessPath,StringComparison.OrdinalIgnoreCase)||!string.Equals(target,Environment.ProcessPath,StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("探针仅允许本程序自身。");
                log("自检：使用本工具自己的挂起进程测试 Windows 父进程 API，不运行官方程序或游戏。");
            }
            else {string signer=Win32.Verify(parentExe);log("官方签名通过："+signer);}
            var startup=new Win32.StartupEx {si=new Win32.Startup {cb=(uint)Marshal.SizeOf<Win32.Startup>()}};
            if(!Win32.CreateProcessW(parentExe,new StringBuilder(Win32.Quote(parentExe)),IntPtr.Zero,IntPtr.Zero,false,0x4|0x08000000,IntPtr.Zero,Path.GetDirectoryName(parentExe)!,ref startup,out var pi)) throw Win32.Error("创建挂起官方父进程失败");
            session.parent=new(pi.process); using(var thread=new OwnedHandle(pi.thread)) { }
            session.ParentId=pi.pid; log($"已创建挂起{(selfProbe?"探针":"官方")}父进程，PID {pi.pid}");
            nuint bytes=0;Win32.InitializeProcThreadAttributeList(IntPtr.Zero,1,0,ref bytes);
            if(bytes==0) throw Win32.Error("查询进程属性空间失败");
            IntPtr list=Marshal.AllocHGlobal(checked((int)bytes)),value=Marshal.AllocHGlobal(IntPtr.Size);bool initialized=false;
            try
            {
                if(!Win32.InitializeProcThreadAttributeList(list,1,0,ref bytes)) throw Win32.Error("初始化进程属性失败");initialized=true;
                Marshal.WriteIntPtr(value,session.parent.DangerousGetHandle());
                if(!Win32.UpdateProcThreadAttribute(list,0,0x00020000,value,(nuint)IntPtr.Size,IntPtr.Zero,IntPtr.Zero)) throw Win32.Error("设置父进程属性失败");
                var extended=new Win32.StartupEx {si=new Win32.Startup{cb=(uint)Marshal.SizeOf<Win32.StartupEx>()},attributes=list};
                var command=new StringBuilder(Win32.Quote(target)+(string.IsNullOrWhiteSpace(args)?"":" "+args));
                if(command.Length>=32767) throw new InvalidDataException("游戏启动参数过长。");
                if(!Win32.CreateProcessW(target,command,IntPtr.Zero,IntPtr.Zero,false,0x00080000,IntPtr.Zero,Path.GetDirectoryName(target)!,ref extended,out var child)) throw Win32.Error("创建目标进程失败");
                session.game=new(child.process);using(var thread=new OwnedHandle(child.thread)) { }
                session.GameId=child.pid;log($"目标进程已创建，PID {child.pid}；这不代表游戏认证或 Mod 加载成功。");
            }
            finally {if(initialized) Win32.DeleteProcThreadAttributeList(list);Marshal.FreeHGlobal(value);Marshal.FreeHGlobal(list);}
            return session;
        }
        catch {session.Dispose();throw;}
    }
    public int Wait()
    {
        if(game==null) throw new InvalidOperationException();
        uint status=Win32.WaitForSingleObject(game.DangerousGetHandle(),uint.MaxValue);
        if(status!=0) throw Win32.Error("等待目标退出失败");
        if(!Win32.GetExitCodeProcess(game.DangerousGetHandle(),out uint code)) throw Win32.Error("读取退出码失败");
        return unchecked((int)code);
    }
    public void Dispose()
    {
        // Only terminate the suspended helper we own. Never terminate the user's game or other launchers.
        if(parent!=null&&!parent.IsClosed)
        {
            if(Win32.WaitForSingleObject(parent.DangerousGetHandle(),0)==258)
            {
                if(!Win32.TerminateProcess(parent.DangerousGetHandle(),0)) Storage.Log("警告：挂起父进程清理失败，PID "+ParentId);
                else Win32.WaitForSingleObject(parent.DangerousGetHandle(),5000);
            }
            parent.Dispose();parent=null;
        }
        game?.Dispose();game=null;
    }
}
