@echo off
echo === Proto AuTo Create ===
cd /d "%~dp0PacketToProtoGenerator"
dotnet run -c Release -- ^
  "%~dp0Share\Packet" ^
  "%~dp0FrogTailGameServer\Protos"
echo.
pause
