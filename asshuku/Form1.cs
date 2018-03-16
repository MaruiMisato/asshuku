using System;
using System.IO;
//using System.IO.Path;
using System.Text;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;//正規表現
using System.Runtime.InteropServices;
using Ionic.Zip;
using OpenCvSharp;
namespace asshuku {    
    public partial class Form1:Form {
        [DllImport("7-zip32.dll",CharSet=CharSet.Ansi)]
        private static extern int SevenZip(IntPtr hWnd,string strCommandLine,StringBuilder strOutPut,uint outputSize);
        [DllImport("kernel32.dll")]
        private static extern uint GetShortPathName(string strLongPath,StringBuilder strShortPath,uint buf);
        public Form1() {
            InitializeComponent();
        }
        /*private void ReNameAlfaBeta(string PathName,ref IEnumerable<string> files,string[] NewFileName) {
            foreach(string f in files.Select((v, i) => new {Value = v, Index = i })) {
                FileInfo file=new FileInfo(f.Value);
                string FileName = NewFileName[f.Index]+".png";
                if(file.Extension==".jpg"||file.Extension==".jpeg"||file.Extension==".JPG"||file.Extension==".JPEG") //jpg
                    FileName = NewFileName[f.Index]+".jpg";
                logs.Items.Add(Path.GetFileNameWithoutExtension(f)+" -> "+ f.Index +" "+FileName);
                file.MoveTo(PathName+"/"+FileName);
            }
        }/* */
        private void ReNameAlfaBeta(string PathName,ref IEnumerable<string> files,string[] NewFileName) {
            int i=0;
            foreach(string f in files) {
                FileInfo file=new FileInfo(f);
                string FileName = NewFileName[i]+".png";
                if(file.Extension==".jpg"||file.Extension==".jpeg"||file.Extension==".JPG"||file.Extension==".JPEG") //jpg
                    FileName = NewFileName[i]+".jpg";
                logs.Items.Add(Path.GetFileNameWithoutExtension(f)+" -> "+ i++ +" "+FileName);
                file.MoveTo(PathName+"/"+FileName);
            }
        }/* */
        /*private void ReNameAlfaBeta(string PathName,ref IEnumerable<string> files,string[] NewFileName) {
            int i=0;
            foreach(string f in files) {
                FileInfo file=new FileInfo(f);
                if(file.Extension==".jpg"||file.Extension==".jpeg"||file.Extension==".JPG"||file.Extension==".JPEG") {//jpg
                    logs.Items.Add(Path.GetFileNameWithoutExtension(f)+" -> "+i+" "+(NewFileName[i]+".jpg"));
                    file.MoveTo((PathName+"/"+NewFileName[i++]+".jpg"));
                } else {//png
                    logs.Items.Add(Path.GetFileNameWithoutExtension(f)+" -> "+i+" "+(NewFileName[i]+".png"));
                    file.MoveTo((PathName+"/"+NewFileName[i++]+".png"));
                }
            }
        }/* */             
        private void NoiseRemoveTwoArea(IplImage p_img,byte max) {
            IplImage q_img=Cv.CreateImage(Cv.GetSize(p_img),BitDepth.U8,1);
            unsafe {
                byte* p=(byte*)p_img.ImageData,q=(byte*)q_img.ImageData;
                for(int y=0;y<q_img.ImageSize;++y)q[y]=p[y]<max?(byte)0:(byte)255;//First, binarize
                for(int y=1;y<q_img.Height-1;++y) 
                    for(int x=1;x<q_img.Width-1;++x) { 
                        int offset=q_img.WidthStep*y+x;
                        if(q[offset]!=0)continue;//Count white spots around black dots
                        for(int yy=-1;yy<2;++yy) 
                            for(int xx=-1;xx<2;++xx) 
                                if(q[q_img.WidthStep*(y+yy)+x+xx]==255)
                                    ++q[offset];
                    }
                for(int y=1;y<q_img.Height-1;++y) {//
                    int yoffset=(q_img.WidthStep*y);
                    for(int x=1;x<q_img.Width-1;++x) {
                        int yxoffset =yoffset+x;
                        if(q[yxoffset]<=6)continue;
                        if(q[yxoffset]==8){
                            p[yxoffset]=max;//Independent
                            continue;
                        }
                        //if(q[yxoffset]==7)//When there are seven white spots in the periphery
                        for(int yy=-1;yy<2;++yy) 
                            for(int xx=-1;xx<2;++xx) {
                                int offset=q_img.WidthStep*(y+yy)+x+xx;
                                if(q[offset]!=7)continue;//仲間 ペア の有無
                                p[yxoffset]=max;//q[offset]=0;//Unnecessary 
                                p[offset]=max;q[offset]=0;
                                yy=1;break;
                            }
                    }
                }
            }
            Cv.ReleaseImage(q_img);
        }        
        private void WhiteCut(IplImage p_img,IplImage q_img,int hi,int fu,int mi,int yo) {
            unsafe {
                byte* p=(byte*)p_img.ImageData,q=(byte*)q_img.ImageData;
                for(int y=hi;y<=mi;++y) {
                    int yoffset=(p_img.WidthStep*y),qyoffset=(q_img.WidthStep*(y-hi));
                    for(int x=fu;x<=yo;++x)
                        q[qyoffset+(x-fu)]=p[yoffset+x];
                }
            }
        }
        private void WhiteCutColor(ref string f,IplImage q_img,int hi,int fu,int mi,int yo) {//階調値線形変換はしない 
            using(Bitmap bmp=new Bitmap(f)) {
                BitmapData data=bmp.LockBits(new Rectangle(0,0,bmp.Width,bmp.Height),ImageLockMode.ReadWrite,PixelFormat.Format32bppArgb);
                byte[] b=new byte[bmp.Width*bmp.Height*4];
                Marshal.Copy(data.Scan0,b,0,b.Length);
                unsafe {
                    byte* q=(byte*)q_img.ImageData;
                    for(int y=hi;y<=mi;++y) {
                        int yoffset=bmp.Width*4*y,qyoffset=q_img.WidthStep*(y-hi);
                        for(int x=fu;x<=yo;++x) {
                            int qoffset=qyoffset+3*(x-fu),offset=yoffset+4*x;
                            q[0+qoffset]=b[0+offset];q[1+qoffset]=b[1+offset];q[2+qoffset]=b[2+offset];
                        }
                    }
                }
                bmp.UnlockBits(data);
            }
        }        
        private void Transform2Linear(ref string f,IplImage p_img,byte min,double magnification) {//内部の空白を除去 グレイスケールのみ
            unsafe {
                byte* p=(byte*)p_img.ImageData;
                for(int y=0;y<p_img.Height;++y){
                    int yoffset=p_img.WidthStep*y;
                    for(int x=0;x<p_img.Width;++x){
                        double temp = (magnification*(p[yoffset+x]-min));
                        temp = temp>255?255:temp<0?0:temp;
                        p[yoffset+(x)]=(byte)temp;//255.99ないと255が254になる
                    }
                }
            }
        }        
        private void PNGOut(IEnumerable<string> files) {
            System.Diagnostics.Process p=new System.Diagnostics.Process();//Create a Process object
            p.StartInfo.FileName=System.Environment.GetEnvironmentVariable("ComSpec");//ComSpec(cmd.exe)のパスを取得して、FileNameプロパティに指定
            p.StartInfo.WindowStyle=System.Diagnostics.ProcessWindowStyle.Hidden;//HiddenMaximizedMinimizedNormal
            Parallel.ForEach(files,new ParallelOptions() { MaxDegreeOfParallelism=4 },f => {
                p.StartInfo.Arguments="/c pngout "+f;//By default, PNGOUT will not overwrite a PNG file if it was not able to compress it further.
                p.Start();p.WaitForExit();//起動
            });
            p.Close();
        }
                   
