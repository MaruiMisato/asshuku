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
using System.Linq;
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
            Parallel.ForEach(files,new ParallelOptions() { MaxDegreeOfParallelism=16 },f => {
                ExecuteAnotherApp("pngout.exe","\""+f+"\"",false,true);
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
        private bool CutPNGMarginMain(ref string f,TextWriter writerSync){
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
        private bool CutJPGMarginMain(ref string f,TextWriter writerSync){
            IplImage InputGrayImage=Cv.LoadImage(f,LoadMode.GrayScale);//
            IplImage MedianImage = Cv.CreateImage(InputGrayImage.Size, BitDepth.U8, 1);
            Image.Filter.FastestMedian(InputGrayImage,MedianImage,GetRangeMedianF(InputGrayImage));
            IplImage LaplacianImage = Cv.CreateImage(MedianImage.Size, BitDepth.U8, 1);
            int[] FilterMask=new int[Const.Neighborhood8];
            Image.Filter.ApplyMask(Image.Filter.SetMask.Laplacian(FilterMask),MedianImage,LaplacianImage);
            Cv.ReleaseImage(MedianImage);
            Image.Filter.FastestMedian(LaplacianImage,GetRangeMedianF(LaplacianImage));
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
            writerSync.WriteLine(f+"\n"+"hi="+NewImageRect.YLow+":fu="+NewImageRect.XLow+":mi="+NewImageRect.YHigh+":yo="+NewImageRect.XHigh+"\n("+InputGrayImage.Width+","+InputGrayImage.Height+")\n("+NewImageRect.Size.Width+","+NewImageRect.Size.Height+")");//prb
            Cv.ReleaseImage(InputGrayImage);
            Cv.ReleaseImage(LaplacianImage);
            //jpegtran.exe -crop 808x1208+0+63 -outfile Z:\bin\22\6.jpg Z:\bin\22\6.jpg
            string Arguments = "-crop "+NewImageRect.Size.Width+"x"+NewImageRect.Size.Height+"+"+NewImageRect.XLow+"+"+NewImageRect.YLow+" -outfile \""+f+"\" \""+f+"\"";
            ExecuteAnotherApp("jpegtran.exe",Arguments,false,true);
            return true;
        }
        private void RemoveMarginEntry(string PathName) {
            System.Diagnostics.Stopwatch sw=new System.Diagnostics.Stopwatch();//stop watch get time
            IEnumerable<string> PNGFiles=System.IO.Directory.EnumerateFiles(PathName,"*.png",System.IO.SearchOption.AllDirectories);//Acquire only png files under the path.
            sw.Start();
            if(PNGFiles.Any()){
                using(TextWriter writerSync=TextWriter.Synchronized(new StreamWriter(DateTime.Now.ToString("yyyy.MM.dd.HH.mm.ss")+".log",false,System.Text.Encoding.GetEncoding("shift_jis")))) { 
                    Parallel.ForEach(PNGFiles,new ParallelOptions() { MaxDegreeOfParallelism=16 },f => {//Specify the number of concurrent threads(The number of cores is reasonable).
                        CutPNGMarginMain(ref f,writerSync);
                    });
                    writerSync.WriteLine(DateTime.Now.ToString("yyyy.MM.dd.HH.mm.ss"));
                    sw.Stop();richTextBox1.Text+=("\nPNGWhiteRemove:"+sw.Elapsed);
                    sw.Restart();
                    PNGOut2(PNGFiles);//PNGOptimize
                    sw.Stop();richTextBox1.Text+=("\npngout:"+sw.Elapsed);
                }
            }
            IEnumerable<string> JPGFiles=System.IO.Directory.EnumerateFiles(PathName,"*.jpg",System.IO.SearchOption.AllDirectories);//Acquire only png files under the path.
            if(JPGFiles.Any()){
                sw.Restart();
                using(TextWriter writerSync=TextWriter.Synchronized(new StreamWriter(DateTime.Now.ToString("yyyy.MM.dd.HH.mm.ss")+".log",false,System.Text.Encoding.GetEncoding("shift_jis")))) { 
                    Parallel.ForEach(JPGFiles,new ParallelOptions() { MaxDegreeOfParallelism=16 },f => {//Specify the number of concurrent threads(The number of cores is reasonable).
                        CutJPGMarginMain(ref f,writerSync);
                    });
                    writerSync.WriteLine(DateTime.Now.ToString("yyyy.MM.dd.HH.mm.ss"));
                    sw.Stop();richTextBox1.Text+=("\nJPGWhiteRemove:"+sw.Elapsed+"\n");
                }
            }
        }
        private int GetFileNameBeforeChange(IEnumerable<string> files,string[] AllOldFileName) {//ゴミファイルを除去 JPG jpeg PNG png種々あるので
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
        public string[] GetFolderName(string Folder_Name){// 指定フォルダの直下にある全てのフォルダ名を取得するメソッド// 第１引数Folder_Name: 対象のフォルダ
            string[] Folder_List; // フォルダ名格納用配列
            if (System.IO.Directory.Exists(Folder_Name) == false){// 指定フォルダが無い場合
                Folder_List = new string[0]; // 要素数が0の配列を返却
                return Folder_List;
            }
            Folder_List = System.IO.Directory.GetDirectories(Folder_Name);// フォルダ名取得
            return Folder_List;// 戻り値: 取得したフォルダ名の配列
        }
        public string[] GetFileName(string Folder_Name){// 指定フォルダの直下にある全てのファイル名を取得するメソッド// 第１引数Folder_Name: 対象のフォルダ
            string[] File_List; // ファイル名格納用配列       
            if (System.IO.Directory.Exists(Folder_Name) == false){ // 指定フォルダが無い場合
                File_List = new string[0]; // 要素数が0の配列を返却
                return File_List;
            }        
            File_List = System.IO.Directory.GetFiles(Folder_Name);// ファイル名取得
            return File_List;// 戻り値: 取得したファイル名の配列
        }
    // CopyFolderフォルダ複写用メソッド　内部フォルダ構造までコピーできる
    // 第１引数Source_Folder_Name: 複写元フォルダ
    // 第２引数Dest_Folder_Name: 複写先フォルダ
    // 第３引数Overwrite_Flag: 複写先に既に同名ファイルが存在する場合の指示。 true:上書きする / false: 上書きしない
        public void CopyFolder(string Source_Folder_Name, string Dest_Folder_Name, bool Overwrite_Flag){
            if (!System.IO.Directory.Exists(Dest_Folder_Name)){// 複写先フォルダが存在しない場合、フォルダを作成
                System.IO.Directory.CreateDirectory(Dest_Folder_Name);// フォルダ作成
                System.IO.File.SetAttributes(Dest_Folder_Name, System.IO.File.GetAttributes(Source_Folder_Name));// 作成したフォルダに、フォルダの属性を複写
                Overwrite_Flag = true; // 複写先フォルダを作ったのだから、そこにファイルは存在しないので、以後上書きで可
            }
            string[] File_List = GetFileName(Source_Folder_Name);// 複写元フォルダ直下のファイル名を取得
            if (Overwrite_Flag){// 複写先に既に同名ファイルが存在する場合に、上書きが許可されている場合    
                foreach (string File_Work in File_List){// 複写元フォルダの直下にあるファイルを複写        
                    string Dest_Direc_Work = System.IO.Path.Combine(Dest_Folder_Name, System.IO.Path.GetFileName(File_Work));// 複写先フォルダ名の末尾に、取得ファイル名を付加
                    System.IO.File.Copy(File_Work, Dest_Direc_Work, true);// ファイル複写
                }
            }else{// 複写元フォルダの直下にあるファイルを複写
                foreach (string File_Work in File_List){        
                    string Dest_Direc_Work = System.IO.Path.Combine(Dest_Folder_Name, System.IO.Path.GetFileName(File_Work));// 複写先フォルダ名の末尾に、取得ファイル名を付加
                    if (!System.IO.File.Exists(Dest_Direc_Work)){       
                        System.IO.File.Copy(File_Work, Dest_Direc_Work, false); // ファイル複写
                    }
                }
            }
            //以下再帰的に内部フォルダ構造までコピーしてしまう
            string[] Folder_List = GetFolderName(Source_Folder_Name);// 複写元フォルダ直下のフォルダ名を取得        
            foreach (string Directory_Work in Folder_List){// 複写元フォルダの直下にある全フォルダに対して、フォルダ複写用メソッド(自メソッド)を実行
                string Dest_Direc_Work = System.IO.Path.Combine(Dest_Folder_Name, System.IO.Path.GetFileName(Directory_Work));// 複写先フォルダ名の末尾に、複写元フォルダ名を付加
                CopyFolder(Directory_Work, Dest_Direc_Work, Overwrite_Flag);// 指定フォルダ直下の各フォルダにおいて、複写処理を実行
            }
        }
    // MoveAllFileFolderファイル複写用メソッド　指定フォルダ内のファイルをすべて移動。階層下のは無視。
    // 第１引数Source_Folder_Name: 複写元フォルダ
    // 第２引数Dest_Folder_Name: 複写先フォルダ
    // 第３引数Overwrite_Flag: 複写先に既に同名ファイルが存在する場合の指示。 true:上書きする / false: 上書きしない
        public void MoveAllFileFolder(string Source_Folder_Name, string Dest_Folder_Name, bool Overwrite_Flag){
            if (!System.IO.Directory.Exists(Dest_Folder_Name)){// 複写先フォルダが存在しない場合、フォルダを作成
                System.IO.Directory.CreateDirectory(Dest_Folder_Name);// フォルダ作成
                System.IO.File.SetAttributes(Dest_Folder_Name, System.IO.File.GetAttributes(Source_Folder_Name));// 作成したフォルダに、フォルダの属性を複写
                Overwrite_Flag = true; // 複写先フォルダを作ったのだから、そこにファイルは存在しないので、以後上書きで可
            }
            string[] File_List = GetFileName(Source_Folder_Name);// 複写元フォルダ直下のファイル名を取得
            if (Overwrite_Flag){// 複写先に既に同名ファイルが存在する場合に、上書きが許可されている場合    
                foreach (string File_Work in File_List){// 複写元フォルダの直下にあるファイルを複写        
                    string Dest_Direc_Work = System.IO.Path.Combine(Dest_Folder_Name, System.IO.Path.GetFileName(File_Work));// 複写先フォルダ名の末尾に、取得ファイル名を付加
                    if (System.IO.File.Exists(Dest_Direc_Work)){//あるか確認
                        System.IO.File.Delete(Dest_Direc_Work);//あったら消す
                    }
                    System.IO.File.Move(File_Work, Dest_Direc_Work);// ファイル移動
                }
            }else{// 複写元フォルダの直下にあるファイルを複写
                foreach (string File_Work in File_List){        
                    string Dest_Direc_Work = System.IO.Path.Combine(Dest_Folder_Name, System.IO.Path.GetFileName(File_Work));// 複写先フォルダ名の末尾に、取得ファイル名を付加
                    if (!System.IO.File.Exists(Dest_Direc_Work)){//あるか確認
                        System.IO.File.Move(File_Work, Dest_Direc_Work); //ないならファイル移動
                    }
                }
            }
        }
        private void CarmineCliAuto(string PathName) {//ハフマンテーブルの最適化によってjpgサイズを縮小
            IEnumerable<string> files=System.IO.Directory.EnumerateFiles(PathName,"*.jpg",System.IO.SearchOption.AllDirectories);//Acquire only jpg files under the path.
            if(!files.Any())
                return;
            Parallel.ForEach(files,new ParallelOptions() {MaxDegreeOfParallelism=16},f=>{//マルチスレッド化するのでファイル毎
                ExecuteAnotherApp("carmine_cli.exe","\""+f+"\"",false,true);
            });
            MoveAllFileFolder(PathName+"\\result_carmine",PathName,true);// フォルダ内の上書き複写
            if(!System.IO.Directory.Exists(PathName+"\\result_carmine"))
                return;
            System.IO.Directory.Delete(PathName+"\\result_carmine",true);
        }
        private void ExecuteAnotherApp(string FileName,string Arguments,bool UseShellExecute,bool CreateNoWindow){
            var App = new System.Diagnostics.ProcessStartInfo();
            App.FileName = FileName;
            App.Arguments = Arguments;
            App.UseShellExecute = UseShellExecute;
            App.CreateNoWindow = CreateNoWindow;    // コンソール・ウィンドウを開
            System.Diagnostics.Process AppProcess = System.Diagnostics.Process.Start(App);
            AppProcess.WaitForExit();	// プロセスの終了を待つ
        }
        private void CreateZip(string PathName) {
            string Extension=".zip";
            string FileName = "Rar.exe";
            string Arguments;
            if(radioButton3.Checked) {//winrar
                Extension=".rar";
                if(radioButton6.Checked)//non compress
                    Arguments = " a \""+PathName+".rar\" -rr5 -mt16 -m0 -ep \""+PathName+"\"";
                else//compress level max
                    Arguments = " a \""+PathName+".rar\" -rr5 -mt16 -m5 -ep \""+PathName+"\"";
                //MessageBox.Show(App.Arguments);
                /*
                a             書庫にファイルを圧縮
                rr[N]         リカバリレコードを付加
                m<0..5>       圧縮方式を指定 (0-無圧縮...5-標準...5-最高圧縮)
                mt<threads>   スレッドの数をセット
                ep            名前からパスを除外/**/
            } else {
                FileName = "7z.exe";
                if(radioButton2.Checked) Extension=".7z";
                if(radioButton5.Checked)
                    Arguments = "a \""+PathName+Extension+"\" -mmt=on \""+PathName+"\\*\"";
                else if(radioButton4.Checked)
                    Arguments = "a \""+PathName+Extension+"\" -mmt=on -mx9 \""+PathName+"\\*\"";
                else
                    Arguments = "a \""+PathName+Extension+"\" -mmt=on -mx0 \""+PathName+"\\*\"";
            }
            ExecuteAnotherApp(FileName,Arguments,false,true);
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
                string NewFileName=GetNumberOnlyPath(PathName)+Extension;
                if (System.IO.File.Exists(NewFileName))//重複
                    return false;
                File.Move(PathName+Extension,NewFileName);
                richTextBox1.Text+=NewFileName+"\n";//Show path
                return true;
        }
        private bool IsTheNumberOfFilesAppropriate(int MaxFile){
            if(MaxFile>36*25+100) {
                richTextBox1.Text+="\nMaxFile:"+MaxFile+" => over 1,000\n";
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
                if(radioButton7.Checked){
                    RemoveMarginEntry(PathName);
                }
                CarmineCliAuto(PathName);
                CreateZip(PathName);
                richTextBox1.SelectionStart = richTextBox1.Text.Length;//末尾に移動
                richTextBox1.ScrollToCaret();
                logs.TopIndex = logs.Items.Count - 1;
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