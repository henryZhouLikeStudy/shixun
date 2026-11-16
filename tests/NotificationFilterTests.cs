using System;using Shixun;
class NotificationFilterTests{
 static void Check(bool ok){if(!ok)throw new Exception("Filter regression");}
 static void Main(){var p=new Preferences{notifyScope="filtered"};var a=new Article{source="a",category="c",ai=new AiResult{rating=5}};p.notifySources.Add("a");Check(Rules.Notify(p,a));p.notifyRatings.Add(4);p.notifyJoin="all";Check(!Rules.Notify(p,a));p.notifyRatings.Clear();p.notifyRatings.Add(5);Check(Rules.Notify(p,a));a.ai=null;Check(!Rules.Notify(p,a));p.notifyRatings.Clear();p.notifyRatings.Add(0);Check(Rules.Notify(p,a));p.notifySources.Clear();p.notifyRatings.Clear();Check(!Rules.Notify(p,a));p.notifyScope="all";Check(Rules.Notify(p,a));Check(Data.CompactTitle(new string('长',100)).Length==44);Console.WriteLine("PASS source/rating/unrated filter combinations and title length");}
}
