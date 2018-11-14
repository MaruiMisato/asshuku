using OpenCvSharp;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
public class Image{
    public static unsafe byte GetPixel(IplImage src_img,int x,int y){
        byte* src=(byte*)src_img.ImageData;
        return src[src_img.WidthStep*y+x];
    }
    public static unsafe  bool SetPixel(IplImage src_img,int x,int y,byte PixelValue){
        byte* src=(byte*)src_img.ImageData;
        src[src_img.WidthStep*y+x]=PixelValue;
        return true;
    }
    public static int GetShortSide(IplImage p_img){
        return p_img.Width>p_img.Height?p_img.Width:p_img.Height;
    }
    public static int GetLongSide(IplImage p_img){
        return p_img.Width>p_img.Height?p_img.Height:p_img.Width;
    }
    private static byte CheckRange2Byte(int ByteValue){
        return(byte)(ByteValue>255?255:ByteValue<0?0:ByteValue);
    }
    private static byte CheckRange2Byte(double ByteValue){
        return(byte)(ByteValue>255?255:ByteValue<0?0:ByteValue);
    }
    public static unsafe byte GetToneValueMax(IplImage p_img) {
        byte ToneValueMax=0;
        byte* p=(byte*)p_img.ImageData;
        for(int y=0;y<p_img.ImageSize;++y) 
            ToneValueMax = p[y]>ToneValueMax ? p[y]:ToneValueMax;
        return ToneValueMax;
    }        
    public static unsafe byte GetToneValueMin(IplImage p_img) {
        byte ToneValueMin=255;
        byte* p=(byte*)p_img.ImageData;
        for(int y=0;y<p_img.ImageSize;++y)
            ToneValueMin = p[y]<ToneValueMin ? p[y]:ToneValueMin;
        return ToneValueMin;
    }
    public static byte GetToneValueMax(int[] Histgram){
        int i=255;
        while(Histgram[i--]==0);
        return (byte)++i;
    }
    public static byte GetToneValueMin(int[] Histgram){
        int i=0;
        while(Histgram[i++]==0);
        return (byte)--i;
    }             
    public static int GetHistgramR(ref string f,int[] Histgram) {
        int Channel=Is.GrayScale;//1:gray,3:bgr color
        Bitmap bmp=new Bitmap(f);
        BitmapData data=bmp.LockBits(new Rectangle(0,0,bmp.Width,bmp.Height),ImageLockMode.ReadWrite,PixelFormat.Format32bppArgb);
        byte[] b=new byte[bmp.Width*bmp.Height*4];
        Marshal.Copy(data.Scan0,b,0,b.Length);
        for(int i=0;i<b.Length;i+=4) 
            if(Channel==Is.Color||b[i]!=b[i+1]||b[i+2]!=b[i]){//Color images are not executed.
                Channel=Is.Color;
                Histgram[(int)((b[i]+b[i+1]+b[i+2]+0.5)/3)]++;//四捨五入
            }else Histgram[b[i]]++;
        bmp.UnlockBits(data);
        bmp.Dispose();
        return Channel;
    }
    public class ToneValue{
        public byte Max{get;set;}
        public byte Min{get;set;}
    }
    public static unsafe void Transform2Linear(IplImage p_img,ToneValue ImageToneValue) {//階調値の線形変換 グレイスケールのみ
        double magnification=255.99/(ImageToneValue.Max-ImageToneValue.Min);//255.99ないと255が254になる
        byte* p=(byte*)p_img.ImageData;
        for(int y=0;y<p_img.ImageSize;++y) 
            p[y]=Image.CheckRange2Byte(magnification*(p[y]-ImageToneValue.Min));
    }
    public class Filter{
        
