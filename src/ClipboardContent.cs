// SPDX-License-Identifier: GPL-3.0-only
using System.Text;
namespace Shixun {
public static class ClipboardContent {
 public static string Html(string fragment){const string template="Version:0.9\r\nStartHTML:{0:D10}\r\nEndHTML:{1:D10}\r\nStartFragment:{2:D10}\r\nEndFragment:{3:D10}\r\n";string head=string.Format(template,0,0,0,0),before="<!DOCTYPE html><html><body><!--StartFragment-->",after="<!--EndFragment--></body></html>";int start=Encoding.UTF8.GetByteCount(head),first=start+Encoding.UTF8.GetByteCount(before),last=first+Encoding.UTF8.GetByteCount(fragment),end=last+Encoding.UTF8.GetByteCount(after);return string.Format(template,start,end,first,last)+before+fragment+after;}
}
}
