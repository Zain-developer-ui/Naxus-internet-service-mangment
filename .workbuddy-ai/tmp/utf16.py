import re, os
src = r"C:/Users/Zain Ansari/Downloads/EProject_Question/NEXUS SERVICE MARKETING SYSTEM"
out = r"C:/Users/Zain Ansari/source/repos/Zain-developer-ui/Naxus-internet-service-mangment/.workbuddy-ai/tmp"
for name in os.listdir(src):
    if not name.lower().endswith('.doc'): continue
    d = open(os.path.join(src,name),'rb').read()
    # utf-16le runs of printable + whitespace
    pat = rb'(?:(?:[\x20-\x7e]\x00)|(?:\x0d\x00)|(?:\x0a\x00)|(?:\t\x00)){8,}'
    runs = re.findall(pat, d)
    chunks=[]
    for r in runs:
        t = r.decode('utf-16le')
        chunks.append(t)
    text = "\n---RUN---\n".join(chunks)
    fn = re.sub(r'[^A-Za-z0-9]+','_', name)
    open(os.path.join(out, fn+'.u16.txt'),'w',encoding='utf-8').write(text)
    print(name, "chars:", len(text), "runs:", len(runs))
