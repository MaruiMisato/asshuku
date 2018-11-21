//#define DEBUG_SAVE  
//#define DEBUG_DISPLAY  
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
using static Image;
namespace asshuku {    
    public partial class Form1:Form {
        [DllImport("7-zip32.dll",CharSet=CharSet.Ansi)]
        private static extern int SevenZip(IntPtr hWnd,string strCommandLine,StringBuilder strOutPut,uint outputSize);
        [DllImport("kernel32.dll")]
        private static extern uint GetShortPathName(string strLongPath,StringBuilder strShortPath,uint buf);
        public Form1() {
            InitializeComponent();
        }
        public class Threshold{
            public byte Concentration{get;set;}
            public int Width{get;set;}
            public int Height{get;set;}
            public int Times{get;}=3;
        }
        public class Rect{
            public int YLow{get;set;}
            public int XLow{get;set;}
            public int YHigh{get;set;}
            public int XHigh{get;set;}
            public CvSize Size=new CvSize();
        }
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
        }
        private unsafe void WhiteCut(IplImage p_img,IplImage q_img,Rect NewImageRect) {
            byte* p=(byte*)p_img.ImageData,q=(byte*)q_img.ImageData;
            for(int y=NewImageRect.YLow;y<NewImageRect.YHigh;++y) 
                for(int x=NewImageRect.XLow;x<NewImageRect.XHigh;++x)
                    q[q_img.WidthStep*(y-NewImageRect.YLow)+(x-NewImageRect.XLow)]=p[p_img.WidthStep*y+x];
        }
        private unsafe void WhiteCutColor(ref string f,IplImage q_img,Rect NewImageRect) {//階調値線形変換はしない 
            Bitmap bmp=new Bitmap(f); 
            BitmapData data=bmp.LockBits(new Rectangle(0,0,bmp.Width,bmp.Height),ImageLockMode.ReadWrite,PixelFormat.Format32bppArgb);
            byte[] b=new byte[bmp.Width*bmp.Height*4];
            Marshal.Copy(data.Scan0,b,0,b.Length);
            byte* q=(byte*)q_img.ImageData;
            for(int y=NewImageRect.YLow;y<NewImageRect.YHigh;++y) 
                for(int x=NewImageRect.XLow;x<NewImageRect.XHigh;++x) {
                    int qoffset=q_img.WidthStep*(y-NewImageRect.YLow)+(x-NewImageRect.XLow)*3,offset=(bmp.Width*y+x)*4;
                    q[0+qoffset]=b[0+offset];q[1+qoffset]=b[1+offset];q[2+qoffset]=b[2+offset];
                }
            bmp.UnlockBits(data);
            bmp.Dispose();
        }                       
        private void PNGOut2(IEnumerable<string> files) {
            Parallel.ForEach(files,new ParallelOptions() { MaxDegreeOfParallelism=8 },f => {
                var app = new System.Diagnostics.ProcessStartInfo();
                app.FileName = "pngout.exe";
                app.Arguments = "\""+f+"\"";
                app.UseShellExecute = false;
                app.CreateNoWindow = true;    // コンソール・ウィンドウを開かない
                System.Diagnostics.Process p = System.Diagnostics.Process.Start(app);
                p.WaitForExit();	// プロセスの終了を待つ
            });
        }
        private bool CompareArrayAnd(int ___Threshold___,int[] ___CompareArray___){
            foreach(int ___CompareValue___ in ___CompareArray___){
                if(___Threshold___>___CompareValue___)continue;
                else return false;
            }
            return true;
        }
        private unsafe bool GetYLow(IplImage p_img,Threshold ImageThreshold,Rect NewImageRect){
            byte* p=(byte*)p_img.ImageData;
            int[] TargetRowArray=new int[Var.MaxMarginSize+1];
            for(int yy = 0;yy<=Var.MaxMarginSize;++yy) 
                for(int x = 0;x<p_img.Width;++x)
                    if(p[p_img.WidthStep*yy+x]<ImageThreshold.Concentration)++TargetRowArray[yy];
            if(CompareArrayAnd(ImageThreshold.Width,TargetRowArray)){
                NewImageRect.YLow=0;
                return true;
            }  
            for(int y=1;y<p_img.Height-Var.MaxMarginSize;y++){
                int TargetRow=0;
                for(int x = 0;x<p_img.Width;x++)
                    if(p[p_img.WidthStep*(y+Var.MaxMarginSize)+x]<ImageThreshold.Concentration) ++TargetRow;
                if(ImageThreshold.Width>TargetRow){
                    NewImageRect.YLow= y-Var.MaxMarginSize<0?0:y-Var.MaxMarginSize; 
                    return true;
                }
            }
            return false;//絶対到達しない
        }
        private unsafe bool GetYHigh(IplImage p_img,Threshold ImageThreshold,Rect NewImageRect){
            byte* p=(byte*)p_img.ImageData;
            int[] TargetRowArray=new int[Var.MaxMarginSize+1];
            for(int yy=-Var.MaxMarginSize;yy<1;++yy) 
                for(int x=0;x<p_img.Width;++x) 
                    if(p[p_img.WidthStep*((p_img.Height-1)+yy)+x]<ImageThreshold.Concentration)++TargetRowArray[-yy];
            if(CompareArrayAnd(ImageThreshold.Width,TargetRowArray)){
                NewImageRect.YHigh=p_img.Height;//prb  
                return true;
            }
            for(int y=p_img.Height-2;y>(NewImageRect.YLow+Var.MaxMarginSize);--y) {//Y下取得
                int TargetRow=0;
                for(int x=0;x<p_img.Width;++x) 
                    if(p[p_img.WidthStep*(y-Var.MaxMarginSize)+x]<ImageThreshold.Concentration)++TargetRow;
                if((ImageThreshold.Width>TargetRow)){
                    NewImageRect.YHigh= y+Var.MaxMarginSize>p_img.Height?p_img.Height:y+Var.MaxMarginSize;//prb
                    return true;
                }
            }
            return false;//絶対到達しない
        }
        private unsafe bool GetXLow(IplImage p_img,Threshold ImageThreshold,Rect NewImageRect){
            byte* p=(byte*)p_img.ImageData;
            int[] TargetRowArray=new int[Var.MaxMarginSize+1];
            for(int xx=0;xx<=Var.MaxMarginSize;++xx) 
                for(int y=NewImageRect.YLow;y<NewImageRect.YHigh;++y) 
                    if(p[xx+p_img.WidthStep*y]<ImageThreshold.Concentration)++TargetRowArray[xx];
            if(CompareArrayAnd(ImageThreshold.Height,TargetRowArray)){
                NewImageRect.XLow=0;
                return true;  
            }
            for(int x=0;x<p_img.Width-Var.MaxMarginSize;x++) {//X左取得
                int TargetRow=0;
                for(int y=NewImageRect.YLow;y<NewImageRect.YHigh;++y) 
                    if(p[x+Var.MaxMarginSize+p_img.WidthStep*y]<ImageThreshold.Concentration)++TargetRow;
                if(ImageThreshold.Height>TargetRow){
                    NewImageRect.XLow= x-Var.MaxMarginSize<0?0:x-Var.MaxMarginSize;
                    return true;  
                }  
            }                
            return false;//絶対到達しない
        }
        private unsafe bool GetXHigh(IplImage p_img,Threshold ImageThreshold,Rect NewImageRect){
            byte* p=(byte*)p_img.ImageData;
            int[] TargetRowArray=new int[Var.MaxMarginSize+1];
            for(int xx=-Var.MaxMarginSize;xx<1;++xx) 
                for(int y=NewImageRect.YLow;y<NewImageRect.YHigh;++y) 
                    if(p[((p_img.Width-1)+xx)+p_img.WidthStep*y]<ImageThreshold.Concentration)++TargetRowArray[-xx];
            if(CompareArrayAnd(ImageThreshold.Height,TargetRowArray)){
                NewImageRect.XHigh=p_img.Width; //prb
                return true;
            }
            for(int x=p_img.Width-2;x>NewImageRect.XLow+Var.MaxMarginSize;--x) {//X右取得
                int TargetRow=0;
                for(int y=NewImageRect.YLow;y<NewImageRect.YHigh;++y) 
                    if(p[x-Var.MaxMarginSize+p_img.WidthStep*y]<ImageThreshold.Concentration)++TargetRow;
                if(ImageThreshold.Height>TargetRow){
                    NewImageRect.XHigh= x+Var.MaxMarginSize>p_img.Width?p_img.Width:x+Var.MaxMarginSize ;//prb
                    return true;
                }  
            }
            return false;
        }
        private bool GetNewImageSize(IplImage p_img,Threshold ImageThreshold,Rect NewImageRect) {
            ImageThreshold.Width = p_img.Width-ImageThreshold.Times;
            if(!GetYLow(p_img,ImageThreshold,NewImageRect))//Y上取得
                return false;
            if(!GetYHigh(p_img,ImageThreshold,NewImageRect))//X左
                return false;
            NewImageRect.Size.Height=NewImageRect.YHigh-NewImageRect.YLow;
            ImageThreshold.Height = NewImageRect.Size.Height-ImageThreshold.Times;
            if(!GetXLow(p_img,ImageThreshold,NewImageRect))//Y下取得
                return false;
            if(!GetXHigh(p_img,ImageThreshold,NewImageRect))//X右
                return false;
            NewImageRect.Size.Width=NewImageRect.XHigh-NewImageRect.XLow;
            return true;
        }   
        private int GetRangeMedianF(IplImage p_img){
            return StandardAlgorithm.Math.MakeItOdd((int) Math.Sqrt(Math.Sqrt(Image.GetShortSide(p_img)+80)));
        }
        private byte GetConcentrationThreshold(ToneValue ImageToneValue){
            return (byte)((ImageToneValue.Max-ImageToneValue.Min)*25/Const.Tone8Bit);
        }
        private bool CutMarginMain(ref string f,TextWriter writerSync){
            IplImage InputGrayImage=Cv.LoadImage(f,LoadMode.GrayScale);//
            IplImage MedianImage = Cv.CreateImage(InputGrayImage.Size, BitDepth.U8, 1);
            Image.Filter.FastestMedian(InputGrayImage,MedianImage,GetRangeMedianF(InputGrayImage));

            IplImage LaplacianImage = Cv.CreateImage(MedianImage.Size, BitDepth.U8, 1);
            int[] FilterMask=new int[Const.Neighborhood8];
            Image.Filter.ApplyMask(Image.Filter.SetMask.Laplacian(FilterMask),MedianImage,LaplacianImage);
                
                #if (DEBUG_SAVE)  
                Debug.SaveImage(InputGrayImage,nameof(InputGrayImage));//debug
                Debug.SaveImage(MedianImage,nameof(MedianImage));//debug
                Debug.SaveImage(LaplacianImage,nameof(LaplacianImage));//debug
                #endif 
                #if (DEBUG_DISPLAY)  
                Debug.DisplayImage(InputGrayImage,nameof(InputGrayImage));//debug
                Debug.DisplayImage(MedianImage,nameof(MedianImage));//debug
                Debug.DisplayImage(LaplacianImage,nameof(LaplacianImage));//debug
                #endif 

            Cv.ReleaseImage(MedianImage);
            Image.Filter.FastestMedian(LaplacianImage,GetRangeMedianF(LaplacianImage));

                #if (DEBUG_SAVE)  
                Debug.SaveImage(LaplacianImage,nameof(LaplacianImage));//debug
                #endif 
                #if (DEBUG_DISPLAY)  
                Debug.DisplayImage(LaplacianImage,nameof(LaplacianImage));//debug
                #endif 
            
            int[] Histgram=new int[Const.Tone8Bit];
            int Channel=Image.GetHistgramR(ref f,Histgram);//bool gray->true
            ToneValue ImageToneValue = new ToneValue();
            ImageToneValue.Max=Image.GetToneValueMax(Histgram);
            ImageToneValue.Min=Image.GetToneValueMin(Histgram);
            if(ImageToneValue.Max==ImageToneValue.Min){
                Cv.ReleaseImage(InputGrayImage);
                Cv.ReleaseImage(LaplacianImage);
                return false;
            }
            Threshold ImageThreshold = new Threshold();
            ImageThreshold.Concentration=GetConcentrationThreshold(ImageToneValue);//勾配が重要？
            Rect NewImageRect=new Rect();
            if(!GetNewImageSize(LaplacianImage,ImageThreshold,NewImageRect)){
                Cv.ReleaseImage(InputGrayImage);
                Cv.ReleaseImage(LaplacianImage);
                return false;
            }
            Cv.ReleaseImage(LaplacianImage);
            writerSync.WriteLine(f+"\n\tthreshold="+ImageThreshold.Concentration+":ToneValueMin="+ImageToneValue.Min+":ToneValueMax="+ImageToneValue.Max+":hi="+NewImageRect.YLow+":fu="+NewImageRect.XLow+":mi="+NewImageRect.YHigh+":yo="+NewImageRect.XHigh+"\n\t("+InputGrayImage.Width+","+InputGrayImage.Height+")\n\t("+NewImageRect.Size.Width+","+NewImageRect.Size.Height+")");//prb
            IplImage OutputCutImage=Cv.CreateImage(NewImageRect.Size,BitDepth.U8,Channel);//prb
            if(Channel==Is.GrayScale){         
                WhiteCut(InputGrayImage,OutputCutImage,NewImageRect);
                Image.Transform2Linear(OutputCutImage,ImageToneValue);// 階調値変換
            }else{//Is.Color
                WhiteCutColor(ref f,OutputCutImage,NewImageRect);//bitmapで読まないと4Byteなのか3Byteなのか曖昧なので統一は出来ない
            } 
            Cv.SaveImage(f,OutputCutImage,new ImageEncodingParam(ImageEncodingID.PngCompression,0));
            Cv.ReleaseImage(InputGrayImage);
            Cv.ReleaseImage(OutputCutImage);
            return true;
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
            PNGOut2(files);//PNGOptimize
            sw.Stop();richTextBox1.Text+=("\npngout:"+sw.Elapsed);
        }
        private int GetFileNameBeforeChange(IEnumerable<string> files,string[] AllOldFileName) {
            int MaxFile=-1;
            foreach(string f in files) {
                FileInfo file=new FileInfo(f);
                if(file.Extension==".db"||file.Extension==".ini") file.Delete();//Disposal of garbage
                else AllOldFileName[++MaxFile]=f;
            }
            return ++MaxFile;
        }
        /*
        (file.Name.Length-file.Extension.Length)拡張子を除いたファイル名長さ
        ファイル名のサイズを三桁以上にして先頭にzを付加
        ファイル名に重複がある場合どうにもならないのでfalseを返す
         */
        private bool SortFiles(int MaxFile,string PathName,string[] AllOldFileName) {
            for(int i=MaxFile-1;i>=0;--i) {//尻からリネームしないと終わらない?
                FileInfo file=new FileInfo(AllOldFileName[i]);
                while((file.Name.Length-file.Extension.Length)<3) 
                    if(System.IO.File.Exists(PathName+"/0"+file.Name)){//重複
                        richTextBox1.Text+="\n:"+PathName+"/0"+file.Name+":Exists";
                        return false;
                    }
                    else file.MoveTo((PathName+"/0"+file.Name));//0->000  1000枚までしか無理 7zは650枚
                if((file.Name[0]!='z')) file.MoveTo((PathName+"/z"+file.Name));//000->z000
            }
            return true;
        }
        private void CreateNewFileName(int MaxFile,string[] NewFileName) {
            if(radioButton2.Checked&&MaxFile<=26*25) {//7zip under 26*25=650
                int MaxRoot=(int)Math.Sqrt(MaxFile)+1;
                richTextBox1.Text+="\nroot MaxRoot"+MaxRoot;
                for(int i=0;i<NewFileName.Length;++i) NewFileName[i]=(char)((i/MaxRoot)+'a')+((char)(i%MaxRoot+'a')).ToString();//26*25  36*35mezasu
            } else if(MaxFile<35) {//一桁で1-9,a-y
                for(int i=0;i<NewFileName.Length&&i<10;++i)NewFileName[i]=i.ToString();//0 ~ 9
                for(int i=10;i<NewFileName.Length;++i)NewFileName[i]=((char)((i-10)+'a')).ToString();//a~y
            } else {//zip under 36*25+100=1000
                for(int i=0;(i<NewFileName.Length)&&(i<100);++i)NewFileName[i]=i.ToString();//0 ~ 99 zipではこの法が軽い
                if(MaxFile<100)return;
                char[] y1=new char[36];
                for(int i=0;i<10;++i)y1[i]=(char)(i+'0');//0 ~ 9
                for(int i=10;i<y1.Length;++i)y1[i]=(char)(i-10+'a');//a~y
                for(int i=100;i<NewFileName.Length;++i) NewFileName[i]+=(char)(((i-100)/36)+'a')+(y1[(i-100)%36]).ToString();
            }
        }
        private void CreateZip(string PathName,IEnumerable<string> files) {
            string Extension="zip";
            if(radioButton3.Checked) {//winrar
                Extension="rar";
                var PythonApp = new System.Diagnostics.ProcessStartInfo();
                PythonApp.FileName = "Rar.exe";
                PythonApp.Arguments = " a "+PathName+".rar -rr5 -mt8 -m5 -ep " + PathName;

                PythonApp.UseShellExecute = false;
                PythonApp.CreateNoWindow = true;    // コンソール・ウィンドウを開かない
                System.Diagnostics.Process PythonProcess = System.Diagnostics.Process.Start(PythonApp);
                PythonProcess.WaitForExit();	// プロセスの終了を待つ
            } else {
                if(radioButton2.Checked) Extension="7z";
                StringBuilder strShortPath=new StringBuilder(1024);
                GetShortPathName(PathName,strShortPath,1024);                
                richTextBox1.Text+="\n"+PathName+"."+Extension+"\n";
                if(radioButton5.Checked)SevenZip(this.Handle,"a -hide -t"+Extension+" \""+PathName+"."+Extension+"\" "+strShortPath+"\\*",new StringBuilder(1024),1024);//Create a ZIP archive
                else if(radioButton4.Checked)SevenZip(this.Handle,"a -hide -t"+Extension+" \""+PathName+"."+Extension+"\" "+strShortPath+"\\* -mx9",new StringBuilder(1024),1024);
                else SevenZip(this.Handle,"a -hide -t"+Extension+" \""+PathName+"."+Extension+"\" "+strShortPath+"\\* -mx0",new StringBuilder(1024),1024);//Create a ZIP archive
            }
            RenameNumberOnlyFile(PathName,Extension); 
        }
        private string GetNumberOnlyPath(string PathName) {//ファイル名からX巻のXのみを返す
            string FileName = System.IO.Path.GetFileName(PathName);//Z:\[宮下英樹] センゴク権兵衛 第05巻 ->[宮下英樹] センゴク権兵衛 第05巻
            Match MatchedNumber = Regex.Match(FileName,"(\\d)+巻");//[宮下英樹] センゴク権兵衛 第05巻 ->05巻
            if(MatchedNumber.Success)
                MatchedNumber = Regex.Match(MatchedNumber.Value,"(\\d)+");//05巻->05
            else{
                MatchedNumber=Regex.Match(FileName,"(\\d)+");//[宮下英樹] センゴク権兵衛 第05 ->05   
                if(!MatchedNumber.Success)
                    return PathName;//[宮下英樹] センゴク権兵衛 第 ->
            }
            //文字列を置換する（FileNameをMatchedNumber.Valueに置換する）
            return PathName.Replace(FileName,int.Parse(MatchedNumber.Value).ToString());//Z:\5
        }
        private bool RenameNumberOnlyFile(string PathName,string Extension) {
                string NewFileName=GetNumberOnlyPath(PathName)+"."+Extension;
                if (System.IO.File.Exists(NewFileName))//重複
                    return false;
                FileInfo file=new FileInfo(PathName+"."+Extension);
                file.MoveTo(NewFileName);
                richTextBox1.Text+=NewFileName+"\n";//Show path
                return true;
        }
        private bool IsTheNumberOfFilesAppropriate(int MaxFile){
            if(MaxFile>36*25+100) {
                richTextBox1.Text+="\nMaxFile:"+MaxFile+" => over 1,000";
                return false;
            }else if(MaxFile<1){
                richTextBox1.Text+="\nMaxFile:"+MaxFile+" 0";
                return false;
            }else{
                richTextBox1.Text+="\nMaxFile:"+MaxFile+":OK.";
                return true;
            }
        }
        private void FileProcessing(System.Collections.Specialized.StringCollection filespath){
            foreach(string PathName in filespath) {//Enumerate acquired paths
                logs.Items.Add(PathName);
                richTextBox1.Text+=PathName;//Show path
                IEnumerable<string> files=System.IO.Directory.EnumerateFiles(PathName,"*",System.IO.SearchOption.TopDirectoryOnly);//Acquire  files  the path.
                string[] AllOldFileName=new string[System.IO.Directory.GetFiles(PathName,"*",SearchOption.TopDirectoryOnly).Length];//36*25+100 ファイル数 ゴミ込み
                int MaxFile=GetFileNameBeforeChange(files,AllOldFileName);
                if(!IsTheNumberOfFilesAppropriate(MaxFile))continue;
                if(!SortFiles(MaxFile,PathName,AllOldFileName))continue;
                string[] NewFileName=new string[MaxFile];
                CreateNewFileName(MaxFile,NewFileName);
                ReNameAlfaBeta(PathName,ref files,NewFileName);
                if(radioButton7.Checked) RemovePNGMarginEntry(PathName);
                CreateZip(PathName,files);
            }
        }
        private void button1_Click(object sender,EventArgs e) {
            if(!Clipboard.ContainsFileDropList()) {//Check if clipboard has file drop format data. 
                MessageBox.Show("Please select folders.");
                return;
            } 
            FileProcessing(Clipboard.GetFileDropList());
        }
        private void BrowserButtonClick(object sender,EventArgs e) {//Folder dialog related.
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