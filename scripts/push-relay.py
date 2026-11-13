"""Private push-fed RSS mirror. No arbitrary upstream fetches, no request logging."""
import hmac,json,os,re,threading,hashlib,time
from urllib.parse import urlsplit,parse_qs
from http.server import BaseHTTPRequestHandler,ThreadingHTTPServer
from pathlib import Path
from datetime import datetime,timezone
from xml.etree import ElementTree

DATA=Path(os.environ.get('RELAY_DATA','/var/lib/shixun-relay'))
LOCK=threading.Lock()
CHANGED=threading.Condition()
WAITERS=threading.BoundedSemaphore(128)
UIDS=set(filter(None,os.environ.get('RELAY_UIDS','').split(',')))
def validate(payload):
 if not isinstance(payload,dict) or not isinstance(payload.get('feeds'),dict) or not payload['feeds']: raise ValueError()
 stamp=datetime.fromisoformat(payload['updatedAt'].replace('Z','+00:00'))
 if stamp.tzinfo is None or stamp.timestamp()>datetime.now(timezone.utc).timestamp()+300: raise ValueError()
 for uid,feed in payload['feeds'].items():
  if uid not in UIDS or not isinstance(feed,dict) or not isinstance(feed.get('xml'),str): raise ValueError()
  datetime.fromisoformat(feed['updatedAt'].replace('Z','+00:00'))
  xml=feed['xml']
  if len(xml)>3000000 or '<!DOCTYPE' in xml.upper() or '<!ENTITY' in xml.upper(): raise ValueError()
  if ElementTree.fromstring(xml).tag!='rss': raise ValueError()
 return payload

class Handler(BaseHTTPRequestHandler):
 def log_message(self,*args): pass
 def reply(self,status,body=b'',kind='text/plain; charset=utf-8',updated=None):
  self.send_response(status); self.send_header('Content-Type',kind);self.send_header('Cache-Control','private, no-store');self.send_header('Content-Length',str(len(body)))
  if updated:self.send_header('X-Feed-Updated-At',updated)
  self.end_headers();self.wfile.write(body)
 def auth(self,name):
  expected=os.environ.get(name,'')
  return bool(expected) and hmac.compare_digest(self.headers.get('Authorization','').encode(),('Bearer '+expected).encode())
 def do_POST(self):
  if self.path!='/push':return self.reply(404)
  if not self.auth('RELAY_PUSH_TOKEN'):return self.reply(401)
  try:
   n=int(self.headers.get('Content-Length','0'))
   if not 0<n<=10000000:return self.reply(413)
   payload=validate(json.loads(self.rfile.read(n)))
   with LOCK:
    path=DATA/'snapshot.json'
    if path.exists() and json.loads(path.read_text())['updatedAt']>payload['updatedAt']:return self.reply(409)
    temp=DATA/'snapshot.tmp';temp.write_text(json.dumps(payload,ensure_ascii=False));temp.replace(path)
   with CHANGED: CHANGED.notify_all()
   return self.reply(204)
  except (ValueError,KeyError,TypeError,ElementTree.ParseError):return self.reply(400)
  except Exception:return self.reply(503)
 def do_GET(self):
  if not self.auth('READER_TOKEN'):return self.reply(401)
  parsed=urlsplit(self.path)
  event=re.fullmatch(r'/events/weibo/user/(\d+)',parsed.path)
  if event:
   if event[1] not in UIDS:return self.reply(404)
   if not WAITERS.acquire(blocking=False):return self.reply(429)
   try:
    since=parse_qs(parsed.query).get('since',[''])[0];deadline=time.monotonic()+45
    with CHANGED:
     while True:
      with LOCK: data=json.loads((DATA/'snapshot.json').read_text())
      feed=data['feeds'].get(event[1])
      if not feed:return self.reply(404)
      items=ElementTree.fromstring(feed['xml']).findall('./channel/item')
      version=hashlib.sha256(b''.join(ElementTree.tostring(x) for x in items)).hexdigest()
      remaining=deadline-time.monotonic()
      if since!=version or remaining<=0:break
      CHANGED.wait(remaining)
    return self.reply(200,json.dumps({'version':version}).encode(),'application/json')
   except Exception:return self.reply(503)
   finally:WAITERS.release()
  try:
   with LOCK: payload=json.loads((DATA/'snapshot.json').read_text())
  except Exception:return self.reply(503,b'Waiting for scheduled collection')
  if datetime.now(timezone.utc).timestamp()-datetime.fromisoformat(payload['updatedAt'].replace('Z','+00:00')).timestamp()>86400:return self.reply(503,b'Cached data expired')
  if self.path=='/status':
   body={'updatedAt':payload['updatedAt'],'feeds':[{'uid':uid,'updatedAt':feed['updatedAt'],'count':len(ElementTree.fromstring(feed['xml']).findall('./channel/item')),'error':'Upstream unavailable; retained last successful data' if feed.get('error') else None} for uid,feed in payload['feeds'].items()]}
   return self.reply(200,json.dumps(body).encode(),'application/json')
  match=re.fullmatch(r'/rss/weibo/user/(\d+)',self.path)
  if not match:return self.reply(404)
  feed=payload['feeds'].get(match[1])
  if not feed:return self.reply(404)
  return self.reply(200,feed['xml'].encode(),'application/xml; charset=utf-8',feed['updatedAt'])
if __name__=='__main__':
 DATA.mkdir(parents=True,exist_ok=True)
 ThreadingHTTPServer(('127.0.0.1',3184),Handler).serve_forever()