        private static byte GetBucketMedianAscendingOrder(int[,] Bucket, int Median,int x){
            byte YIndex=0;//256 探索範囲の最小値を探す　
            int ScanHalf=0;
            while((ScanHalf+=Bucket[x,YIndex++])<Median);//Underflow
            return --YIndex;
        }/* */
        private static byte GetBucketMedianDescendingOrder(int[,] Bucket, int Median,int x){
            byte YIndex=0;//256 探索範囲の最小値を探す　
            int ScanHalf=0;
            while((ScanHalf+=Bucket[x,--YIndex])<Median);//Underflow
            return YIndex;
        }        
        /*private static bool GetBucketMedianAscendingOrder(int[] Bucket, int Median,ref byte MedianValue){
            int YIndex=0;//256 探索範囲の最小値を探す　
            int ScanHalf=0;
            while((ScanHalf+=Bucket[YIndex++])<Median)//Underflow
                if(YIndex>255)return false;
            MedianValue=(byte)(--YIndex);
            return true;
        }
        private static bool GetBucketMedianDescendingOrder(int[] Bucket, int Median,ref byte MedianValue){
            int YIndex=255;//256 探索範囲の最小値を探す　
            int ScanHalf=0;
            while((ScanHalf+=Bucket[YIndex--])<Median)//Underflow
                if(YIndex<0)return false;
            MedianValue =(byte)(++YIndex);
            return true;
        }/* */
        private static bool GetBucketMedianAscendingOrder(int[] Bucket, int Median,ref byte MedianValue){
            int YIndex=-1;//256 探索範囲の最小値を探す　
            for(int ScanHalf=0;ScanHalf<Median;ScanHalf+=Bucket[YIndex]){
                if(++YIndex>255)return false;
                else if(Bucket[YIndex]<0)return false;
            }
            MedianValue=(byte)(YIndex);
            return true;
        }/* */
        
