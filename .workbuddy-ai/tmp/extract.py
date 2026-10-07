import re, os
src = r"C:/Users/Zain Ansari/Downloads/EProject_Question/NEXUS SERVICE MARKETING SYSTEM"
out = r"C:/Users/Zain Ansari/source/repos/Zain-developer-ui/Naxus-internet-service-mangment/.workbuddy-ai/tmp"
for name in os.listdir(src):
    if not name.lower().endswith('.doc'): continue
    d = open(os.path.join(src,name),'rb').read()
    runs = re.findall(rb'[\x20-\x7e\r\n\t\x0b\x0c]{4,}', d)
    text = "\n".join(r.decode('latin-1') for r in runs)
    # try utf-16le extraction too
    u16 = re.findall(rb'(?:[\x20-\x7e]\x00){5,}', d)
    utext = "\n".join(r.decode('utf-16le') for r in u16)
    fn = name.replace(' ','_').replace('.','_')
    open(os.path.join(out, fn+'.ascii.txt'),'w',encoding='utf-8').write(text)
    open(os.path.join(out, fn+'.utf16.txt'),'w',encoding='utf-8').write(utext)
    print(name, "ascii chars:", len(text), "utf16 chars:", len(utext))