        private void GetNewImageSize(IplImage p_img,byte ConcentrationThreshold,int TimesThreshold,ref int YLow,ref int XLow,ref int YHigh,ref int XHigh) {
            unsafe {
                byte* p=(byte*)p_img.ImageData;
                int ThresholdWidth = p_img.Width-TimesThreshold;
                for(int y=0;y<p_img.Height-4;y++) {//Y上取得
                    if(y==0){
                        int[] l=new int[5];
                        for(int yy = 0;(yy<5);++yy) {
                            int yyyoffset = (p_img.WidthStep*(y+yy));
                            for(int x = 0;x<p_img.Width;x++)
                                if(p[yyyoffset+x]<ConcentrationThreshold)++l[yy];
                        }
                        if((ThresholdWidth>l[0])&&(ThresholdWidth>l[1])&&(ThresholdWidth>l[2])&&(ThresholdWidth>l[3])&&(ThresholdWidth>l[4])) { YLow=0; break; }
                    } else {
                        int l=0;
                        int yoffset=(p_img.WidthStep*(y+4));
                        for(int x = 0;x<p_img.Width;x++)if(p[yoffset+x]<ConcentrationThreshold) ++l;
                        if((ThresholdWidth>l)) { YLow=y-4; break; }
                    }
                }YLow=YLow<0?0:YLow;            
                for(int y=p_img.Height-1;y>(YLow+4);--y) {//Y下取得
                    if(y==p_img.Height-1) {
                        int[] l=new int[5];
                        for(int yy=-4;(yy<1);++yy) {
                            int yyyoffset=(p_img.WidthStep*(y+yy));
                            for(int x=0;x<p_img.Width;x++) if(p[yyyoffset+x]<ConcentrationThreshold)++l[-yy];
                        }
                        if((ThresholdWidth>l[0])&&(ThresholdWidth>l[1])&&(ThresholdWidth>l[2])&&(ThresholdWidth>l[3])&&(ThresholdWidth>l[4])) { YHigh=y; break; }
                    } else {
                        int yoffset=(p_img.WidthStep*(y-4));
                        int l=0;
                        for(int x=0;x<p_img.Width;x++) if(p[yoffset+x]<ConcentrationThreshold)++l;
                        if((ThresholdWidth>l)) { YHigh=y+4; break; }
                    }
                }
                YHigh=YHigh>=p_img.Height?p_img.Height-1:YHigh;
                int ThresholdHeight = (YHigh-YLow)-TimesThreshold;
                for(int x=0;x<p_img.Width-4;x++) {//X左取得
                    if(x==0) {
                        int[] l=new int[5];
                        for(int xx=0;(xx<5);++xx) {
                            int xxxoffset=(x+xx);
                            for(int y=YLow;y<YHigh;y++) if(p[xxxoffset+p_img.WidthStep*y]<ConcentrationThreshold)++l[xx];
                        }
                        if((ThresholdHeight>l[0])&&(ThresholdHeight>l[1])&&(ThresholdHeight>l[2])&&(ThresholdHeight>l[3])&&(ThresholdHeight>l[4])) { XLow=0; break; }
                    } else {
                        int xoffset=(x+4);
                        int l=0;
                        for(int y=YLow;y<YHigh;y++) if(p[xoffset+p_img.WidthStep*y]<ConcentrationThreshold)++l;
                        if(ThresholdHeight>l) { XLow=x-4; break; }
                    }
                }                
                XLow=XLow<0?0:XLow;
                for(int x=p_img.Width-1;x>(XLow+4);--x) {//X右取得
                    if(x==p_img.Width-1) {
                        int[] l=new int[5];
                        for(int xx=-4;(xx<1);++xx) {
                            int xxxoffset=(x+xx);
                            for(int y=YLow;y<YHigh;y++) if(p[xxxoffset+p_img.WidthStep*y]<ConcentrationThreshold)++l[-xx];
                        }
                        if((ThresholdHeight>l[0])&&(ThresholdHeight>l[1])&&(ThresholdHeight>l[2])&&(ThresholdHeight>l[3])&&(ThresholdHeight>l[4])) { XHigh=x; break; }
                    } else {
                        int xoffset=(x-4);
                        int l=0;
                        for(int y=YLow;y<YHigh;y++) if(p[xoffset+p_img.WidthStep*y]<ConcentrationThreshold)++l;
                        if(ThresholdHeight>l) { XHigh=x+4; break; }
                    }
                }
                XHigh=XHigh>=p_img.Width?p_img.Width-1:XHigh;
            }
        }        
        private int GetRangeMedianF(IplImage p_img){
            int Range=(int) Math.Sqrt(Math.Sqrt(Image.GetShortSide(p_img)+80));//4乗根
            return (Range - ((Range+1)&1));//奇数にしたい
        }
        private byte GetConcentrationThreshold(byte ToneValueMax,byte ToneValueMin){
            return (byte)((ToneValueMax-ToneValueMin)*25/GetConstant.Tone8Bit);
        }
        private void CutMarginMain(ref string f,TextWriter writerSync){
            IplImage InputGrayImage=Cv.LoadImage(f,LoadMode.GrayScale);//
            IplImage MedianImage = Cv.CreateImage(InputGrayImage.GetSize(), BitDepth.U8, 1);
            Image.Filter.FastestMedian(InputGrayImage,MedianImage,GetRangeMedianF(InputGrayImage));

            IplImage LaplacianImage = Cv.CreateImage(MedianImage.GetSize(), BitDepth.U8, 1);
            int[] FilterMask=new int[GetConstant.Neighborhood8];
            Image.Filter.ApplyMask(Image.Filter.SetMask.Laplacian(FilterMask),MedianImage,LaplacianImage);
            //Debug.DisplayImage(MedianImage,nameof(MedianImage));//debug
            //Debug.SaveImage(MedianImage,nameof(MedianImage));//debug
            Cv.ReleaseImage(MedianImage);
            
            int[] Histgram=new int[GetConstant.Tone8Bit];
            int Channel=Image.GetHistgramR(ref f,Histgram);//bool gray->true
            byte ToneValueMax=Image.GetToneValueMax(Histgram);
            byte ToneValueMin=Image.GetToneValueMin(Histgram);
            if(ToneValueMax==ToneValueMin){
                Cv.ReleaseImage(InputGrayImage);
                Cv.ReleaseImage(LaplacianImage);
                return;
            }/**/
            byte ConcentrationThreshold=GetConcentrationThreshold(ToneValueMax,ToneValueMin);//勾配が重要？
            int TimesThreshold=1;
            int YLow=0,XLow=0,YHigh=InputGrayImage.Height-1,XHigh=InputGrayImage.Width-1;
            GetNewImageSize(LaplacianImage,ConcentrationThreshold,TimesThreshold,ref YLow,ref XLow,ref YHigh,ref XHigh);
            
            Cv.ReleaseImage(LaplacianImage);
            writerSync.WriteLine(f+"\n\tthreshold="+ConcentrationThreshold+":ToneValueMin="+ToneValueMin+":ToneValueMax="+ToneValueMax+":hi="+YLow+":fu="+XLow+":mi="+YHigh+":yo="+XHigh+"\n\t("+InputGrayImage.Width+","+InputGrayImage.Height+")\n\t("+((XHigh-XLow)+1)+","+((YHigh-YLow)+1)+")");
            IplImage OutputCutImage=Cv.CreateImage(new CvSize((XHigh-XLow)+1,(YHigh-YLow)+1),BitDepth.U8,Channel);
            if(Channel==Is.GrayScale){         
                WhiteCut(InputGrayImage,OutputCutImage,YLow,XLow,YHigh,XHigh);
                Transform2Linear(ref f,OutputCutImage,ToneValueMin,255.99/(ToneValueMax-ToneValueMin));//内部の空白を除去 階調値変換
            }else{//Is.Color
                WhiteCutColor(ref f,OutputCutImage,YLow,XLow,YHigh,XHigh);//bitmapで読まないと4Byteなのか3Byteなのか曖昧なので統一は出来ない
            } 
            Cv.SaveImage(f,OutputCutImage,new ImageEncodingParam(ImageEncodingID.PngCompression,0));
            Cv.ReleaseImage(InputGrayImage);
            Cv.ReleaseImage(OutputCutImage);
        }
        private void RemovePNGMarginEntry(string PathName) {
            IEnumerable<string> files=System.IO.Directory.EnumerateFiles(PathName,"*.png",System.IO.SearchOption.AllDirectories);//Acquire only png files under the path.
            System.Diagnostics.Stopwatch sw=new System.Diagnostics.Stopwatch();//stop watch get time
            sw.Start();
            using(TextWriter writerSync=TextWriter.Synchronized(new StreamWriter(DateTime.Now.ToString("yyyy.MM.dd.HH.mm.ss")+".log",false,System.Text.Encoding.GetEncoding("shift_jis")))) { 
                Parallel.ForEach(files,new ParallelOptions() { MaxDegreeOfParallelism=4 },f => {//Specify the number of concurrent threads(The number of cores is reasonable).
                    CutMarginMain(ref f,writerSync);
                });
                writerSync.WriteLine(DateTime.Now.ToString("yyyy.MM.dd.HH.mm.ss"));
            }
            sw.Stop();richTextBox1.Text+=("\nWhiteRemove:"+sw.Elapsed);
            sw.Restart();
            PNGOut(files);//PNGOptimize
            sw.Stop();richTextBox1.Text+=("\npngout:"+sw.Elapsed);
        }
        private int GetFileNameBeforeChange(IEnumerable<string> files,string[] AllOldFileName) {
            int MaxFile=0;
            foreach(string f in files) {
                FileInfo file=new FileInfo(f);
                if(file.Extension==".db"||file.Extension==".ini") file.Delete();//Disposal of garbage
                else AllOldFileName[MaxFile++]=f;
            }
            return MaxFile;
        }
        private void SortFiles(int MaxFile,string PathName,string[] AllOldFileName) {
            for(int i=MaxFile-1;i>=0;--i) {//尻からリネーム
                FileInfo file=new FileInfo(AllOldFileName[i]);
                while((file.Name.Length-file.Extension.Length)<3) file.MoveTo((PathName+"/0"+file.Name));//0->000  1000枚までしか無理 7zは650枚
                if((file.Name[0]!='z')) file.MoveTo((PathName+"/z"+file.Name));//000->z000
            }
        }
        private void CreateNewFileName(int MaxFile,string[] NewFileName) {
            if(radioButton2.Checked&&MaxFile<=26*25) {//7zip under 26*25=650
                int MaxRoot=(int)Math.Sqrt(MaxFile)+1;
                richTextBox1.Text+="\nroot MaxRoot"+MaxRoot;
                for(int i=0;i<NewFileName.Length;++i) NewFileName[i]=(char)((i/MaxRoot)+'a')+((char)(i%MaxRoot+'a')).ToString();//26*25  36*35mezasu
            } else if(MaxFile<35) {//一桁で1-y
                for(int i=0;(i<NewFileName.Length)&&(i<10);++i) NewFileName[i]=i.ToString();//0 ~ 9
                for(int i=10;i<NewFileName.Length;++i) NewFileName[i]=((char)((i-10)+'a')).ToString();//a~y
            } else {//zip under 36*25+100=1000
                char[] y1=new char[36];
                for(int i=0;i<10;++i) y1[i]=(char)(i+'0');//0 ~ 9
                for(int i=10;i<y1.Length;++i) y1[i]=(char)(i-10+'a');//a~y
                for(int i=0;(i<NewFileName.Length)&&(i<100);++i) NewFileName[i]=i.ToString();//0 ~ 99 zipではこの法が軽い
                for(int i=100;i<NewFileName.Length;++i) NewFileName[i]+=(char)(((i-100)/36)+'a')+(y1[(i-100)%36]).ToString();
            }
        }
        private void CreateZip(string PathName,IEnumerable<string> files) {
            if(radioButton3.Checked) {//Ionic.Zip
                Ionic.Zip.ZipFile zip=new Ionic.Zip.ZipFile();//Create a ZIP archive
                if(radioButton4.Checked) zip.CompressionLevel=Ionic.Zlib.CompressionLevel.Level9;//max
                else if(radioButton5.Checked) zip.CompressionLevel=Ionic.Zlib.CompressionLevel.Default;//Default
                else zip.CompressionLevel=Ionic.Zlib.CompressionLevel.None;
                foreach(string f in files) {
                    ZipEntry entry=zip.AddFile(f);//Add a file
                    entry.FileName=new FileInfo(f).Name;
                } 
                zip.Save(PathName+".zip");//Create a ZIP archive
                RenameNumberOnlyFile(PathName,"zip");
            } else {
                string Extension="zip";
                if(radioButton2.Checked) Extension="7z";
                StringBuilder strShortPath=new StringBuilder(1024);
                GetShortPathName(PathName,strShortPath,1024);                
                richTextBox1.Text+="\n"+PathName+"."+Extension+"\n";
                if(radioButton5.Checked) SevenZip(this.Handle,"a -hide -t"+Extension+" \""+PathName+"."+Extension+"\" "+strShortPath+"\\*",new StringBuilder(1024),1024);//Create a ZIP archive
                else if(radioButton4.Checked) SevenZip(this.Handle,"a -hide -t"+Extension+" \""+PathName+"."+Extension+"\" "+strShortPath+"\\* -mx9",new StringBuilder(1024),1024);
                else SevenZip(this.Handle,"a -hide -t"+Extension+" \""+PathName+"."+Extension+"\" "+strShortPath+"\\* -mx0",new StringBuilder(1024),1024);//Create a ZIP archive
                RenameNumberOnlyFile(PathName,Extension);
            }
        }
        private string GetNumberOnlyPath(string PathName) {
            string FileName = System.IO.Path.GetFileName(PathName);//Z:\[宮下英樹] センゴク権兵衛 第05巻 ->[宮下英樹] センゴク権兵衛 第05巻
            Match matchedObject = Regex.Match(FileName,"(\\d)+巻");//[宮下英樹] センゴク権兵衛 第05巻 ->05巻
            if(matchedObject.Success)
                matchedObject = Regex.Match(matchedObject.Value,"(\\d)+");//05巻->05
            else{
                matchedObject=Regex.Match(FileName,"(\\d)+");//[宮下英樹] センゴク権兵衛 第05 ->05
                if(!matchedObject.Success)
                    return PathName;//[宮下英樹] センゴク権兵衛 第 ->
            }
            //文字列を置換する（FileNameをmatchedObject.Valueに置換する）
            return PathName.Replace(FileName,int.Parse(matchedObject.Value).ToString());//Z:\5
        }
        private bool RenameNumberOnlyFile(string PathName,string Extension) {
                string NewFileName=GetNumberOnlyPath(PathName)+"."+Extension;
                if (System.IO.File.Exists(NewFileName))//重複
                    return false;
                FileInfo file=new FileInfo(PathName+"."+Extension);
                file.MoveTo(NewFileName);
                richTextBox1.Text+=NewFileName;//Show path
                return true;
        }
        private void FileProcessing(System.Collections.Specialized.StringCollection filespath){
            foreach(string PathName in filespath) {//Enumerate acquired paths
                logs.Items.Add(PathName);
                richTextBox1.Text+=PathName;//Show path
                IEnumerable<string> files=System.IO.Directory.EnumerateFiles(PathName,"*",System.IO.SearchOption.TopDirectoryOnly);//Acquire  files  the path.
                string[] AllOldFileName=new string[System.IO.Directory.GetFiles(PathName,"*",SearchOption.TopDirectoryOnly).Length];//36*25+100 ファイル数 ゴミ込み
                int MaxFile=GetFileNameBeforeChange(files,AllOldFileName);
                if(MaxFile>(36*25)+100) {
                    richTextBox1.Text+="\nMaxFile:"+MaxFile+" => over 1,000";
                    continue;
                }
                richTextBox1.Text+="\nMaxFile:"+MaxFile;
                SortFiles(MaxFile,PathName,AllOldFileName);
                string[] NewFileName=new string[MaxFile];
                CreateNewFileName(MaxFile,NewFileName);
                ReNameAlfaBeta(PathName,ref files,NewFileName);
                if(radioButton7.Checked) RemovePNGMarginEntry(PathName);
                CreateZip(PathName,files);
            }
        }
        private void button1_Click(object sender,EventArgs e) {
            if(!Clipboard.ContainsFileDropList()) {//Check if clipboard has file drop format data. 取得できなかったときはnull listBox1.Items.Clear();
                MessageBox.Show("Please select folders.");
                return;
            } 
            System.Collections.Specialized.StringCollection filespath=Clipboard.GetFileDropList();//Get filepath from clipboard
            FileProcessing(filespath);
        }
        private void button2_Click(object sender,EventArgs e) {//Folder dialog related.
            FolderBrowserDialog fbd=new FolderBrowserDialog();//Create an instance of the FolderBrowserDialog class
            fbd.Description="Please specify a folder.";//Specify explanatory text to be displayed at the top.
            fbd.SelectedPath=@"Z:\download\";//Specify the folder to select first // It must be a folder under RootFolder
            fbd.ShowNewFolderButton=true;//AlLow users to create new folders
            fbd.ShowDialog(this);//Display a dialog
            richTextBox1.Text=fbd.SelectedPath;//Show path
            Clipboard.SetFileDropList(new System.Collections.Specialized.StringCollection() { fbd.SelectedPath });//コピーするファイルのパスをStringCollectionに追加する. Copy to clipboard
        }
    }
}