# Alcatel-Lucent Installer's Toolkit

## Disclaimer
© 2024 ALE USA Inc. All Rights Reserved. Permission to use, copy, modify, and distribute this source code and its documentation without a fee and without a signed license agreement is hereby granted, provided that the copyright notice, this paragraph, and the following two paragraphs appear in all copies, modifications, and distributions.
 
IN NO EVENT SHALL ALE USA INC. BE LIABLE TO ANY PARTY FOR DIRECT, INDIRECT, SPECIAL, INCIDENTAL, OR CONSEQUENTIAL DAMAGES, INCLUDING LOST PROFITS, ARISING OUT OF THE USE OF THIS SOURCE CODE AND ITS DOCUMENTATION, EVEN IF ALE USA INC. HAS BEEN ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 
ALE USA INC. SPECIFICALLY DISCLAIMS ANY WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE. THE SOURCE CODE AND ACCOMPANYING DOCUMENTATION, IF ANY, PROVIDED HEREUNDER IS PROVIDED “AS IS.” ALE USA INC. HAS NO OBLIGATION TO PROVICE MAINTENANCE, SUPPORT, UPDATES, ENHANCEMENTS, OR MODIFICATIONS.

## Description
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
For example: strings-enUS.xaml, strings-frFR.xaml, etc. Once the language file is added to this folder, the application will add the corresponding language code to 
the language menu option.

Note that log files and some configuration options are saved in the user's %AppData% folder (normally c:\users\<username>\AppData\Roaming) under folder
Alcatel-Lucent Enterprise/AOS Toolkit
Log files are under Log folder and configuration options are found in file app.cfg.
The app.cfg may have the following entries:
- theme (Dark/Light)
- language
- hash (corresponds to the password required for dangerous operations, such as reboot switch and factory default)
- switches (list of switch IP addresses the user has connected to, presented as a drop-down in the log-in screen)
There is a hardcoded password in class Data/Constants.cs (DEFAULT_PASS_CODE) that can be used for the first time the password is requested.
The user may change that, by selecting the Change Password option, in the password dialog. One the password is changed, the built in
password is no longer accepted. The user needs to delete the app.cfg file to reactivate the built in password.




