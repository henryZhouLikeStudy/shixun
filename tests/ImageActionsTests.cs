// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using Shixun;
class ImageActionsTests {
 delegate bool EnumProc(IntPtr window,IntPtr param);
 [DllImport("user32.dll")]static extern bool EnumWindows(EnumProc callback,IntPtr param);
 [DllImport("user32.dll")]static extern bool EnumChildWindows(IntPtr parent,EnumProc callback,IntPtr param);
 [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr window,out uint process);
 [DllImport("user32.dll")]static extern int GetDlgCtrlID(IntPtr window);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetClassName(IntPtr window,StringBuilder text,int size);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern IntPtr SendMessage(IntPtr window,int message,IntPtr wParam,string lParam);
 static ReaderApp app;static Timer poll;static string folder,path;static int stage,ticks;static bool busy,passed,saveClicked;static object previousClipboard;
 static object Field(string name){return typeof(ReaderApp).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(app);}
 static void SaveDialog(){EnumWindows(delegate(IntPtr window,IntPtr p){uint process;GetWindowThreadProcessId(window,out process);var name=new StringBuilder(128);GetClassName(window,name,128);if(process!=(uint)System.Diagnostics.Process.GetCurrentProcess().Id||name.ToString()!="#32770")return true;IntPtr edit=IntPtr.Zero,save=IntPtr.Zero;EnumChildWindows(window,delegate(IntPtr child,IntPtr ignored){var cls=new StringBuilder(128);GetClassName(child,cls,128);if(GetDlgCtrlID(child)==1001&&cls.ToString()=="Edit")edit=child;if(GetDlgCtrlID(child)==1&&cls.ToString()=="Button")save=child;return true;},IntPtr.Zero);if(edit!=IntPtr.Zero&&save!=IntPtr.Zero){SendMessage(edit,0x000C,IntPtr.Zero,path);SendMessage(save,0x00F5,IntPtr.Zero,null);saveClicked=true;}return true;},IntPtr.Zero);}
 static async void Tick(object sender,EventArgs args){
  if(++ticks>180){Console.Error.WriteLine("FAIL image workflow timeout at stage "+stage);poll.Stop();app.ExitThread();return;}
  if(stage==3&&!saveClicked)SaveDialog();
  if(busy)return;busy=true;
  try{var web=(WebView2)Field("web");if(web==null||web.CoreWebView2==null||!(bool)Field("ready"))return;
   if(stage==0){if(await web.CoreWebView2.ExecuteScriptAsync("state.articles.some(a=>a.id==='image-fixture')")!="true")return;await web.CoreWebView2.ExecuteScriptAsync("selected='image-fixture';render();showDetail()");stage=1;}
   else if(stage==1){if(await web.CoreWebView2.ExecuteScriptAsync("!!document.querySelector('.gallery img')?.naturalWidth")!="true")return;stage=2;await web.CoreWebView2.ExecuteScriptAsync("document.querySelector('.gallery img').dispatchEvent(new MouseEvent('contextmenu',{bubbles:true,cancelable:true,clientX:400,clientY:300}));document.querySelector('#contextMenu button').click()");}
   else if(stage==2){if(!Clipboard.ContainsImage())return;using(var image=Clipboard.GetImage()){if(image.Width!=12||image.Height!=9)throw new Exception("clipboard dimensions");}GC.Collect();GC.WaitForPendingFinalizers();if(!Clipboard.ContainsImage())throw new Exception("clipboard persistence");Clipboard.Clear();stage=4;await web.CoreWebView2.ExecuteScriptAsync("document.querySelector('.gallery img').click()");}
   else if(stage==4){if(await web.CoreWebView2.ExecuteScriptAsync("!!document.querySelector('#largeImage')?.naturalWidth")!="true")return;stage=5;await web.CoreWebView2.ExecuteScriptAsync("document.querySelector('#largeImage').dispatchEvent(new MouseEvent('contextmenu',{bubbles:true,cancelable:true,clientX:400,clientY:300}));if(!document.querySelector('#lightbox #contextMenu'))throw Error('menu outside modal');document.querySelector('#contextMenu button').click()");}
   else if(stage==5){if(!Clipboard.ContainsImage())return;using(var image=Clipboard.GetImage()){if(image.Width!=12||image.Height!=9)throw new Exception("zoom clipboard dimensions");}stage=3;await web.CoreWebView2.ExecuteScriptAsync("document.querySelector('#largeImage').dispatchEvent(new MouseEvent('contextmenu',{bubbles:true,cancelable:true,clientX:400,clientY:300}));document.querySelectorAll('#contextMenu button')[1].click()");}
   else if(stage==3&&File.Exists(path)){using(var image=Image.FromFile(path)){if(image.Width!=12||image.Height!=9)throw new Exception("saved dimensions");}var state=((Store)Field("store")).State;state.settings.notification="custom";state.settings.quiet=false;state.settings.notifyScope="filtered";state.settings.notifyKeywords="";state.settings.notifySources.Clear();state.settings.notifyCategories.Clear();state.settings.notifyRatings.Clear();typeof(ReaderApp).GetMethod("Notify",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(app,new object[]{new System.Collections.Generic.List<Article>{state.articles[0]},false});stage=6;}
   else if(stage==6){var popups=(System.Collections.Generic.List<Popup>)Field("popups");if(popups.Count!=1||!popups[0].Visible)return;passed=true;Console.WriteLine("PASS WebView2 image copy/save and actual native notification popup with empty filtered conditions");poll.Stop();app.ExitThread();}
  }catch(Exception error){Console.Error.WriteLine("FAIL native stage "+stage+": "+error.GetType().Name+" "+error.Message);poll.Stop();app.ExitThread();}finally{busy=false;}
 }
 [STAThread]static int Main(){folder=Path.Combine(Path.GetTempPath(),"ShixunImages-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);path=Path.Combine(folder,"saved-fixture.png");try{previousClipboard=Clipboard.GetDataObject();Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);byte[] png;using(var bitmap=new Bitmap(12,9))using(var stream=new MemoryStream()){using(var graphics=Graphics.FromImage(bitmap))graphics.Clear(Color.Coral);bitmap.Save(stream,ImageFormat.Png);png=stream.ToArray();}ImageCache.Get("image-fixture:0",()=>Task.FromResult(png)).GetAwaiter().GetResult();var store=new Store(folder);store.State.settings.checkUpdates=false;store.State.settings.notification="off";store.State.sources.Add(new Source{id="fixture",name="图片测试",url="https://example.com/feed",enabled=false});var article=new Article{id="image-fixture",source="fixture",title="测试图片",time=Data.Now(),firstSeen=Data.Now()};article.images.Add("https://example.com/fixture.png");store.State.articles.Add(article);store.Save();app=new ReaderApp(folder,new string[0]);if(Environment.GetEnvironmentVariable("GITHUB_ACTIONS")=="true"){typeof(ReaderApp).GetField("choosePicturePath",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(app,new Func<string,string>(extension=>{if(extension!="png")throw new Exception("unexpected image format");saveClicked=true;return path;}));Console.WriteLine("Hosted runner uses a deterministic destination for the native image save flow.");}poll=new Timer{Interval=200};poll.Tick+=Tick;poll.Start();Application.Run(app);return passed?0:1;}finally{if(previousClipboard!=null)try{Clipboard.SetDataObject(previousClipboard,true,5,100);}catch{}// WebView profile cleanup is left to the OS temp-folder lifecycle.
 }}
}
