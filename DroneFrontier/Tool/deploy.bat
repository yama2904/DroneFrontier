@echo off
setlocal

set COPY_FROM=bin\
set COPY_TO=H:\Share\bin\

rmdir /S /Q %COPY_TO%
xcopy %COPY_FROM% %COPY_TO% /E /Y