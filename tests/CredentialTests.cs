// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using Shixun;
class CredentialTests {
 [STAThread]static int Main(){try{var profile=new RequestProfile{mode="bearer",token="fixture-token",method="POST",body="{\"token\":\"fixture-json\"}",fields=new List<RequestField>{new RequestField{place="header",name="X-Key",value="fixture-key"},new RequestField{place="query",name="signature",value="fixture-signature"}}};using(var dialog=new CredentialDialog(profile,"测试来源")){var masked=Walk(dialog).OfType<TextBox>().Count(t=>t.UseSystemPasswordChar);if(masked!=2)throw new Exception("Bearer token and password must be masked");var grid=Walk(dialog).OfType<DataGridView>().Single();if(grid.Rows.Count!=3)throw new Exception("multiple parameters missing");dialog.Shown+=delegate{var save=Walk(dialog).OfType<Button>().Single(b=>b.Text=="加密保存");save.PerformClick();};dialog.ShowDialog();if(dialog.Result==null||dialog.Result.token!="fixture-token"||dialog.Result.fields.Count!=2||dialog.Result.body!=profile.body)throw new Exception("profile round-trip failed");}Console.WriteLine("RESULT: Native credential dialog: masked secrets, multi-header/query rows and JSON profile round-trip passed");return 0;}catch(Exception e){Console.Error.WriteLine(e.Message);return 1;}}
 static IEnumerable<Control> Walk(Control parent){foreach(Control c in parent.Controls){yield return c;foreach(var child in Walk(c))yield return child;}}
}
