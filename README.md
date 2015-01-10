# StyleCop.AnalyzersAnalyzer

The tool used to generate http://stylecop.pdelvo.com/

## Usage

The easiest way to use this is by having a script that will update a local copy of the project and then call this tool to generate some
html. Below you see the script I use. You just need to have this script in a directory with a directory called "Analyzer" with the executables of this tool
and NuGet.exe (https://nuget.codeplex.com/releases/view/58939) in the directory of the script. If the script does have permissions 
it will clone StyleCopAnalyzers into a directory with the same name (or update it if it exists), generate a directory web where it will put
a index.html.

```cmd
@echo off

IF exist StyleCopAnalyzers (cd StyleCopAnalyzers && git fetch origin master && git reset --hard FETCH_HEAD && git clean -df ) ELSE ( git clone https://github.com/DotNetAnalyzers/StyleCopAnalyzers.git && cd StyleCopAnalyzers)
set EnableNuGetPackageRestore=true

..\NuGet.exe restore
cd ..
mkdir web
Analyzer\StyleCop.AnalyzersAnalyzer.exe StyleCopAnalyzers > web\index.html
```
