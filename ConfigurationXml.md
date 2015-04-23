To use this feature, in extension package should be included a xml file with "extension\_settings.xml" name, which contain description of used setting by a plugin. This file no need to be installed anywhere, just included in package, this can be done making a separated group for this file and uncheck the "Default Selected"
A example file used by radiotime plugin:
```
<extension_settings>
	<disable_entry>
		<setting entryname="plugins" name="RadioTime" displayname="Plugin Enabled" defaultvalue="yes" type="string" listvalues="yes|no" displaylistvalues="Yes|No"/> 
	</disable_entry>

	<settings section="Configuration">
 		<setting name="user" entryname="radiotime" displayname="User Name" defaultvalue="" type="string" listvalues="" displaylistvalues=""/> 
		<setting name="password" entryname="radiotime" displayname="Password" defaultvalue="" type="string" listvalues="" displaylistvalues=""/> 
		<setting name="pluginname" entryname="radiotime" displayname="Plugin Name" defaultvalue="RadioTime" type="string" listvalues="" displaylistvalues=""/> 		
		<setting name="StartWithFastPreset" entryname="radiotime" displayname="Show fast preset on startup" defaultvalue="no" type="string" listvalues="yes|no" displaylistvalues="Yes|No"/> 		
	</settings>
	<settings section="Plugin">
			<setting entryname="plugins" name="RadioTime" displayname="Plugin Enabled" defaultvalue="yes" type="string" listvalues="yes|no" displaylistvalues="Yes|No"/> 
			<setting entryname="home" name="RadioTime" displayname="Listed in Home" defaultvalue="yes" type="string" listvalues="yes|no" displaylistvalues="Yes|No"/> 
			<setting entryname="myplugins" name="RadioTime" displayname="Listed in My Plugins" defaultvalue="no" type="string" listvalues="yes|no" displaylistvalues="Yes|No"/> 
	</settings>
</extension_settings>
```
Can be defined more section with node name settings
Settings are defined by node setting with more attributes :
  * name - setting name in Mediaportal.xml
  * entryname - entry name in Mediaportal.xml
  * displayname - the text which is displayed to this setting
  * defaultvalue - default value in no value exist in Mediaportal.xml
  * type - the setting type, now just string is supported
  * listvalues - (optional) values for settings, items are separated by |
  * displaylistvalues - should have same item number like listvalues