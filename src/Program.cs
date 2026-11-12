// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
namespace Shixun {
static class Program {
 [STAThread]static void Main(string[] args){Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);string data=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Shixun");int index=Array.IndexOf(args,"--data-dir");if(index>=0&&index+1<args.Length)data=Path.GetFullPath(args[index+1]);Directory.CreateDirectory(data);using(var mutex=new Mutex(false,"Local\\Shixun-"+Feeds.Hash(data).Substring(0,16))){if(!mutex.WaitOne(0)){File.WriteAllText(Path.Combine(data,"show.signal"),"");return;}try{Application.Run(new ReaderApp(data,args));}catch(Exception ex){MessageBox.Show(ex is SafeError?ex.Message:"启动失败，请确认安装 WebView2 Runtime，并检查数据目录访问权限。","拾讯",MessageBoxButtons.OK,MessageBoxIcon.Error);}finally{mutex.ReleaseMutex();}}}
}
}
