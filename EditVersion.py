#!/usr/bin/python

import sys

with open('version', "w") as version:
    version.write(sys.argv[1])

    
    with open("src/UI/Dialogs/ChangelogWindow.cs", "r") as f:
        contents = f.readlines()
        mySeparator = "TEST"
        x = mySeparator.join(sys.argv[2])

    contents.insert(24, x)

    with open("src/UI/Dialogs/ChangelogWindow.cs", "w") as f:
        contents = "".join(contents)
        f.write(contents)
