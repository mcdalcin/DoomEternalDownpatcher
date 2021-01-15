
# DOOM Eternal Steam Downpatcher
A Downpatcher for DOOM Eternal. Steam only.

![Preview](https://github.com/mcdalcin/DoomEternalDownpatcher/blob/master/Images/mainPreview.PNG?raw=true)


Made with love from Xiae.

  ![XiaeKawaii](https://github.com/mcdalcin/DoomEternalDownpatcher/blob/master/Images/kawaii.jpg?raw=true)

## INSTRUCTIONS

Instructions included in the application under the Help link in the top right.

![Instructions](https://github.com/mcdalcin/DoomEternalDownpatcher/blob/master/Images/instructions.PNG?raw=true)

## BELOW IS FOR DEVELOPERS ONLY

## ADDING SUPPORT FOR NEW VERSION

When adding in a new version, two things must be done.

 - Adding in the filelist.
 - Modifying data/versions.json with the manifest and size information.

### ADDING IN THE FILELIST

The FileListGenerator makes this part very easy. Simply get the patch notes page from [SteamDB](https://steamdb.info/app/782330/patchnotes/) and enter it into the FileListGenerator. Follow the directions carefully. An example usage with the December 9th 2020 patch is below.

![FileListGenerator](https://github.com/mcdalcin/DoomEternalDownpatcher/blob/master/Images/fileListGenerator.PNG?raw=true)

Rename the filelist.txt created to new_version_name.txt and add it to the data folder in this repository.

### MODIFY VERSIONS.JSON

**Eventually, this part will be automated, but for now we must do it manually.**

Each version will contain a new manifest id for each depot that has changed.

##### CREATE AN ENTRY FOR THE NEW VERSION
To begin, edit data/versions.json and add in a new entry based on the previous version.  For example, if we are adding in version 4.1, begin by creating a copy of 4.0.

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
##### ADJUST MANIFESTIDS FOR THE NEW VERSION

The manifestIds correspond to the 8 steam depots for DOOM Eternal starting with depot 782332 and ending with depot 782339. Note that a manifest id will only change for the depot if it contained changes from the previous patch.

To get the new manifestIds, go to https://steamdb.info/app/782330/patchnotes/. Click on the current patch (in this example, the December 9th patch corresponds to version 4.1). Once there, go through each depot change and replace the manifestId accordingly. 

In our example, the depots 782332, 782333, and 782336 contain changes and therefore their manifest ids must be updated. This corresponds to the first, second, and fifth manifest ids.

If done correctly, the code should now look like

```json
...
      ]
    },
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
##### UPDATING THE SIZE FOR THE NEW VERSION
The final thing that will need updating is the size. This is the size of the DOOMEternalx64vk.exe executable located in your Steam folder. Right click it, go to properties, and copy and paste the size in bytes (NOT the size on disk).

```json
...
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

#### FINALLY, COMMIT THE NEW VERSIONS.JSON AND TEST THE DOWNPATCHER!
