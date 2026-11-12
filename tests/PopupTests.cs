// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Shixun;
class PopupTests {
 [STAThread]static int Main(){try{bool marked=false;Exception failure=null;using(var popup=new Popup("虚构来源",new Article{title="虚构标题",summary="测试通知"},new Preferences{sound=false,images=false},()=>{},()=>{},()=>{},()=>marked=true,()=>Task.FromResult(new byte[0]))){
  popup.Shown+=delegate{try{var buttons=popup.Controls.OfType<Button>().Where(b=>b.Text!="×").OrderBy(b=>b.Left).ToArray();if(buttons.Length!=4||buttons.Last().Right>popup.ClientSize.Width)throw new Exception("notification actions clipped");for(int i=1;i<buttons.Length;i++)if(buttons[i].Left<buttons[i-1].Right)throw new Exception("notification actions overlap");buttons.Single(b=>b.Text=="标记已读").PerformClick();}catch(Exception e){failure=e;popup.Close();}};
  popup.ShowDialog();if(failure!=null)throw failure;if(!marked)throw new Exception("mark-read callback missing");}
  Console.WriteLine("PASS notification mark-read action and four-button layout");return 0;
 }catch(Exception e){Console.Error.WriteLine(e.Message);return 1;}}
}
