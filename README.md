# RemoteCommand

## Sample

Start Receiver
```powershell
Start-RemoteCommandReceiver
```

Run command in Remote terminal
```powershell
Invoke-RemoteCommand -Target "ws://targetHostOrIP:3000" -Command "ping localhost"
```
