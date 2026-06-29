@echo off
echo Step 0: Cleaning old data...

:: Delete the PlantUML data file if it exists
if exist "full_project.puml" (
    del /f /q "full_project.puml"
    echo   - Deleted old full_project.puml
)

:: Delete Doxygen output folders (/s removes subdirectories, /q is quiet mode)
if exist "html" (
    rmdir /s /q "html"
    echo   - Deleted old html folder
)
if exist "latex" (
    rmdir /s /q "latex"
    echo   - Deleted old latex folder
)
if exist "rtf" (
    rmdir /s /q "rtf"
    echo   - Deleted old rtf folder
)

echo.
echo Step 1: Generating PlantUML data from C# scripts...
powershell -NoProfile -ExecutionPolicy Bypass -File "update_uml.ps1" -OutputName "full_project.puml"
powershell -NoProfile -ExecutionPolicy Bypass -File "update_uml.ps1" -OutputName "public_project.puml" -HidePrivate

echo.
echo Step 2: Running Doxygen...
doxygen Doxyfile

echo.
echo Done! Your documentation is fresh and updated.
pause