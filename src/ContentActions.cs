// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace Shixun {
public sealed partial class ReaderApp {
 async Task<object> ContentAction(string action,Dictionary<string,object> data){
  if(action=="copyImageData"){string encoded=S(data,"data");if(encoded.Length==0||encoded.Length>1800000)throw new SafeError("复制图片过大，请保存原图。");byte[] png;try{png=Convert.FromBase64String(encoded);}catch{throw new SafeError("图片格式无效。");}if(png.Length<8||png[0]!=0x89||png[1]!=0x50)throw new SafeError("图片格式无效。");using(var stream=new MemoryStream(png))using(var image=Image.FromStream(stream)){if((long)image.Width*image.Height>6000000)throw new SafeError("图片尺寸过大。");using(var bitmap=new Bitmap(image))Clipboard.SetImage(bitmap);}return true;}
  if(action=="copyRich"){string text=S(data,"text"),html=S(data,"html");if(text.Length==0||text.Length>200000||html.Length>500000)throw new SafeError("复制内容为空或过长。");var content=new DataObject();content.SetData(DataFormats.UnicodeText,text);if(html.Length>0)content.SetData(DataFormats.Html,ClipboardContent.Html(html));Clipboard.SetDataObject(content,true,5,100);return true;}
  int index;if(!Int32.TryParse(S(data,"index"),out index))throw new SafeError("图片不存在。");byte[] bytes=await ImageBytes(S(data,"id"),index);string operation=S(data,"operation");
  if(operation=="copy"){try{using(var stream=new MemoryStream(bytes))using(var image=Image.FromStream(stream)){if((long)image.Width*image.Height>40000000)throw new SafeError("图片尺寸过大，请保存原图。");using(var bitmap=new Bitmap(image))Clipboard.SetImage(bitmap);}}catch{throw new SafeError("图片暂时无法复制，请尝试保存原图。");}return true;}
  if(operation=="save"){string extension=bytes[0]==0x89?"png":bytes[0]==0xff?"jpg":bytes[0]==0x47?"gif":"webp";using(var dialog=new SaveFileDialog{Filter="原始图片|*."+extension,FileName="拾讯-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+"."+extension,DefaultExt=extension}){if(dialog.ShowDialog(window)!=DialogResult.OK)return false;File.WriteAllBytes(dialog.FileName,bytes);return true;}}
  throw new SafeError("图片操作无效。");
 }
 async Task<byte[]> AiImageBytes(string id,int index){byte[] bytes=await ImageBytes(id,index);return await Task.Run(()=>{using(var stream=new MemoryStream(bytes))using(var image=Image.FromStream(stream)){if((long)image.Width*image.Height>40000000)throw new SafeError("图片尺寸过大。");var encoder=ImageCodecInfo.GetImageEncoders().First(c=>c.MimeType=="image/jpeg");foreach(int edge in new[]{1200,960,768,600,480})foreach(long quality in new long[]{70,50,35}){double ratio=Math.Min(1,edge/(double)Math.Max(image.Width,image.Height));using(var bitmap=new Bitmap(Math.Max(1,(int)(image.Width*ratio)),Math.Max(1,(int)(image.Height*ratio))))using(var graphics=Graphics.FromImage(bitmap))using(var output=new MemoryStream())using(var parameters=new EncoderParameters(1)){graphics.Clear(Color.White);graphics.DrawImage(image,0,0,bitmap.Width,bitmap.Height);parameters.Param[0]=new EncoderParameter(System.Drawing.Imaging.Encoder.Quality,quality);bitmap.Save(output,encoder,parameters);if(output.Length<=85000)return output.ToArray();}}throw new SafeError("图片压缩后仍过大。");}});}
}
}
