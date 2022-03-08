#!/usr/bin/python

import sys

with open('version', "w") as version:
    version.write(sys.argv[1])
