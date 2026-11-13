// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Drawing;
using System.Windows.Forms;
namespace Shixun {
public sealed class StartupPopup:Form {
 readonly Timer dismiss=new Timer();
 protected override bool ShowWithoutActivation{get{return true;}}
 public StartupPopup(int count,int seconds,Action open){Text="拾讯 · 启动摘要";FormBorderStyle=FormBorderStyle.FixedToolWindow;ShowInTaskbar=false;TopMost=true;Width=390;Height=140;Font=new Font("Microsoft YaHei UI",10);BackColor=Color.FromArgb(247,249,245);Controls.Add(new Label{Text="启动前有 "+count+" 篇未读内容",AutoSize=true,Left=18,Top=18});var view=new Button{Text="打开阅读",Left=235,Top=58,Width=115};view.Click+=delegate{Close();open();};Controls.Add(view);dismiss.Interval=Math.Max(3,Math.Min(60,seconds))*1000;dismiss.Tick+=delegate{Close();};Shown+=delegate{dismiss.Start();};FormClosed+=delegate{dismiss.Stop();dismiss.Dispose();};}
}
}
