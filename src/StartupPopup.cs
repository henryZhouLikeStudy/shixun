// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Drawing;
using System.Windows.Forms;
namespace Shixun {
public sealed class StartupPopup:Form {
 readonly Timer dismiss=new Timer();
 protected override bool ShowWithoutActivation{get{return true;}}
 protected override CreateParams CreateParams{get{var p=base.CreateParams;p.ExStyle|=0x08000080;return p;}}
 public StartupPopup(int count,int seconds,Action open):this("有 "+count+" 篇未读内容","启动前的更新已整理好，打开阅读空间查看。",seconds,"打开阅读",open){}
 public StartupPopup(string title,string body,int seconds,string action,Action open){
  Text="拾讯 · 阅读提醒";FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;Width=410;Height=205;StartPosition=FormStartPosition.Manual;Font=new Font("Microsoft YaHei UI",10);BackColor=Color.FromArgb(247,249,245);ForeColor=Color.FromArgb(36,56,49);
  var brand=new Label{Text="◔  拾讯",ForeColor=Color.FromArgb(231,89,51),Font=new Font(Font,FontStyle.Bold),Left=18,Top=14,Width=130,Height=25};
  var close=Button("×",365,10,28,false);close.Click+=delegate{Close();};
  var heading=new Label{Text=title,Font=new Font("Microsoft YaHei UI",15,FontStyle.Bold),Left=18,Top=55,Width=375,Height=35};
  var note=new Label{Text=body,ForeColor=Color.FromArgb(111,129,120),Left=18,Top=99,Width=375,Height=42};
  var view=Button(action,265,154,125,true);view.Click+=delegate{Close();open();};
  Controls.AddRange(new Control[]{brand,close,heading,note,view});Paint+=delegate(object s,PaintEventArgs e){using(var pen=new Pen(Color.FromArgb(221,229,218)))e.Graphics.DrawRectangle(pen,0,0,Width-1,Height-1);};
  dismiss.Interval=Math.Max(3,Math.Min(60,seconds))*1000;dismiss.Tick+=delegate{Close();};Shown+=delegate{dismiss.Start();};FormClosed+=delegate{dismiss.Stop();dismiss.Dispose();};
 }
 Button Button(string text,int left,int top,int width,bool primary){var b=new Button{Text=text,Left=left,Top=top,Width=width,Height=32,FlatStyle=FlatStyle.Flat,BackColor=primary?Color.FromArgb(36,56,49):BackColor,ForeColor=primary?Color.White:ForeColor};b.FlatAppearance.BorderSize=0;return b;}
}
}
