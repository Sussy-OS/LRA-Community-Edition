#!/usr/bin/python

import sys

with open('version', "w") as version:
    version.write(sys.argv[1])

with open('src/Program.cs','r',encoding='utf-8') as file:
    data = file.readlines()
    
data[38] = "        public static string Version = "sys.argv[1]";\n"

with open('src/Program.cs', 'w', encoding='utf-8') as file:
    file.writelines(data)
