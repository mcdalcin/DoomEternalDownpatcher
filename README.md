# DoomEternalDownpatcher
A Downpatcher for DOOM Eternal. Steam only.

In progress.

:) hello

## Adding a new version

There are two parts to adding a new version. 

### Adding in a filelist for the new version.

Add a filelist in the data/ folder. It should be named version_id.txt where version_id is the version number (e.g. 1.0, 1.1.1, etc).

regex that can be used to extract filenames for the filelist:
[a-zA-Z_]+\\\(\*\\w\*\\\)\*\\w\*\\.[^ ]*\w


### Adding in the manifestIds and size for the new version.

Each version may contain new manifest ids for each depot that we need to grab. Note that a manifest id may not have changed if there are no changes made to a particular depot.

To begin, edit data/versions.json and add in a new entry based on the previous version. For example, if we are adding in version 4.1, we would create a copy of 4.0's entry as such:

```json
    {
      "name": "4.0",
      "size": "432238592",
      "manifestIds": [
        "3615994377729244622",
        "8383133549687402109",
        "3131765218301983886",
        "6434174793668377623",
        "2932353992537037119",
        "379760896725657507",
        "4899404039317730890",
        "9073293208177965840"
      ]
    },
    {
      "name": "4.1",
      "size": "427006464",
      "manifestIds": [
        "874160952806574135",
        "2403401997055312635",
        "3131765218301983886",
        "6434174793668377623",
        "4833863637416404249",
        "379760896725657507",
        "4899404039317730890",
        "9073293208177965840"
      ]
    }
```

Each manifestId will remain the same as the previous version unless a depot contains changes. There are 8 steam depots for DOOM Eternal that we care about. In particular, depots  782332 through 782339. The manifestIds are in order starting with 782332.

To get the new manifestIds, go to https://steamdb.info/app/782330/patchnotes/. Click on the current patch (in this example, the December 9th patch here: https://steamdb.info/patchnotes/5922677). Once there, go through each depot change and replace the manifestId accordingly.

If done correctly, the code should now look like

```json
    {
      "name": "4.1",
      "size": "[4.1 SIZE]",
      "manifestIds": [
        "874160952806574135",
        "2403401997055312635",
        "3131765218301983886",
        "6434174793668377623",
        "4833863637416404249",
        "379760896725657507",
        "4899404039317730890",
        "9073293208177965840"
      ]
    }
```

Note only depot 782332, 782333, and 782336 contained changes, therefore we only changed the 1st, 2nd, and 5th manifestIds.

The final thing that will need updating is the size. This is the size of the DOOMEternalx64vk.exe executable located in your Steam folder. Right click it, go to properties, and copy and paste the size in bytes (NOT the size on disk).

```json
    {
      "name": "4.1",
      "size": "427006464",
      "manifestIds": [
        "874160952806574135",
        "2403401997055312635",
        "3131765218301983886",
        "6434174793668377623",
        "4833863637416404249",
        "379760896725657507",
        "4899404039317730890",
        "9073293208177965840"
      ]
    }
```

Finally, commit the changes and test out the downpatcher to ensure it is working for the new version.
