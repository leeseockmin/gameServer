@echo off
echo === Proto 파일 자동 생성 ===
cd /d "%~dp0PacketToProtoGenerator"
dotnet run -c Release -- ^
  "%~dp0Share\Packet" ^
  "%~dp0FrogTailGameServer\Protos"
echo.
pause
