@echo off
"C:\Users\kanri\Desktop\tools\����\asshuku\packages\Microsoft.Net.Compilers.3.3.0\tools\csc.exe" /noconfig /nologo /unsafe+ /nowarn:1701,1702,2008 /nostdlib+ /platform:x86 /errorreport:prompt -parallel+ /warn:4 /errorendlocation /preferreduilang:ja-JP /highentropyva+ -lib:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\v3.5",C:\\Users\\kanri\\Desktop\\tools\\����\\asshuku\\packages\\OpenCvSharp-AnyCPU.2.4.10.20170306\\lib\\net45\\ /reference:Microsoft.CSharp.dll /reference:mscorlib.dll /reference:OpenCvSharp.Blob.dll /reference:OpenCvSharp.CPlusPlus.dll /reference:OpenCvSharp.dll /reference:OpenCvSharp.Extensions.dll /reference:OpenCvSharp.UserInterface.dll /reference:PresentationCore.dll /reference:PresentationFramework.dll /reference:System.Core.dll /reference:System.Data.DataSetExtensions.dll /reference:System.Data.dll /reference:System.Deployment.dll /reference:System.DirectoryServices.dll /reference:System.dll /reference:System.Drawing.dll /reference:System.Messaging.dll /reference:System.Windows.Forms.dll /reference:System.Xml.dll /reference:System.Xml.Linq.dll /reference:WindowsBase.dll /debug:pdbonly /filealign:512 /nowin32manifest /optimize+ /out:bin\Release\asshuku.exe /subsystemversion:6.00 /resource:obj\Release\asshuku.Form1.resources /resource:obj\Release\asshuku.Properties.Resources.resources /target:winexe /utf8output *.cs -optimize