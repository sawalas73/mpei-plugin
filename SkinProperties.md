# Properties #
```
<id> is a extension guid with "-" replaced with "_" like 3664ae64_c5af_4084_baf0_7ecb57b89711
EX: #mpei.3664ae64_c5af_4084_baf0_7ecb57b89711.haveupdate
```
```
#mpei.<id>.haveupdate - true if the extension have new update
#mpei.<id>.updatelog
#mpei.<id>.updatedate
#mpei.<id>.updateversion
#mpei.<id>.installedversion
#mpei.<id>.isinstalled - true if the extension is installed
#mpei.<id>.name
#mpei.<id>.author

#mpei.newextensions - a list with newly released extensions
#mpei.updates - a list with available updates            
```

```
<control>
  <description><COMMAND>:<Extension ID></description>
  <type>button</type>
  <id>59</id>
  <posX>71</posX>
  <posY>499</posY>	
  <label>New version</label>
  <onright>50</onright>
  <onup>7</onup>
  <ondown>2</ondown> 
  <visible>string.equals(#mpei.<Extension ID>.haveupdate,true)</visible>
</control>
```

The command can be :
  * MPEIUPDATE - update to the latest version the specified extension
  * MPEISHOWCHANGELOG - show the changelog of the latest update version
  * MPEIINSTALL - install the specified extension
  * MPEIUNINSTALL - uninstall the specified extension
  * MPEICONFIGURE - configure the extension (if this is supported)

Ex:
Update a extension
```
<control>
  <description>MPEIUPDATE:3664ae64-c5af-4084-baf0-7ecb57b89711</description>
  <type>button</type>
  <id>59</id>
  <posX>71</posX>
  <posY>499</posY>	
  <label>New version</label>
  <onright>50</onright>
  <onup>7</onup>
  <ondown>2</ondown> 
  <visible>string.equals(#mpei.3664ae64_c5af_4084_baf0_7ecb57b89711.haveupdate,true)</visible>
</control>
```

Properties in main window

```
#MPE.Selected.installedversion
#MPE.Selected.isinstalled
#MPE.Selected.haveupdate
#MPE.Selected.updatelog
#MPE.Selected.updatedate
#MPE.Selected.updateversion

#MPE.Selected.Id
#MPE.Selected.Name
#MPE.Selected.Version
#MPE.Selected.Author
#MPE.Selected.Description
#MPE.Selected.VersionDescription
#MPE.Selected.Icon
```

Properties only for main site view
```
#MPE.Selected.JustAded
#MPE.Selected.Popular
#MPE.Selected.DeveloperPick
```