        private static bool GetBucketMedianDescendingOrder(int[] Bucket, int Median,ref byte MedianValue){
            int YIndex=256;//256 探索範囲の最小値を探す　
            for(int ScanHalf=0;ScanHalf<Median;ScanHalf+=Bucket[YIndex]){
                if(--YIndex<0)return false;
                else if(Bucket[YIndex]<0)return false;
            }
            MedianValue =(byte)(YIndex);
            return true;
        }
        private static byte GetBucketMedianDescendingOrder(int[] Bucket, int Median){
            byte YIndex=0;//256 探索範囲の最小値を探す　
            int ScanHalf=0;
            while((ScanHalf+=Bucket[--YIndex])<Median);//Underflow
            return YIndex;
        }
        private static byte GetBucketMedianAscendingOrder(int[] Bucket, int Median){
            byte YIndex=0;//256 探索範囲の最小値を探す　
            int ScanHalf=0;
            while((ScanHalf+=Bucket[YIndex++])<Median);//Underflow
            return --YIndex;
        }/* */
        //src_img:入出力
        public static bool FastestMedian(IplImage src_img,int n){
            if((n&1)==0)return false;//偶数はさいなら
            IplImage dst_img = Cv.CreateImage(src_img.GetSize(), BitDepth.U8, 1);
            FastestMedian(src_img,dst_img,n);
            Cv.Copy(dst_img,src_img);//dst_img->src_img
            Cv.ReleaseImage(dst_img);
            return true;
        }
        private static unsafe bool SelectAscendingDescendingOrder(IplImage src_img){
            byte* src=(byte*)src_img.ImageData;
            return src[0]+src[src_img.ImageSize-(src_img.WidthStep-src_img.Width)-1]+src[src_img.Width-1]+src[src_img.ImageSize-src_img.Width-1] > 511 ? Is.DESCENDING_ORDER:Is.ASCENDING_ORDER;
        }
        delegate byte SelectBucketMedian(int[] Bucket, int Median);
        public static unsafe bool FastestMedian(IplImage src_img,IplImage dst_img,int n){
            if((n&1)==0)return false;//偶数はさいなら
            Cv.Copy(src_img,dst_img);
            int MaskSize=n>>1;//
            SelectBucketMedian BucketMedian=GetBucketMedianAscendingOrder;
            if(SelectAscendingDescendingOrder(src_img)==Is.DESCENDING_ORDER)
                BucketMedian=GetBucketMedianDescendingOrder;

            byte* dst=(byte*)dst_img.ImageData;
            dst +=MaskSize*(src_img.WidthStep)+MaskSize;
            for(int y=MaskSize;y<src_img.Height-MaskSize;++y,dst +=src_img.WidthStep){
                int[] Bucket=new int[Const.Tone8Bit];//256tone It is cleared each time
                for(int x=0;x<n;++x){
                    byte* src=(byte*)src_img.ImageData;
                    src +=(y-MaskSize)*src_img.WidthStep+x;
                    for(int yy=0;yy<n;++yy,src+=src_img.WidthStep)
                        ++Bucket[*src];
                }/* */
                    *dst=BucketMedian(Bucket,((n*n)>>1));

                for(int x=0;x<src_img.Width-n;++x){
                    byte* src=(byte*)src_img.ImageData;
                    src +=(y-MaskSize)*src_img.WidthStep+x;
                    for(int yy=0;yy<n;++yy,src+=src_img.WidthStep){
                        --Bucket[*src];
                        ++Bucket[*(src+n)];
                    }
                    *(dst+x+1)=BucketMedian(Bucket,((n*n)>>1));
                }
            }
            return true;
        }
        //src_img:入出力
        public static void Median8(IplImage src_img){
            IplImage dst_img = Cv.CreateImage(src_img.GetSize(), BitDepth.U8, 1);
            Median8(src_img,dst_img);
            Cv.Copy(dst_img,src_img);//dst_img->src_img
            Cv.ReleaseImage(dst_img);
        }
        private static unsafe void Median8(IplImage src_img,IplImage dst_img){
            Cv.Copy(src_img,dst_img);
            byte* src=(byte*)src_img.ImageData,dst=(byte*)dst_img.ImageData;
            for(int y=1;y<src_img.Height-1;++y) {
                for(int x=1;x<src_img.Width-1;++x) {
                    int offset=(src_img.WidthStep*y)+x;
                    byte[] temp = new byte[Const.Neighborhood8];
                    temp[0]=(src[offset-src_img.WidthStep-1]);
                    temp[1]=(src[offset-src_img.WidthStep]);
                    temp[2]=(src[offset-src_img.WidthStep+1]);

                    temp[3]=(src[offset-1]);
                    temp[4] = (src[offset]);
                    temp[5]=(src[offset+1]);

                    temp[6]=(src[offset+src_img.WidthStep-1]);
                    temp[7]=(src[offset+src_img.WidthStep]);
                    temp[8]=(src[offset+src_img.WidthStep+1]);
                    StandardAlgorithm.Sort.Bubble(temp);
                    dst[offset]=temp[4];
                }
            }
        }
        public class SetMask{
            public static int[] Laplacian(int[] FilterMask){//マスクサイズは5or9を想定
                for(int i=0;i<FilterMask.Length;++i){       //1, 1,1     , 1,
                    FilterMask[i]=1;                        //1.-8,1    1,-4,1
                }                                           //1, 1,1     , 1,
                FilterMask[FilterMask.Length>>2] = -FilterMask.Length+1;   
                return FilterMask;
            }
        }
        //src_img:入出力
        public static bool ApplyMask(int[] Mask,IplImage src_img){
            if((Mask.Length!=Const.Neighborhood8)&&(Mask.Length!=Const.Neighborhood4))return false;
            IplImage dst_img = Cv.CreateImage(src_img.GetSize(), BitDepth.U8, 1);
            ApplyMask(Mask,src_img,dst_img);
            Cv.Copy(dst_img,src_img);//dst_img->src_img
            Cv.ReleaseImage(dst_img);
            return true;
        }
        public static unsafe bool ApplyMask(int[] Mask,IplImage src_img,IplImage dst_img){
            if(Mask.Length!=Const.Neighborhood8&&Mask.Length!=Const.Neighborhood4)return false;
            Cv.Set(dst_img,new CvScalar(0));
            byte* src=(byte*)src_img.ImageData,dst=(byte*)dst_img.ImageData;
            for(int y=1;y<src_img.Height-1;++y) 
                for(int x=1;x<src_img.Width-1;++x) {
                    int offset=src_img.WidthStep*y+x;
                    int temp;
                    if(Mask.Length==Const.Neighborhood8){
                        temp =(Mask[0]*src[offset-src_img.WidthStep-1]);
                        temp+=(Mask[1]*src[offset-src_img.WidthStep]);
                        temp+=(Mask[2]*src[offset-src_img.WidthStep+1]);

                        temp+=(Mask[3]*src[offset-1]);
                        temp+=(Mask[4]*src[offset]);
                        temp+=(Mask[5]*src[offset+1]);

                        temp+=(Mask[6]*src[offset+src_img.WidthStep-1]);
                        temp+=(Mask[7]*src[offset+src_img.WidthStep]);
                        temp+=(Mask[8]*src[offset+src_img.WidthStep+1]);
                    }else {
                        temp=(Mask[0]*src[offset-src_img.WidthStep]);

                        temp+=(Mask[1]*src[offset-1]);
                        temp+=(Mask[2]*src[offset]);
                        temp+=(Mask[3]*src[offset+1]);

                        temp+=(Mask[4]*src[offset+src_img.WidthStep]);
                    }
                    dst[offset]=Image.CheckRange2Byte(temp);
                }
            return true;
        }
    }
}
    