// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Drawing;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
namespace Shixun {
public sealed class Popup:Form {
 [DllImport("shell32.dll")]static extern int SHQueryUserNotificationState(out int state);
 public static bool Allowed(){try{int state;return SHQueryUserNotificationState(out state)==0&&state==5;}catch{return false;}}
 protected override bool ShowWithoutActivation{get{return true;}}
 protected override CreateParams CreateParams{get{var p=base.CreateParams;p.ExStyle|=0x08000080;return p;}}
 readonly Timer timer=new Timer();readonly PictureBox picture=new PictureBox();
 public Popup(string source,Article article,int total,Preferences prefs,Action open,Action original,Action later,Action markRead,Func<Task<byte[]>> image){FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;BackColor=Color.FromArgb(247,249,245);Width=410;Height=prefs.images&&article.images.Count>0?228:205;Font=new Font("Microsoft YaHei UI",10);Padding=new Padding(17);var header=new Label{Text=source+(total>1?" · "+total+" 条更新":" · 刚刚"),ForeColor=Color.Gray,Left=17,Top=14,Width=350,Height=23};var close=new Button{Text="×",FlatStyle=FlatStyle.Flat,Left=375,Top=8,Width=27,Height=27};close.FlatAppearance.BorderSize=0;close.Click+=delegate{Close();};var title=new Label{Text=Data.Limit(article.title,65),Font=new Font(Font,FontStyle.Bold),Left=17,Top=48,Width=prefs.images&&article.images.Count>0?278:375,Height=54};var body=new Label{Text=Data.Limit(article.summary,75),ForeColor=Color.DimGray,Left=17,Top=108,Width=prefs.images&&article.images.Count>0?278:375,Height=46};picture.SetBounds(304,49,88,88);picture.SizeMode=PictureBoxSizeMode.Zoom;var view=new Button{Text="查看详情",Left=17,Top=Height-48,Width=88,Height=30};var link=new Button{Text="打开原文",Left=111,Top=Height-48,Width=88,Height=30};var snooze=new Button{Text="15分钟后",Left=205,Top=Height-48,Width=88,Height=30};var read=new Button{Text="标记已读",Left=299,Top=Height-48,Width=94,Height=30};read.Click+=delegate{markRead();Close();};view.Click+=delegate{open();Close();};link.Click+=delegate{original();Close();};snooze.Click+=delegate{later();Close();};Controls.AddRange(new Control[]{header,close,title,body,picture,view,link,snooze,read});timer.Interval=Math.Max(3,prefs.duration)*1000;timer.Tick+=delegate{Close();};foreach(Control c in Controls){c.MouseEnter+=delegate{timer.Stop();};c.MouseLeave+=delegate{if(!ClientRectangle.Contains(PointToClient(Cursor.Position)))timer.Start();};}MouseEnter+=delegate{timer.Stop();};MouseLeave+=delegate{if(!ClientRectangle.Contains(PointToClient(Cursor.Position)))timer.Start();};Shown+=async delegate{timer.Start();if(prefs.sound)System.Media.SystemSounds.Asterisk.Play();if(prefs.images&&article.images.Count>0)try{byte[] bytes=await image();if(!IsDisposed&&bytes!=null){using(var stream=new MemoryStream(bytes))using(var decoded=Image.FromStream(stream))picture.Image=new Bitmap(decoded);}}catch{}};FormClosed+=delegate{timer.Dispose();if(picture.Image!=null)picture.Image.Dispose();};}
}
}
