# Alcatel-Lucent Installer's Toolkit
The application doesn't have a setup project, as the idea is to provide something that the user just loads to their PC and starts using it without installing.
When building a version for distribution (in release mode), you need to copy some of the contents from the /bin/release folder and add them to an archive file,
to be sent to the users. The following files should be added to the archive:
- ALE-InstallersToolkit.exe
- ALE-InstallersTookit.exe.config
- HtmlAgilityPack.dll
- Microsof.Bcl.AsyncInterfaces.dll
- RenciSshNet.dll
- oui.csv

The user will then unpack this archive in a folder on their PC and launch the ALE-InstallersToolkit.exe application.

The application currently has 2 branches, the main branch only has english language, and the i18n branch, where we can have multiple languages, for localization. 
When using an application built from the i18n branch, there is an additional menu option to select the language. Note that the language selection only affects the menu options 
and the messages presented to the user, but they don't translate the table contents and messages coming from the switch (also log files are not translated).
Language files are under Resources folder and must be called strings-<language_code>.xaml, where <language_code> is the code for the specific country and language.
For example: strings-enUS.xml, strings-frFR, etc.



