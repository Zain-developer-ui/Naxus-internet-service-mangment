import re, os
src = r"C:/Users/Zain Ansari/Downloads/EProject_Question/NEXUS SERVICE MARKETING SYSTEM"
out = r"C:/Users/Zain Ansari/source/repos/Zain-developer-ui/Naxus-internet-service-mangment/.workbuddy-ai/tmp"
for name in os.listdir(src):
    if not name.lower().endswith('.doc'): continue
    d = open(os.path.join(src,name),'rb').read()
    parts=[]
    for m in re.finditer(rb'[\x20-\x7e]{3,}', d):
        parts.append((m.start(), m.group().decode('latin-1')))
    for m in re.finditer(rb'(?:[\x20-\x7e]\x00){3,}', d):
        parts.append((m.start(), m.group().decode('utf-16le')))
    parts.sort(key=lambda x: x[0])
    seen=set(); lines=[]
    for pos,t in parts:
        t = t.strip()
        if len(t)<3: continue
        ok = sum(c.isalnum() or c in ' .,:;()/$%-\'"' for c in t)
        if ok/len(t) < 0.75: continue
        if not re.search(r'[A-Za-z]{2,}', t): continue
        if t in seen: continue
        seen.add(t)
        lines.append(t)
    fn = re.sub(r'[^A-Za-z0-9]+','_', name)
    open(os.path.join(out, fn+'.clean.txt'),'w',encoding='utf-8').write("\n".join(lines))
    print(name, "clean lines:", len(lines))
