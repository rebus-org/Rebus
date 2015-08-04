@echo off

set /P VERSION=Enter version: 

echo Verify version: %VERSION%

set /P CONFIRM=Version correct? (y/n): 

if NOT [%CONFIRM%] == [y] goto :DONE

set /P DOPUSH=Push packages? (y/n): 

:GO
call "%~dp0\build.bat" %VERSION%

if %errorlevel% neq 0 (
	echo Error code returned from build: %errorlevel%
	goto :DONE
)

if NOT [%DOPUSH%] == [y] goto :DONE

git tag %VERSION%
echo Tagged commit with tag '%VERSION%'

git push
git push --tags

call "%~dp0\push.bat"

if %errorlevel% neq 0 (
  echo Error code returned from build: %errorlevel%
  goto :DONE
)

:DONE
