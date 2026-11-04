Unicode true
!include "MUI2.nsh"
Name "MonitorMate"
OutFile "..\artifacts\MonitorMate-1.0.0-windows-x64.exe"
InstallDir "$LOCALAPPDATA\Programs\MonitorMate"
RequestExecutionLevel user
Icon "..\src\MonitorMate\Assets\monitormate.ico"
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "..\LICENSE"
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "English"
Function .onInit
  StrCpy $INSTDIR "$LOCALAPPDATA\Programs\MonitorMate"
  System::Call 'kernel32::CreateMutexW(p 0, i 0, w "Local\MonitorMate.SingleInstance") p .r0 ?e'
  Pop $1
  StrCmp $1 183 running ready
  running:
    MessageBox MB_OK "Close MonitorMate normally before installing or updating." /SD IDOK
    SetErrorLevel 2
    Abort
  ready:
  System::Call 'kernel32::CloseHandle(p r0)'
FunctionEnd
Section "MonitorMate"
  SetOutPath "$INSTDIR"
  File /r "..\artifacts\windows\*"
  File "..\LICENSE"
  WriteUninstaller "$INSTDIR\Uninstall.exe"
  CreateShortcut "$SMPROGRAMS\MonitorMate.lnk" "$INSTDIR\MonitorMate.exe"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\MonitorMate" "DisplayName" "MonitorMate"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\MonitorMate" "DisplayVersion" "1.0.0"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\MonitorMate" "Publisher" "Benedictus RH"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\MonitorMate" "UninstallString" '$"$INSTDIR\Uninstall.exe$"'
SectionEnd
Function un.onInit
  StrCpy $INSTDIR "$LOCALAPPDATA\Programs\MonitorMate"
  System::Call 'kernel32::CreateMutexW(p 0, i 0, w "Local\MonitorMate.SingleInstance") p .r0 ?e'
  Pop $1
  StrCmp $1 183 running ready
  running:
    MessageBox MB_OK "Close MonitorMate normally before uninstalling." /SD IDOK
    SetErrorLevel 2
    Abort
  ready:
  System::Call 'kernel32::CloseHandle(p r0)'
FunctionEnd
Section "Uninstall"
  StrCpy $INSTDIR "$LOCALAPPDATA\Programs\MonitorMate"
  Delete "$SMPROGRAMS\MonitorMate.lnk"
  DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\MonitorMate"
  RMDir /r "$INSTDIR"
SectionEnd
