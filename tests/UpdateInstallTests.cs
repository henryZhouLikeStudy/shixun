// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.IO;
class UpdateInstallTests {
 static int Main(){string folder=Path.Combine(Path.GetTempPath(),"ShixunUpdateTest-"+Guid.NewGuid().ToString("N"));try{
  string root=Path.Combine(folder,"installed"),stage=Path.Combine(folder,"payload");Directory.CreateDirectory(root);Directory.CreateDirectory(stage);Directory.CreateDirectory(Path.Combine(root,"ui"));Directory.CreateDirectory(Path.Combine(stage,"ui"));
  File.WriteAllText(Path.Combine(root,"Shixun.exe"),"old");File.WriteAllText(Path.Combine(root,"subscriptions.private.json"),"private-fixture");File.WriteAllText(Path.Combine(root,"ui/client.js"),"old-ui");File.WriteAllText(Path.Combine(stage,"Shixun.exe"),"new");File.WriteAllText(Path.Combine(stage,"ui/client.js"),"new-ui");
  UpdateInstaller.Apply(root,stage,Path.Combine(folder,"backup"));if(File.ReadAllText(Path.Combine(root,"Shixun.exe"))!="new"||File.ReadAllText(Path.Combine(root,"subscriptions.private.json"))!="private-fixture")throw new Exception("update or private data preservation failed");
  File.WriteAllText(Path.Combine(stage,"Shixun.exe"),"failed-update");File.WriteAllText(Path.Combine(stage,"subscriptions.private.json"),"bad-payload");bool failed=false;try{UpdateInstaller.Apply(root,stage,Path.Combine(folder,"rollback"));}catch{failed=true;}
  if(!failed||File.ReadAllText(Path.Combine(root,"Shixun.exe"))!="new"||File.ReadAllText(Path.Combine(root,"subscriptions.private.json"))!="private-fixture")throw new Exception("rollback or private payload protection failed");
  Console.WriteLine("PASS updater file replacement, private seed preservation, invalid payload rejection and rollback");return 0;
 }catch(Exception ex){Console.Error.WriteLine(ex.Message);return 1;}finally{if(Directory.Exists(folder))Directory.Delete(folder,true);}}
}
