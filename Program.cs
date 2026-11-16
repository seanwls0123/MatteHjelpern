using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

static class Program
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr LoadLibrary(string path);
    [DllImport("user32.dll")]
    static extern bool SetProcessDPIAware();

    public static readonly string Root = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MatteHjelpern");
    public static readonly string Bin = Path.Combine(Root, "bin");
    public static readonly string Web = Path.Combine(Root, "web");
    public static readonly string Data = Path.Combine(Root, "data");

    static void Extract(string resName, string target, bool always)
    {
        Assembly asm = Assembly.GetExecutingAssembly();
        using (Stream s = asm.GetManifestResourceStream(resName))
        {
            if (s == null) return;
            if (!always && File.Exists(target) && new FileInfo(target).Length == s.Length) return;
            try
            {
                using (FileStream f = File.Create(target)) { s.CopyTo(f); }
            }
            catch (IOException) { }
        }
    }

    [STAThread]
    static int Main()
    {
        bool created;
        Mutex mutex = new Mutex(true, "MatteHjelpern_enkelt_vindu", out created);
        if (!created) return 0;

        Directory.CreateDirectory(Bin);
        Directory.CreateDirectory(Web);
        Directory.CreateDirectory(Data);

        Extract("Microsoft.Web.WebView2.Core.dll", Path.Combine(Bin, "Microsoft.Web.WebView2.Core.dll"), false);
        Extract("Microsoft.Web.WebView2.WinForms.dll", Path.Combine(Bin, "Microsoft.Web.WebView2.WinForms.dll"), false);
        Extract("WebView2Loader.dll", Path.Combine(Bin, "WebView2Loader.dll"), false);
        Extract("index.html", Path.Combine(Web, "index.html"), true);

        LoadLibrary(Path.Combine(Bin, "WebView2Loader.dll"));
        AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender, ResolveEventArgs e)
        {
            string name = new AssemblyName(e.Name).Name;
            string p = Path.Combine(Bin, name + ".dll");
            if (File.Exists(p)) return Assembly.LoadFrom(p);
            return null;
        };

        try { SetProcessDPIAware(); } catch (Exception) { }
        Run();
        GC.KeepAlive(mutex);
        return 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Run()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }
}

class MainForm : Form
{
    WebView2 web;

    public MainForm()
    {
        Text = "MatteHjelpern";
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(1280, 820);
        MinimumSize = new Size(800, 600);
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch (Exception) { }
        web = new WebView2();
        web.Dock = DockStyle.Fill;
        Controls.Add(web);
        Load += OnLoadAsync;
    }

    static void OpenExternal(string url)
    {
        if (url == null) return;
        if (!url.StartsWith("http://") && !url.StartsWith("https://")) return;
        try { Process.Start(url); } catch (Exception) { }
    }

    async void OnLoadAsync(object sender, EventArgs e)
    {
        try
        {
            CoreWebView2Environment env = await CoreWebView2Environment.CreateAsync(null, Program.Data);
            await web.EnsureCoreWebView2Async(env);
            CoreWebView2 core = web.CoreWebView2;
            core.Settings.AreDevToolsEnabled = false;
            core.Settings.IsStatusBarEnabled = false;
            core.SetVirtualHostNameToFolderMapping("mattehjelpern.local", Program.Web,
                CoreWebView2HostResourceAccessKind.Allow);
            core.NewWindowRequested += delegate(object s2, CoreWebView2NewWindowRequestedEventArgs a)
            {
                a.Handled = true;
                OpenExternal(a.Uri);
            };
            core.NavigationStarting += delegate(object s3, CoreWebView2NavigationStartingEventArgs a)
            {
                if (a.Uri != null && !a.Uri.StartsWith("https://mattehjelpern.local/"))
                {
                    a.Cancel = true;
                    OpenExternal(a.Uri);
                }
            };
            core.Navigate("https://mattehjelpern.local/index.html");
        }
        catch (Exception ex)
        {
            DialogResult r = MessageBox.Show(
                "MatteHjelpern trenger Microsoft Edge WebView2 for a vise vinduet.\n\n" +
                "Vil du apne nedlastingssiden na?\n\n(" + ex.Message + ")",
                "MatteHjelpern", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (r == DialogResult.Yes) OpenExternal("https://go.microsoft.com/fwlink/p/?LinkId=2124703");
            Close();
        }
    }
}
