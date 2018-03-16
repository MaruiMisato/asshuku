using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Ionic.Zip;
using System.Runtime.InteropServices;
using OpenCvSharp;
using System.Drawing;
using System.Drawing.Imaging;
namespace asshuku {
    public partial class Form1:Form {
        [DllImport("7-zip32.dll",CharSet=CharSet.Ansi)]
        private static extern int SevenZip(IntPtr hWnd,string strCommandLine,StringBuilder strOutPut,uint outputSize);
        [DllImport("kernel32.dll")]
        private static extern uint GetShortPathName(string strLongPath,StringBuilder strShortPath,uint buf);
        public Form1() {
            InitializeComponent();
        }
        private void ReNameAlfaBeta(string PathName,ref IEnumerable<string> files,string[] NewFileName) {
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
        }
        private bool GetHistgramR(ref string f,int[] histgram) {
            bool isGrayScale=true;
            using(Bitmap bmp=new Bitmap(f)) {
                BitmapData data=bmp.LockBits(new Rectangle(0,0,bmp.Width,bmp.Height),ImageLockMode.ReadWrite,PixelFormat.Format32bppArgb);
                byte[] b=new byte[bmp.Width*bmp.Height*4];
                Marshal.Copy(data.Scan0,b,0,b.Length);
                for(int i=0;i<b.Length;i+=4) 
                    if(isGrayScale==false||b[i]!=b[i+1]||b[i+2]!=b[i]) { //Color images are not executed.
                        isGrayScale=false;
                        histgram[(int)((b[i]+b[i+1]+b[i+2]+0.5)/3)]++;//四捨五入
                    }else histgram[b[i]]++;
                bmp.UnlockBits(data);
            }
            return isGrayScale;
        }
        private void NoiseRemoveTwoArea(IplImage p_img,byte max) {
            using(IplImage q_img=Cv.CreateImage(Cv.GetSize(p_img),BitDepth.U8,1)) {
                unsafe {
                    byte* p=(byte*)p_img.ImageData,q=(byte*)q_img.ImageData;
                    for(int y=0;y<q_img.ImageSize;++y)q[y]=p[y]<max?(byte)0:(byte)255;//First, binarize
                    for(int y=1;y<q_img.Height-1;++y) {
                        int yoffset=(q_img.WidthStep*y);
                        for(int x=1;x<q_img.Width-1;++x) { 
                            int offset=yoffset+x;
                            if(q[offset]==0)//Count white spots around black dots
                                for(int yy=-1;yy<2;++yy) {
                                    int yyyoffset=q_img.WidthStep*(y+yy);
                                    for(int xx=-1;xx<2;++xx) if(q[yyyoffset+(x+xx)]==255)++q[offset];
                                }
                        }
                    }
                    for(int y=1;y<q_img.Height-1;++y) {//
                        int yoffset=(q_img.WidthStep*y);
                        for(int x=1;x<q_img.Width-1;++x) {
                            if(q[yoffset+x]==7)//When there are seven white spots in the periphery
                                for(int yy=-1;yy<2;++yy) {
                                    int yyyoffset = q_img.WidthStep*(y+yy);
                                    for(int xx=-1;xx<2;++xx) {
                                        int offset=yyyoffset+(x+xx);
                                        if(q[offset]==7) {//仲間 ペア
                                            p[yoffset+x]=max;//q[offset]=0;//Unnecessary 
                                            p[offset]=max;q[offset]=0;
                                            yy=1;break;
                                        } else;
                                    }
                                }
                            else if(q[yoffset+x]==8)p[yoffset+x]=max;//Independent
                        }
                    }
                }
            }
        }
        private void NoiseRemoveWhite(IplImage p_img,byte min) {//White spot will be remove
            using(IplImage q_img=Cv.CreateImage(Cv.GetSize(p_img),BitDepth.U8,1)) {
                unsafe {
                    byte* p=(byte*)p_img.ImageData,q=(byte*)q_img.ImageData;
                    for(int y=0;y<q_img.ImageSize;++y)q[y]=p[y]>min?(byte)255:(byte)0;//First, binarize
                    for(int y=1;y<q_img.Height-1;++y) {
                        int yoffset=(q_img.WidthStep*y);
                        for(int x=1;x<q_img.Width-1;++x) {
                            int offset=yoffset+x;
                            if(q[offset]==255)//Count white spots around black dots
                                for(int yy=-1;yy<2;++yy) {
                                    int yyyoffset=q_img.WidthStep*(y+yy);
                                    for(int xx=-1;xx<2;++xx) if(q[yyyoffset+(x+xx)]==0)--q[offset];
                                }
                        }
                    }
                    for(int y=1;y<q_img.Height-1;++y) {
                        int yoffset=(q_img.WidthStep*y);
                        for(int x=1;x<q_img.Width-1;++x) {
                            if(q[yoffset+x]==248)//When there are seven white spots in the periphery
                                for(int yy=-1;yy<2;++yy) {
                                    int yyyoffset = q_img.WidthStep*(y+yy);
                                    for(int xx=-1;xx<2;++xx) {
                                        int offset=yyyoffset+(x+xx);
                                        if(q[offset]==248) {//仲間 ペア
                                            p[yoffset+x]=min;//q[offset]=0;//Unnecessary 
                                            p[offset]=min;q[offset]=0;
                                            yy=1;break;
                                        } else;
                                    }
                                }
                            else if(q[yoffset+x]==247)p[yoffset+x]=min;//Independent
                        }
                    }
                }
            }
        }
        private void HiFuMiYoWhite(IplImage p_img,byte threshold,ref int hi,ref int fu,ref int mi,ref int yo) {
            unsafe {
                byte* p=(byte*)p_img.ImageData;
                for(int y=0;y<p_img.Height-4;y++) {//Y上取得
                    int l=0;
                    for(int yy=0;(l==yy)&&(yy<5);++yy) {
                        int yyyoffset=(p_img.WidthStep*(y+yy));
                        for(int x=0;x<p_img.Width-1;x++) if(p[yyyoffset+x]<threshold) { l=yy+1; break; }
                    }if(l==5) {hi=y;break;} 
                    else y+=l;
                }for(int y=p_img.Height-1;y>hi+4;--y) {//Y下取得
                    int l=0;
                    for(int yy=0;(l==yy)&&(yy>-5);--yy) {
                        int yyyoffset=(p_img.WidthStep*(y+yy));
                        for(int x=0;x<p_img.Width-1;x++)if(p[yyyoffset+x]<threshold) { l=yy-1; break; }
                    }if(l==-5) {mi=y;break;}
                    else y+=l;
                }for(int x=0;x<p_img.Width-4;x++) {//X左取得
                    int l=0;
                    for(int xx=0;(l==xx)&&(xx<5);++xx) {
                        int xxxoffset=(x+xx);
                        for(int y=hi;y<mi-1;y++) if(p[p_img.WidthStep*y+xxxoffset]<threshold) { l=xx+1; break; }
                    }if(l==5) { fu=x;break; }
                    else x+=l;
                }for(int x=p_img.Width-1;x>fu+4;--x) {//X右取得
                    int l=0;
                    for(int xx=0;(l==xx)&&(xx>-5);--xx) {
                        int xxxoffset=(x+xx);
                        for(int y=hi;y<mi;y++)if(p[p_img.WidthStep*y+xxxoffset]<threshold) { l=xx-1; break; }
                    }if(l==-5) { yo=x;break; }
                    else x+=l;
                }
            }
        }
        private void HiFuMiYoBlack(IplImage p_img,byte threshold,ref int hi,ref int fu,ref int mi,ref int yo) {
            unsafe {
                byte* p=(byte*)p_img.ImageData;
                for(int y=0;y<p_img.Height-4;y++) {//Y上取得
                    if(y==0){
                        int[] l=new int[5];
                        for(int yy = 0;(yy<5);++yy) {
                            int yyyoffset = (p_img.WidthStep*(y+yy));
                            for(int x = 0;x<p_img.Width;x++)if(p[yyyoffset+x]<threshold)++l[yy];
                        }
                        if((p_img.Width>l[0])&&(p_img.Width>l[1])&&(p_img.Width>l[2])&&(p_img.Width>l[3])&&(p_img.Width>l[4])) { hi=y; break; }
                    } else {
                        int l=0;
                        int yoffset=(p_img.WidthStep*(y+4));
                        for(int x = 0;x<p_img.Width;x++)if(p[yoffset+x]<threshold) ++l;
                        if((p_img.Width>l)) { hi=y-4; break; }
                    }
                }for(int y=p_img.Height-1;y>(hi+4);--y) {//Y下取得
                    if(y==p_img.Height-1) {
                        int[] l=new int[5];
                        for(int yy=-4;(yy<1);++yy) {
                            int yyyoffset=(p_img.WidthStep*(y+yy));
                            for(int x=0;x<p_img.Width;x++) if(p[yyyoffset+x]<threshold)++l[-yy];
                        }
                        if((p_img.Width>l[0])&&(p_img.Width>l[1])&&(p_img.Width>l[2])&&(p_img.Width>l[3])&&(p_img.Width>l[4])) { mi=y; break; }
                    } else {
                        int yoffset=(p_img.WidthStep*(y-4));
                        int l=0;
                        for(int x=0;x<p_img.Width;x++) if(p[yoffset+x]<threshold)++l;
                        if((p_img.Width>l)) { mi=y+4; break; }
                    }
                }for(int x=0;x<p_img.Width-4;x++) {//X左取得
                    if(x==0) {
                        int[] l=new int[5];
                        for(int xx=0;(xx<5);++xx) {
                            int xxxoffset=(x+xx);
                            for(int y=hi;y<mi;y++) if(p[xxxoffset+p_img.WidthStep*y]<threshold)++l[xx];
                        }
                        if(((mi-hi)>l[0])&&((mi-hi)>l[1])&&((mi-hi)>l[2])&&((mi-hi)>l[3])&&((mi-hi)>l[4])) { fu=x; break; }
                    } else {
                        int xoffset=(x+4);
                        int l=0;
                        for(int y=hi;y<mi;y++) if(p[xoffset+p_img.WidthStep*y]<threshold)++l;
                        if((mi-hi)>l) { fu=x-4; break; }
                    }
                }for(int x=p_img.Width-1;x>(fu+4);--x) {//X右取得
                    if(x==p_img.Width-1) {
                        int[] l=new int[5];
                        for(int xx=-4;(xx<1);++xx) {
                            int xxxoffset=(x+xx);
                            for(int y=hi;y<mi;y++) if(p[xxxoffset+p_img.WidthStep*y]<threshold)++l[-xx];
                        }
                        if(((mi-hi)>l[0])&&((mi-hi)>l[1])&&((mi-hi)>l[2])&&((mi-hi)>l[3])&&((mi-hi)>l[4])) { yo=x; break; }
                    } else {
                        int xoffset=(x-4);
                        int l=0;
                        for(int y=hi;y<mi;y++) if(p[xoffset+p_img.WidthStep*y]<threshold)++l;
                        if((mi-hi)>l) { yo=x+4; break; }
                    }
                }
            }
        }
        private void WhiteCut(IplImage p_img,IplImage q_img,int hi,int fu,int mi,int yo) {
            unsafe {
                byte* p=(byte*)p_img.ImageData,q=(byte*)q_img.ImageData;
                for(int y=hi;y<=mi;++y) {
                    int yoffset=(p_img.WidthStep*y),qyoffset=(q_img.WidthStep*(y-hi));
                    for(int x=fu;x<=yo;++x)q[qyoffset+(x-fu)]=p[yoffset+x];
                }
            }
        }
        private void WhiteCutColor(ref string f,IplImage g_img,IplImage q_img,int hi,int fu,int mi,int yo,byte max,byte min) {//階調値線形変換はしない 
            using(Bitmap bmp=new Bitmap(f)) {
                BitmapData data=bmp.LockBits(new Rectangle(0,0,bmp.Width,bmp.Height),ImageLockMode.ReadWrite,PixelFormat.Format32bppArgb);
                byte[] b=new byte[bmp.Width*bmp.Height*4];
                Marshal.Copy(data.Scan0,b,0,b.Length);
                unsafe {
                    byte* q=(byte*)q_img.ImageData,g=(byte*)g_img.ImageData;
                    for(int y=hi;y<=mi;++y) {
                        int yoffset=bmp.Width*4*y,qyoffset=q_img.WidthStep*(y-hi),gyoffset=g_img.WidthStep*(y);
                        for(int x=fu;x<=yo;++x) {
                            int qoffset=qyoffset+3*(x-fu);
                            if(g[gyoffset+x]==max) {//NoiseRemoveTwoAreaノイズ除去を反映させる
                                q[0+qoffset]=max;q[1+qoffset]=max;q[2+qoffset]=max;
                            } else if(g[gyoffset+x]==min) {//NoiseRemoveTwoAreaノイズ除去を反映させる
                                q[0+qoffset]=min;q[1+qoffset]=min;q[2+qoffset]=min;
                            }else{
                                int offset=yoffset+4*x;
                                q[0+qoffset]=b[0+offset];q[1+qoffset]=b[1+offset];q[2+qoffset]=b[2+offset];
                            }
                        }
                    }
                }
                bmp.UnlockBits(data);
            }
        }
        private void GetSpaces(IplImage p_img,byte threshold,int[] ly,int[] lx,ref int new_h,ref int new_w) {
            unsafe {
                byte* p=(byte*)p_img.ImageData;
                for(int y=1;y<p_img.Height;++y) {
                    int yoffset=p_img.WidthStep*y;
                    int ybefore=ly[y-1]+1;
                    int blackstone=0;
                    for(int x=0;x<p_img.Width;++x)
                        if(p[yoffset+x]>threshold)ly[y]=ybefore;//白
                        else if(++blackstone>(2)){ ly[y]=0; break; }//黒2つ以上
                }
                int w_width=(int)((p_img.Height+p_img.Width)*0.02);//(p_img.Height+p_img.Width)*0.02)は残す空白の大きさ
                for(int y=1;y<p_img.Height;++y) if(ly[y]<=w_width) { ly[y]=0; ++new_h; }//(p_img.Height+p_img.Width)*0.02)は残す空白の大きさ
                for(int x=1;x<p_img.Width;++x) {
                    int xbefore=lx[x-1]+1;
                    int blackstone=0;
                    for(int y=0;y<p_img.Height;++y)
                        if(p[p_img.WidthStep*y+x]>threshold)lx[x]=xbefore;//白
                        else if(++blackstone>(2)){ lx[x]=0; break; }//黒2つ以上
                }
                for(int x=1;x<p_img.Width;++x) if(lx[x]<=w_width) { lx[x]=0; ++new_w; }//(p_img.Height+p_img.Width)*0.02)は残す空白の大きさ
            }
        }
        private void DeleteSpaces(ref string f,IplImage p_img,byte threshold,byte min,double magnification) {//内部の空白を除去 グレイスケールのみ
            int[] ly=new int[p_img.Height],lx=new int[p_img.Width];
            int new_h=+1,new_w=+1;
            GetSpaces(p_img,threshold,ly,lx,ref new_h,ref new_w);
            using(IplImage q_img=Cv.CreateImage(new CvSize(new_w,new_h),BitDepth.U8,1)) {
                unsafe {
                    byte* q=(byte*)q_img.ImageData,p=(byte*)p_img.ImageData;
                    for(int y=0,yy=0;y<p_img.Height;++y)
                        if(ly[y]==0) {
                            int yyoffset=q_img.WidthStep*(yy++),yoffset=p_img.WidthStep*y;
                            for(int x=0,xx=0;x<p_img.Width;++x) if(lx[x]==0)q[yyoffset+(xx++)]=(byte)(magnification*(p[yoffset+x]-min));//255.99ないと255が254になる
                        }
                }Cv.SaveImage(f,q_img,new ImageEncodingParam(ImageEncodingID.PngCompression,0));
            }
        }
        private void DeleteSpacesColor(ref string f,IplImage p_img,byte threshold) {//内部の空白を除去 カラー
            int[] ly=new int[p_img.Height],lx=new int[p_img.Width];
            int new_h=+1,new_w=+1;
            using(IplImage g_img=Cv.CreateImage(new CvSize(p_img.Width,p_img.Height),BitDepth.U8,1)) {
                Cv.CvtColor(p_img,g_img,ColorConversion.BgrToGray);
                GetSpaces(g_img,threshold,ly,lx,ref new_h,ref new_w);
            }
            using(IplImage q_img=Cv.CreateImage(new CvSize(new_w,new_h),BitDepth.U8,3)) {
                unsafe {
                    byte* q=(byte*)q_img.ImageData,p=(byte*)p_img.ImageData;
                    for(int y=0,yy=0;y<p_img.Height;++y)
                        if(ly[y]==0) {
                            int yyoffset=q_img.WidthStep*(yy++),yoffset=p_img.WidthStep*y;
                            for(int x=0,xx=0;x<p_img.Width;++x) if(lx[x]==0) {
                                    int qoffset=yyoffset+3*(xx++),offset=yoffset+3*x;
                                    q[qoffset+0]=p[offset+0];q[qoffset+1]=p[offset+1];q[qoffset+2]=p[offset+2];//階調値線形変換はカラーではしない
                                }
                        }
                }Cv.SaveImage(f,q_img,new ImageEncodingParam(ImageEncodingID.PngCompression,0));
            }
        }
        private void PNGRemoveAlways(ref string f,uint n) {
            while(0!=n--) 
                using(IplImage g_img=Cv.LoadImage(f,LoadMode.GrayScale)) {
                    int[] histgram=new int[256];
                    bool isGrayScale=GetHistgramR(ref f,histgram);//bool gray->true
                    byte i=255;
                    for(int total=0;(total+=histgram[i])<g_img.ImageSize*0.6;--i);byte threshold=--i;//0.1~0.7/p_img.NChannels
                    NoiseRemoveTwoArea(g_img,255);//colorには後ほど反映させる
                    NoiseRemoveWhite(g_img,0);
                    int hi=0,fu=0,mi=g_img.Height-1,yo=g_img.Width-1;
                    HiFuMiYoWhite(g_img,threshold,ref hi,ref fu,ref mi,ref yo);
                    if((hi==0)&&(fu==0)&&(mi==g_img.Height-1)&&(yo==g_img.Width-1))HiFuMiYoBlack(g_img,127,ref hi,ref fu,ref mi,ref yo);//background black
                    if(isGrayScale)
                        using(IplImage p_img=Cv.CreateImage(new CvSize((yo-fu)+1,(mi-hi)+1),BitDepth.U8,1)) {
                            WhiteCut(g_img,p_img,hi,fu,mi,yo);
                            DeleteSpaces(ref f,p_img,threshold,0,1);//内部の空白を除去 階調値変換
                        } 
                    else
                        using(IplImage p_img=Cv.CreateImage(new CvSize((yo-fu)+1,(mi-hi)+1),BitDepth.U8,3)) {
                            WhiteCutColor(ref f,g_img,p_img,hi,fu,mi,yo,255,0);
                            DeleteSpacesColor(ref f,p_img,threshold);//内部の空白を除去
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
        private byte GetToneValueMax(int[] histgram){
            byte i=256-2;
            while(histgram[(byte)(--i+1)]==0);
            return ++i;
        }
        private byte GetToneValueMin(int[] histgram){
            byte i=1;
            while(histgram[(byte)(++i-1)]==0);
            return --i;
        }
        private void CutMarginMain(ref string f,TextWriter writerSync){
            int[] histgram=new int[256];
            bool isGrayScale=GetHistgramR(ref f,histgram);//bool gray->true
            //byte i=256-2;
            //for(/*i=256-2*/;histgram[(byte)(i+1)]==0;--i);byte ToneValueMax=++i;//(byte)がないとアスファルトでエラー
            byte ToneValueMax=GetToneValueMax(histgram);
            byte ToneValueMin=GetToneValueMin(histgram);
            //for(i=1;histgram[(byte)(i-1)]==0;++i);byte ToneValueMin=--i;//(byte)がないと豆腐でエラー
            if(ToneValueMax<=ToneValueMin)return; //<はありえない　＝　が想定される豆腐･アスファルトはスルー
            using(IplImage InputGrayImage=Cv.LoadImage(f,LoadMode.GrayScale)) {//load GrayScale
                NoiseRemoveTwoArea(InputGrayImage,ToneValueMax);//colorには後ほど反映させる
                NoiseRemoveWhite(InputGrayImage,ToneValueMin);
                byte i=255;
                for(int total=0;(total+=histgram[i])<InputGrayImage.ImageSize*0.6;--i);byte threshold=--i;//0.1~0.7/p_img.NChannels
                int hi=0,fu=0,mi=InputGrayImage.Height-1,yo=InputGrayImage.Width-1;
                HiFuMiYoWhite(InputGrayImage,threshold,ref hi,ref fu,ref mi,ref yo);
                if((hi==0)&&(fu==0)&&(mi==InputGrayImage.Height-1)&&(yo==InputGrayImage.Width-1))HiFuMiYoBlack(InputGrayImage,(byte)((ToneValueMax-ToneValueMin)>>1),ref hi,ref fu,ref mi,ref yo);//background black
                writerSync.WriteLine(f+"\n\tthreshold="+threshold+":ToneValueMin="+ToneValueMin+":ToneValueMax="+ToneValueMax+":hi="+hi+":fu="+fu+":mi="+mi+":yo="+yo+"\n\t("+InputGrayImage.Width+","+InputGrayImage.Height+")\n\t("+((yo-fu)+1)+","+((mi-hi)+1)+")");
                if(isGrayScale){
                    using(IplImage OutputCutImage=Cv.CreateImage(new CvSize((yo-fu)+1,(mi-hi)+1),BitDepth.U8,1)) {
                        WhiteCut(InputGrayImage,OutputCutImage,hi,fu,mi,yo);
                        DeleteSpaces(ref f,OutputCutImage,(byte)(threshold*(255.99/(ToneValueMax-ToneValueMin))),ToneValueMin,255.99/(ToneValueMax-ToneValueMin));//内部の空白を除去 階調値変換
                    } 
                }else{
                    using(IplImage OutputCutImage=Cv.CreateImage(new CvSize((yo-fu)+1,(mi-hi)+1),BitDepth.U8,3)) {
                        WhiteCutColor(ref f,InputGrayImage,OutputCutImage,hi,fu,mi,yo,ToneValueMax,ToneValueMin);
                        DeleteSpacesColor(ref f,OutputCutImage,(byte)(threshold*(255.99/(ToneValueMax-ToneValueMin))));//内部の空白を除去
                    }
                }
            }
            PNGRemoveAlways(ref f,4);//n回繰り返す
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
                } zip.Save(PathName+".zip");//Create a ZIP archive
            } else {
                string Extension="zip";
                if(radioButton2.Checked) Extension="7z";
                StringBuilder strShortPath=new StringBuilder(1024);
                GetShortPathName(PathName,strShortPath,1024);
                richTextBox1.Text+="\n"+PathName+"."+Extension+"\n";
                if(radioButton5.Checked) SevenZip(this.Handle,"a -hide -t"+Extension+" \""+PathName+"."+Extension+"\" "+strShortPath+"\\*",new StringBuilder(1024),1024);//Create a ZIP archive
                else if(radioButton4.Checked) SevenZip(this.Handle,"a -hide -t"+Extension+" \""+PathName+"."+Extension+"\" "+strShortPath+"\\* -mx9",new StringBuilder(1024),1024);
                else SevenZip(this.Handle,"a -hide -t"+Extension+" \""+PathName+"."+Extension+"\" "+strShortPath+"\\* -mx0",new StringBuilder(1024),1024);//Create a ZIP archive
            }
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
            fbd.ShowNewFolderButton=true;//Allow users to create new folders
            fbd.ShowDialog(this);//Display a dialog
            richTextBox1.Text=fbd.SelectedPath;//Show path
            Clipboard.SetFileDropList(new System.Collections.Specialized.StringCollection() { fbd.SelectedPath });//コピーするファイルのパスをStringCollectionに追加する. Copy to clipboard
        }
    }
}
