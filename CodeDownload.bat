@echo off
pushd %~dp0

rem # プロジェクト名
set ProjectName=RemoteCommand

rem # Code for Manifest
powershell -Command "Invoke-WebRequest -Uri \"https://raw.githubusercontent.com/tgiqfe/Manifest/master/Manifest/Program.cs\" -OutFile \".\Manifest\Program.cs\""
powershell -Command "Invoke-WebRequest -Uri \"https://raw.githubusercontent.com/tgiqfe/Manifest/master/Manifest/PSD1.cs\" -OutFile \".\Manifest\PSD1.cs\""
powershell -Command "Invoke-WebRequest -Uri \"https://raw.githubusercontent.com/tgiqfe/Manifest/master/Manifest/PSM1.cs\" -OutFile \".\Manifest\PSM1.cs\""
powershell -Command "Invoke-WebRequest -Uri \"https://raw.githubusercontent.com/tgiqfe/Manifest/master/Manifest/ExternalPackage.cs\" -OutFile \".\Manifest\ExternalPackage.cs\""

powershell -Command "(Get-Content \".\Manifest\Program.cs\") -replace \"`n\",\"`r`n\" | Out-File \".\Manifest\Program.cs\" -Encoding UTF8"
powershell -Command "(Get-Content \".\Manifest\PSD1.cs\") -replace \"`n\",\"`r`n\" | Out-File \".\Manifest\PSD1.cs\" -Encoding UTF8"
powershell -Command "(Get-Content \".\Manifest\PSM1.cs\") -replace \"`n\",\"`r`n\" | Out-File \".\Manifest\PSM1.cs\" -Encoding UTF8"
powershell -Command "(Get-Content \".\Manifest\ExternalPackage.cs\") -replace \"`n\",\"`r`n\" | Out-File \".\Manifest\ExternalPackage.cs\" -Encoding UTF8"

rem # Code for ScriptLanguage
echo ScriptLanguage Code Update

set ProjectName=WebSocketConnect

set DefaultLanguageSettingCS=%ProjectName%\Language\DefaultLanguageSetting.cs 
set LanguageCS=%ProjectName%\Language\Language.cs 

set beforeNamespace=namespace ScriptLanguage
set afterNamespace=namespace %ProjectName%.ScriptLanguage

powershell -Command "Invoke-WebRequest -Uri \"https://raw.githubusercontent.com/tgiqfe/ScriptLanguage/master/ScriptLanguage/Language/DefaultLanguageSetting.cs\" -OutFile \".\%DefaultLanguageSettingCS%\""
powershell -Command "Invoke-WebRequest -Uri \"https://raw.githubusercontent.com/tgiqfe/ScriptLanguage/master/ScriptLanguage/Language/Language.cs\" -OutFile \".\%LanguageCS%\""

powershell -Command "(Get-Content \".\%DefaultLanguageSettingCS%\") -replace \"`n\",\"`r`n\" | Out-File \".\%DefaultLanguageSettingCS%\" -Encoding UTF8"
powershell -Command "(Get-Content \".\%LanguageCS%\") -replace \"`n\",\"`r`n\" | Out-File \".\%LanguageCS%\" -Encoding UTF8"

powershell -Command "(Get-Content \".\%DefaultLanguageSettingCS%\") -replace \"%beforeNamespace%\",\"%afterNamespace%\" | Out-File \".\%DefaultLanguageSettingCS%\" -Encoding UTF8"
powershell -Command "(Get-Content \".\%LanguageCS%\") -replace \"%beforeNamespace%\",\"%afterNamespace%\" | Out-File \".\%LanguageCS%\" -Encoding UTF8"


