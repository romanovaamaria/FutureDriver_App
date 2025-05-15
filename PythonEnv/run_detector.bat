::@echo off
::call "%~dp0venv\Scripts\activate.bat"
::python "%~dp0Scripts\traffic_sign_detector.py" %*
@echo off
call "%~dp0venv\Scripts\activate.bat"
"D:\conda\envs\tf310\python.exe" "%~dp0Scripts\traffic_sign_detector.py" %*