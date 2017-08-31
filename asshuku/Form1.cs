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
            bool grayscale=true;
            using(Bitmap bmp=new Bitmap(f)) {
                BitmapData data=bmp.LockBits(new Rectangle(0,0,bmp.Width,bmp.Height),ImageLockMode.ReadWrite,PixelFormat.Format32bppArgb);
                byte[] buf=new byte[bmp.Width*bmp.Height*4];
                Marshal.Copy(data.Scan0,buf,0,buf.Length);
                for(int i=0;i<buf.Length;i+=4) {
                    if(grayscale==false||buf[i]!=buf[i+1]||buf[i+2]!=buf[i]) { //Color images are not executed.
                        grayscale=false;
                        histgram[(buf[i]+buf[i+1]+buf[i+2])/3]++;
                    }
                    else histgram[buf[i]]++;
                }//Marshal.Copy(buf,0,data.Scan0,buf.Length);
                bmp.UnlockBits(data);
            }
            return grayscale;
        }
        private void NoiseRemoveTwoArea(IplImage p_img,byte max) {
            using(IplImage q_img=Cv.CreateImage(Cv.GetSize(p_img),BitDepth.U8,p_img.NChannels)) {
                unsafe {
                    byte* p=(byte*)p_img.ImageData,q=(byte*)q_img.ImageData;
                    for(int y=0;y<q_img.ImageSize;y+=p_img.NChannels)q[y]=p[y]<max?(byte)0:(byte)255;//First, binarize
                    for(int y=1;y<q_img.Height-1;y++) {
                        int yoffset=(q_img.WidthStep*y);
                        for(int x=1;x<q_img.Width-1;x++)
                            if(q[yoffset+p_img.NChannels*x]==0)//Count white spots around black dots
                                for(int yy=-1;yy<2;++yy) {
                                    int yyyoffset=q_img.WidthStep*(y+yy);
                                    for(int xx=-1;xx<2;++xx) if(q[yyyoffset+(x+xx)*p_img.NChannels]==255) ++q[yoffset+p_img.NChannels*x];
                                }
                    }
                    for(int y=1;y<q_img.Height-1;y++) {
                        int yoffset=(q_img.WidthStep*y);
                        for(int x=1;x<q_img.Width-1;x++) {
                            if(q[yoffset+p_img.NChannels*x]==7)//When there are seven white spots in the periphery
                                for(int yy=-1;yy<2;++yy) {
                                    int yyyoffset = q_img.WidthStep*(y+yy);
                                    for(int xx=-1;xx<2;++xx) {
                                        int offset=yyyoffset+(x+xx)*p_img.NChannels;
                                        if(q[offset]==7) {//仲間 ペア
                                            p[yoffset+p_img.NChannels*x]=max;//q[yoffset+p_img.NChannels*x]=6;//Unnecessary 
                                            p[offset]=max;q[offset]=6;
                                            yy=1;break;
                                        } else;
                                    }
                                }
                            else if(q[yoffset+p_img.NChannels*x]==8)p[yoffset+p_img.NChannels*x]=max;//Independent
                        }
                    }
                }
            }
        }
        private void HiFuMiYoWhite(IplImage p_img,byte threshold,ref int hi,ref int fu,ref int mi,ref int yo) {
            unsafe {
                byte* p=(byte*)p_img.ImageData;
                for(int y=0;y<p_img.Height-1;y++) {//Y上取得
                    int l=0;
                    for(int yy=0;((l==yy)&&(yy<5)&&(y+yy)<p_img.Height-1);++yy) {
                        int yyyoffset=(p_img.WidthStep*(y+yy));
                        for(int x=0;x<p_img.Width-1;x++) {
                            int offset=yyyoffset+p_img.NChannels*x;
                            if(p[offset]<threshold)if((p[offset+p_img.WidthStep]<threshold)||(p[offset+p_img.NChannels]<threshold)||(p[offset+p_img.NChannels+p_img.WidthStep]<threshold)) {l=yy+1;break;}
                        }
                    }if(l==5) {hi=y;break;} 
                    else y+=l;
                }
                for(int y=p_img.Height-1;y>hi;--y) {//Y下取得
                    int l=0;
                    for(int yy=0;((l==yy)&&(yy>-5)&&(y+yy)>hi);--yy) {
                        int yyyoffset=(p_img.WidthStep*(y+yy));
                        for(int x=0;x<p_img.Width-1;x++) {
                            int offset=yyyoffset+p_img.NChannels*x;
                            if(p[offset]<threshold) if((p[(offset-p_img.WidthStep)]<threshold)||(p[offset+p_img.NChannels]<threshold)||(p[(offset+p_img.NChannels)-p_img.WidthStep]<threshold)) {l=yy-1;break;}
                        }
                    }if(l==-5) {mi=y;break;}
                    else y+=l;
                }
                for(int x=0;x<p_img.Width-1;x++) {//X左取得
                    int l=0;
                    for(int xx=0;((l==xx)&&(xx<5)&&(x+xx)<p_img.Width-1);++xx) {
                        int xxxoffset=p_img.NChannels*(x+xx);
                        for(int y=hi;y<mi-1;y++) {
                            int offset=p_img.WidthStep*y+xxxoffset;
                            if(p[offset]<threshold) if((p[offset+p_img.WidthStep]<threshold)||(p[offset+p_img.NChannels]<threshold)||(p[offset+p_img.NChannels+p_img.WidthStep]<threshold)) {l=xx+1;break;}
                        }
                    }if(l==5) { fu=x;break; }
                    else x+=l;
                }
                for(int x=p_img.Width-1;x>fu;--x) {//X右取得
                    int l=0;
                    for(int xx=0;((l==xx)&&(xx>-5)&&(x+xx)>fu);--xx) {
                        int xxxoffset=p_img.NChannels*(x+xx);
                        for(int y=hi;y<mi;y++) {
                            int offset=p_img.WidthStep*y+xxxoffset;
                            if(p[offset]<threshold) if((p[offset+p_img.WidthStep]<threshold)||(p[(offset-p_img.NChannels)]<threshold)||(p[(offset-p_img.NChannels)+p_img.WidthStep]<threshold)) {
                                    l=xx-1;
                                    break;
                                }
                        }
                    }if(l==-5) { yo=x;break; }
                    else x+=l;
                }
            }
        }
        private void HiFuMiYoBlack(IplImage p_img,byte threshold,ref int hi,ref int fu,ref int mi,ref int yo) {
            unsafe {
                byte* p=(byte*)p_img.ImageData;
                for(int y=0;y<p_img.Height-1-4;y++) {//Y上取得
                    if(y==0){
                        int[] l=new int[5];
                        for(int yy = 0;((yy<5)&&(y+yy)<p_img.Height-1);++yy) {
                            int yyyoffset = (p_img.WidthStep*(y+yy));
                            for(int x = 0;x<p_img.Width;x++)if(p[yyyoffset+p_img.NChannels*x]<threshold)++l[yy];
                        }
                        if((p_img.Width!=l[0])||(p_img.Width!=l[1])||(p_img.Width!=l[2])||(p_img.Width!=l[3])||(p_img.Width!=l[4])) {hi=y;break;}
                    } else {
                        int l=0;
                        int yyyoffset=(p_img.WidthStep*(y+4));
                        for(int x = 0;x<p_img.Width;x++)if(p[yyyoffset+p_img.NChannels*x]<threshold) ++l;
                        if((p_img.Width!=l)) {hi=y; break;}
                    }
                }
                for(int y=p_img.Height-1;y>(hi+4);--y) {//Y下取得
                    if(y==p_img.Height-1) {
                        int[] l=new int[5];
                        for(int yy=-4;((yy<1)&&(y+yy)>hi);++yy) {
                            int yyyoffset=(p_img.WidthStep*(y+yy));
                            for(int x=0;x<p_img.Width;x++) if(p[yyyoffset+p_img.NChannels*x]<threshold)++l[-yy];
                        }
                        if((p_img.Width!=l[0])||(p_img.Width!=l[1])||(p_img.Width!=l[2])||(p_img.Width!=l[3])||(p_img.Width!=l[4])) {mi=y;break;}
                    } else {
                        int yyyoffset=(p_img.WidthStep*(y-4));
                        int l=0;
                        for(int x=0;x<p_img.Width;x++) if(p[yyyoffset+p_img.NChannels*x]<threshold)++l;
                        if((p_img.Width!=l)) {mi=y;break;}
                    }
                }
                for(int x=0;x<p_img.Width-1-4;x++) {//X左取得
                    if(x==0) {
                        int[] l=new int[5];
                        for(int xx=0;((xx<5)&&(x+xx)<p_img.Width-1);++xx) {
                            int xxxoffset=p_img.NChannels*(x+xx);
                            for(int y=hi;y<mi;y++) if(p[xxxoffset+p_img.WidthStep*y]<threshold)++l[xx];
                        }
                        if((mi-hi)!=l[0]&&(mi-hi)!=l[1]&&(mi-hi)!=l[2]&&(mi-hi)!=l[3]&&(mi-hi)!=l[4]) {fu=x;break;}
                    } else {
                        int xxxoffset=p_img.NChannels*(x+4);
                        int l=0;
                        for(int y=hi;y<mi;y++) if(p[xxxoffset+p_img.WidthStep*y]<threshold)++l;
                        if((mi-hi)!=l) {fu=x;break;}
                    }
                }
                for(int x=p_img.Width-1;x>(fu+4);--x) {//X右取得
                    if(x==p_img.Width-1) {
                        int[] l=new int[5];
                        for(int xx=-4;((xx<0)&&(x+xx)>fu);++xx) {
                            int xxxoffset=p_img.NChannels*(x+xx);
                            for(int y=hi;y<mi;y++) if(p[xxxoffset+p_img.WidthStep*y]<threshold)++l[-xx];
                        }
                        if((mi-hi)!=l[0]&&(mi-hi)!=l[1]&&(mi-hi)!=l[2]&&(mi-hi)!=l[3]&&(mi-hi)!=l[4]) {yo=x;break;}
                    } else {
                        int xxxoffset=p_img.NChannels*(x-4);
                        int l=0;
                        for(int y=hi;y<mi;y++) if(p[xxxoffset+p_img.WidthStep*y]<threshold)++l;
                        if((mi-hi)!=l) {yo=x;break;}
                    }
                }
            }
        }
         
        private void WhiteCut(ref string f,IplImage p_img,IplImage q_img,int hi,int fu,int mi,int yo,byte min,byte range) {
                unsafe {
                    byte* p=(byte*)p_img.ImageData,q=(byte*)q_img.ImageData;
                    using(System.Drawing.Image img=System.Drawing.Image.FromFile(f)) {
                        if(System.Drawing.Image.GetPixelFormatSize(img.PixelFormat)<8) {//1pixel当たり4,2,1bitの明らかに最適化済みの画像は，階調値変換が不要
                            for(int y=hi;y<=mi;++y) {
                                int yoffset=(p_img.WidthStep*y),qyoffset=(q_img.WidthStep*(y-hi));
                                for(int x=fu;x<=yo;++x)q[qyoffset+(x-fu)]=p[yoffset+p_img.NChannels*x];
                            }
                        } else 
                            for(int y=hi;y<=mi;++y) {
                                int yoffset=(p_img.WidthStep*y),qyoffset=(q_img.WidthStep*(y-hi));
                                for(int x=fu;x<=yo;++x)q[qyoffset+(x-fu)]=(byte)((255.99/range)*(p[yoffset+p_img.NChannels*x]-min));//255.99ないと255が254になる
                            }
                    }
                }
        }
        /*private void WhiteCut(ref string f,IplImage p_img,int hi,int fu,int mi,int yo,byte min,byte range) {
            using(IplImage q_img=Cv.CreateImage(new CvSize((yo-fu)+1,(mi-hi)+1),BitDepth.U8,1)) {
                unsafe {
                    byte* p=(byte*)p_img.ImageData,q=(byte*)q_img.ImageData;
                    using(System.Drawing.Image img=System.Drawing.Image.FromFile(f)) {
                        if(System.Drawing.Image.GetPixelFormatSize(img.PixelFormat)<8) {//1pixel当たり4,2,1bitの明らかに最適化済みの画像は，階調値変換が不要
                            for(int y=hi;y<=mi;++y) {
                                int yoffset=(p_img.WidthStep*y),qyoffset=(q_img.WidthStep*(y-hi));
                                for(int x=fu;x<=yo;++x)
                                    q[qyoffset+(x-fu)]=p[yoffset+p_img.NChannels*x];
                            }
                        } else
                            for(int y=hi;y<=mi;++y) {
                                int yoffset=(p_img.WidthStep*y),qyoffset=(q_img.WidthStep*(y-hi));
                                for(int x=fu;x<=yo;++x)
                                    q[qyoffset+(x-fu)]=(byte)((255.99/range)*(p[yoffset+p_img.NChannels*x]-min));//255.99ないと255が254になる
                            }
                    }
                }
                Cv.SaveImage(f,q_img,new ImageEncodingParam(ImageEncodingID.PngCompression,0));
            }
        }       
        private void WhiteCutColor(ref string f,int hi,int fu,int mi,int yo,byte min) {//階調値線形変換は現状しない
            using(IplImage q_img=Cv.CreateImage(new CvSize((yo-fu)+1,(mi-hi)+1),BitDepth.U8,3)) {
                using(Bitmap bmp=new Bitmap(f)) {
                    BitmapData data=bmp.LockBits(new Rectangle(0,0,bmp.Width,bmp.Height),ImageLockMode.ReadWrite,PixelFormat.Format32bppArgb);
                    byte[] buf=new byte[bmp.Width*bmp.Height*4];
                    Marshal.Copy(data.Scan0,buf,0,buf.Length);
                    unsafe {
                        byte* q=(byte*)q_img.ImageData;
                        for(int y=hi;y<=mi;++y) {
                            int yoffset=bmp.Width*4*y,qyoffset=q_img.WidthStep*(y-hi);
                            for(int x=fu;x<=yo;++x) {
                                int offset=yoffset+4*x,qoffset=qyoffset+3*(x-fu);
                                q[0+qoffset]=buf[0+offset];
                                q[1+qoffset]=buf[1+offset];
                                q[2+qoffset]=buf[2+offset];
                            }
                        }
                    }bmp.UnlockBits(data);
                }Cv.SaveImage(f,q_img,new ImageEncodingParam(ImageEncodingID.PngCompression,0));
            }
        }/**/
        private void WhiteCutColor(ref string f,IplImage q_img,int hi,int fu,int mi,int yo,byte min) {//階調値線形変換は現状しない
            using(Bitmap bmp=new Bitmap(f)) {
                BitmapData data=bmp.LockBits(new Rectangle(0,0,bmp.Width,bmp.Height),ImageLockMode.ReadWrite,PixelFormat.Format32bppArgb);
                byte[] buf=new byte[bmp.Width*bmp.Height*4];
                Marshal.Copy(data.Scan0,buf,0,buf.Length);
                unsafe {
                    byte* q=(byte*)q_img.ImageData;
                    for(int y=hi;y<=mi;++y) {
                        int yoffset=bmp.Width*4*y,qyoffset=q_img.WidthStep*(y-hi);
                        for(int x=fu;x<=yo;++x) {
                            int offset=yoffset+4*x,qoffset=qyoffset+3*(x-fu);
                            q[0+qoffset]=buf[0+offset];
                            q[1+qoffset]=buf[1+offset];
                            q[2+qoffset]=buf[2+offset];
                        }
                    }
                }
                bmp.UnlockBits(data);
            }
        }
        private void PNGRemove(string PathName) {
            IEnumerable<string> files=System.IO.Directory.EnumerateFiles(PathName,"*.png",System.IO.SearchOption.AllDirectories);//Acquire all files under the path.
            System.Diagnostics.Stopwatch sw=new System.Diagnostics.Stopwatch();
            sw.Start();
            Parallel.ForEach(files,new ParallelOptions(){MaxDegreeOfParallelism=4},f=>{//Specify the number of concurrent threads(The number of cores is reasonable).
                using(IplImage src_img=Cv.LoadImage(f,LoadMode.AnyDepth)) { 
                    int[] histgram=new int[256];
                    bool grayscale=GetHistgramR(ref f,histgram);//bool gray->true
                    byte i=255;
                    for(int total=0;total<src_img.ImageSize*0.6;--i)total+=histgram[i];//0.1~0.7/p_img.NChannels
                    byte threshold=i;
                    for(i=256-2;histgram[(byte)(i+1)]==0;--i);byte max=++i;//(byte)がないとアスファルトでエラー
                    for(i=1;histgram[(byte)(i-1)]==0;++i);byte min=--i;//(byte)がないと豆腐でエラー
                    if(max>min) {//豆腐･アスファルトはスルー
                        int hi=0,fu=0,mi=src_img.Height-1,yo=src_img.Width-1;
                        if(grayscale) {
                            NoiseRemoveTwoArea(src_img,max);
                            HiFuMiYoWhite(src_img,threshold,ref hi,ref fu,ref mi,ref yo);
                            //logs.Items.Add(f+":th="+threshold+":min="+min+":max="+max+":hi="+hi+":fu="+fu+":mi="+mi+":yo="+yo);
                            if((hi==0)&&(fu==0)&&(mi==src_img.Height-1)&&(yo==src_img.Width-1))HiFuMiYoBlack(src_img,(byte)((max-min)>>1),ref hi,ref fu,ref mi,ref yo);//background black
                            using(IplImage q_img=Cv.CreateImage(new CvSize((yo-fu)+1,(mi-hi)+1),BitDepth.U8,1)) {
                                WhiteCut(ref f,src_img,q_img,hi,fu,mi,yo,min,max-=min);
                                Cv.SaveImage(f,q_img,new ImageEncodingParam(ImageEncodingID.PngCompression,0));
                            }
                        } else {
                            using(IplImage gry_img=Cv.LoadImage(f,LoadMode.GrayScale)) {
                                NoiseRemoveTwoArea(gry_img,max);
                                HiFuMiYoWhite(gry_img,threshold,ref hi,ref fu,ref mi,ref yo);
                                //logs.Items.Add(f+":th="+threshold+":min="+min+":max="+max+":hi="+hi+":fu="+fu+":mi="+mi+":yo="+yo);
                                if((hi==0)&&(fu==0)&&(mi==gry_img.Height-1)&&(yo==gry_img.Width-1))HiFuMiYoBlack(gry_img,(byte)((max-min)>>1),ref hi,ref fu,ref mi,ref yo);//background black
                                using(IplImage q_img=Cv.CreateImage(new CvSize((yo-fu)+1,(mi-hi)+1),BitDepth.U8,3)) {
                                    WhiteCutColor(ref f,q_img,hi,fu,mi,yo,min);
                                    Cv.SaveImage(f,q_img,new ImageEncodingParam(ImageEncodingID.PngCompression,0));
                                }
                            }
                        }
                    } 
                }
            });
            sw.Stop();
            richTextBox1.Text+=("\nWhiteRemove:"+sw.Elapsed);
            sw.Reset();sw.Start();
            System.Diagnostics.Process p=new System.Diagnostics.Process();//Create a Process object
            /*p.StartInfo.FileName=System.Environment.GetEnvironmentVariable("ComSpec");//ComSpec(cmd.exe)のパスを取得して、FileNameプロパティに指定
            p.StartInfo.WindowStyle=System.Diagnostics.ProcessWindowStyle.Hidden;//HiddenMaximizedMinimizedNormal
            Parallel.ForEach(files,new ParallelOptions(){MaxDegreeOfParallelism=4},f=>{
                p.StartInfo.Arguments="/c pngout "+f;//By default, PNGOUT will not overwrite a PNG file if it was not able to compress it further.
                p.Start();p.WaitForExit();//起動
            });
            p.Close();/**/
            sw.Stop();
            richTextBox1.Text+=("\npngout:"+sw.Elapsed);
        }
        private void button1_Click(object sender,EventArgs e) {
            if(Clipboard.ContainsFileDropList()) {//Check if clipboard has file drop format data. 取得できなかったときはnull listBox1.Items.Clear();
                System.Collections.Specialized.StringCollection filespath=Clipboard.GetFileDropList();//Get filepath from clipboard
                foreach(string PathName in filespath) {//Enumerate acquired paths
                    logs.Items.Add(PathName);
                    richTextBox1.Text+=PathName;//Show path
                    IEnumerable<string> files=System.IO.Directory.EnumerateFiles(PathName,"*",System.IO.SearchOption.AllDirectories);//Acquire all files under the path.
                    string[] AllOldFileName=new string[System.IO.Directory.GetFiles(PathName,"*",SearchOption.TopDirectoryOnly).Length];//36*25+100
                    int MaxFile=0;
                    foreach(string f in files) {
                        FileInfo file=new FileInfo(f);
                        if(file.Extension==".db"||file.Extension==".ini") file.Delete();//Disposal of garbage
                        else AllOldFileName[MaxFile++]=f;
                    }
                    if(MaxFile>(36*25)+100) {
                        richTextBox1.Text+="\nMaxFile:"+MaxFile+" => over 1,000";
                        break;
                    }
                    richTextBox1.Text+="\nMaxFile:"+MaxFile;
                    for(int i=MaxFile-1;i>=0;--i) {//尻からリネーム
                        FileInfo file=new FileInfo(AllOldFileName[i]);
                        while((file.Name.Length-file.Extension.Length)<3)file.MoveTo((PathName+"/0"+file.Name));//0->000  1000枚までしか無理 7zは650枚
                        if((file.Name[0]!='z'))file.MoveTo((PathName+"/z"+file.Name));//000->z000
                    }
                    string[] NewFileName=new string[MaxFile];
                    if(radioButton2.Checked==true&&MaxFile<=26*25) {//7zip under 26*25=650
                        int MaxRoot=(int)Math.Sqrt(MaxFile)+1;
                        richTextBox1.Text+="\nroot MaxRoot"+MaxRoot;
                        for(int i=0;i<NewFileName.Length;++i)NewFileName[i]=(char)((i/MaxRoot)+'a')+((char)(i%MaxRoot+'a')).ToString();//26*25  36*35mezasu
                    } else if(MaxFile<35) {//一桁で1-y
                        for(int i=0;(i<NewFileName.Length)&&(i<10);++i)NewFileName[i]=i.ToString();//0 ~ 9
                        for(int i=10;i<NewFileName.Length;++i)NewFileName[i]=((char)((i-10)+'a')).ToString();//a~y
                    } else {//zip under 36*25+100=1000
                        char[] y1=new char[36];
                        for(int i=0;i<10;++i)y1[i]=(char)(i+'0');//0 ~ 9
                        for(int i=10;i<y1.Length;++i)y1[i]=(char)(i-10+'a');//a~y
                        for(int i=0;(i<NewFileName.Length)&&(i<100);++i)NewFileName[i]=i.ToString();//0 ~ 99 zipではこの法が軽い
                        for(int i=100;i<NewFileName.Length;++i)NewFileName[i]+=(char)(((i-100)/36)+'a')+(y1[(i-100)%36]).ToString();
                    }
                    ReNameAlfaBeta(PathName,ref files,NewFileName);
                    if(radioButton7.Checked==true)PNGRemove(PathName);
                    if(radioButton3.Checked==true) {//Ionic.Zip
                        Ionic.Zip.ZipFile zip=new Ionic.Zip.ZipFile();//Create a ZIP archive
                        if(radioButton4.Checked==true)zip.CompressionLevel=Ionic.Zlib.CompressionLevel.Level9;//max
                        else if(radioButton5.Checked==true)zip.CompressionLevel=Ionic.Zlib.CompressionLevel.Default;//Default
                        else zip.CompressionLevel=Ionic.Zlib.CompressionLevel.None;
                        foreach(string f in files) {
                            ZipEntry entry=zip.AddFile(f);//Add a file
                            entry.FileName=new FileInfo(f).Name;
                        } 
                        zip.Save(PathName+".zip");//Create a ZIP archive
                    } else {
                        string Extension="zip";
                        if(radioButton2.Checked==true)Extension="7z";
                        StringBuilder strShortPath=new StringBuilder(1024);
                        GetShortPathName(PathName,strShortPath,1024);
                        richTextBox1.Text+="\n"+PathName+"."+Extension+"\n";
                        if(radioButton5.Checked==true)      SevenZip(this.Handle,"a -hide -t"+Extension+" \""+PathName+"."+Extension+"\" "+strShortPath+"\\*",new StringBuilder(1024),1024);//Create a ZIP archive
                        else if(radioButton4.Checked==true) SevenZip(this.Handle,"a -hide -t"+Extension+" \""+PathName+"."+Extension+"\" "+strShortPath+"\\* -mx9",new StringBuilder(1024),1024);
                        else if(radioButton6.Checked==true) SevenZip(this.Handle,"a -hide -t"+Extension+" \""+PathName+"."+Extension+"\" "+strShortPath+"\\* -mx0",new StringBuilder(1024),1024);//Create a ZIP archive
                    }
                }
            } else MessageBox.Show("Please select folders.");
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
