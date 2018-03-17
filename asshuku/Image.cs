using OpenCvSharp;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
public class Image{
    public static int GetShortSide(IplImage p_img){
        return p_img.Width>p_img.Height?p_img.Width:p_img.Height;
    }
    public static int GetLongSide(IplImage p_img){
        return p_img.Width>p_img.Height?p_img.Height:p_img.Width;
    }
    public static byte GetToneValueMax(IplImage p_img) {
        byte ToneValueMax=0;
        unsafe {
            byte* p=(byte*)p_img.ImageData;
            for(int y=0;y<p_img.ImageSize;y++) 
                ToneValueMax = p[y]>ToneValueMax ? p[y]:ToneValueMax;
        }
        return ToneValueMax;
    }        
    public static byte GetToneValueMin(IplImage p_img) {
        byte ToneValueMin=255;
        unsafe {
            byte* p=(byte*)p_img.ImageData;
            for(int y=0;y<p_img.ImageSize;++y) 
                ToneValueMin = p[y]<ToneValueMin ? p[y]:ToneValueMin;
        }
        return ToneValueMin;
    }
    public static byte GetToneValueMax(int[] Histgram){
        byte i=255;
        while(Histgram[(byte)(--i+1)]==0);
        return ++i;
    }
    public static byte GetToneValueMin(int[] Histgram){
        byte i=0;
        while(Histgram[(byte)(++i-1)]==0);
        return --i;
    }             
    public static int GetHistgramR(ref string f,int[] Histgram) {
        int Channel=Is.GrayScale;//1:gray,3:bgr color
        using(Bitmap bmp=new Bitmap(f)) {
            BitmapData data=bmp.LockBits(new Rectangle(0,0,bmp.Width,bmp.Height),ImageLockMode.ReadWrite,PixelFormat.Format32bppArgb);
            byte[] b=new byte[bmp.Width*bmp.Height*4];
            Marshal.Copy(data.Scan0,b,0,b.Length);
            for(int i=0;i<b.Length;i+=4) 
                if(Channel==Is.Color||b[i]!=b[i+1]||b[i+2]!=b[i]) { //Color images are not executed.
                    Channel=Is.Color;
                    Histgram[(int)((b[i]+b[i+1]+b[i+2]+0.5)/3)]++;//四捨五入
                }else Histgram[b[i]]++;
            bmp.UnlockBits(data);
        }
        return Channel;
    }

    public class Filter{
        private static byte GetBucketMedianAscendingOrder(int[] Bucket, int Median){
            byte YIndex=0;//256 探索範囲の最小値を探す　
            int ScanHalf=0;
            while((ScanHalf+=Bucket[YIndex++])<Median);//Underflow
            return --YIndex;
        }/* */
        private static byte GetBucketMedianDescendingOrder(int[] Bucket, int Median){
            byte YIndex=0;//256 探索範囲の最小値を探す　
            int ScanHalf=0;
            while((ScanHalf+=Bucket[--YIndex])<Median);//Underflow
            return YIndex;
        }
        //src_img:入出力
        public static bool FastestMedian(IplImage src_img,int n){
            if((n&1)==0)return false;//偶数はさいなら
            IplImage dst_img = Cv.CreateImage(src_img.GetSize(), BitDepth.U8, 1);
            FastestMedian(src_img,dst_img,n);
            Cv.Copy(dst_img,src_img);//dst_img->src_img
            Cv.ReleaseImage(dst_img);
            return true;
        }
        public static string SelectAscendingDescendingOrder(IplImage src_img){
            unsafe{
                byte* src=(byte*)src_img.ImageData;
                int Average =(int)((src[0]+src[(src_img.Height*src_img.Width)-1]+src[src_img.Width-1]+src[(src_img.Height*src_img.Width)-src_img.Width-1])>>2);
                return Average > 127 ? "DescendingOrder":"AscendingOrder";
            }
        }
        delegate byte SelectBucketMedian(int[] Bucket, int Median);

        public static bool FastestMedian(IplImage src_img,IplImage dst_img,int n){
            if((n&1)==0)return false;//偶数はさいなら
            Cv.Copy(src_img,dst_img);
            int MaskSize=n>>1;//
            SelectBucketMedian BucketMedian;
            if(SelectAscendingDescendingOrder(src_img)=="DescendingOrder")
                BucketMedian=GetBucketMedianDescendingOrder;
            else BucketMedian=GetBucketMedianAscendingOrder;
            
            unsafe{                    
                byte* src=(byte*)src_img.ImageData,dst=(byte*)dst_img.ImageData;
                for(int y=MaskSize;y<src_img.Height-MaskSize;y++){
                    int[] Bucket=new int[GetConstant.Tone8Bit];//256tone It is cleared each time
                    for(int x=0;x<n;x++)
                        for(int yy=y-MaskSize;yy<=y+MaskSize;yy++)
                            Bucket[src[yy*src_img.Width+x]]++;
                        dst[y*src_img.Width+MaskSize]=BucketMedian(Bucket,((n*n)>>1));

                    for(int x=MaskSize+1;x<src_img.Width-MaskSize;x++){
                        for(int yy=y-MaskSize;yy<=y+MaskSize;yy++){
                            Bucket[src[yy*src_img.Width+x+MaskSize-n]]--;
                            Bucket[src[yy*src_img.Width+x+MaskSize]]++;
                        }
                        dst[y*src_img.Width+x]=BucketMedian(Bucket,((n*n)>>1));
                    }
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
        private static void Median8(IplImage src_img,IplImage dst_img){
            Cv.Copy(src_img,dst_img);
            unsafe {
                byte* src=(byte*)src_img.ImageData,dst=(byte*)dst_img.ImageData;
                for(int y=1;y<dst_img.Height-1;++y) {
                    for(int x=1;x<dst_img.Width-1;++x) {
                        int offset=(dst_img.WidthStep*y)+x;
                        byte[] temp = new byte[GetConstant.Neighborhood8];
                        temp[0]=(src[offset-dst_img.WidthStep-1]);
                        temp[1]=(src[offset-dst_img.WidthStep]);
                        temp[2]=(src[offset-dst_img.WidthStep+1]);

                        temp[3]=(src[offset-1]);
                        temp[4] = (src[offset]);
                        temp[5]=(src[offset+1]);

                        temp[6]=(src[offset+dst_img.WidthStep-1]);
                        temp[7]=(src[offset+dst_img.WidthStep]);
                        temp[8]=(src[offset+dst_img.WidthStep+1]);
                        StandardAlgorithm.Sort.Bubble(temp);
                        dst[offset]=temp[4];
                    }
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
            if((Mask.Length!=GetConstant.Neighborhood8)&&(Mask.Length!=GetConstant.Neighborhood4))return false;
            IplImage dst_img = Cv.CreateImage(src_img.GetSize(), BitDepth.U8, 1);
            ApplyMask(Mask,src_img,dst_img);
            Cv.Copy(dst_img,src_img);//dst_img->src_img
            Cv.ReleaseImage(dst_img);
            return true;
        }
        public static bool ApplyMask(int[] Mask,IplImage src_img,IplImage dst_img){//戻り値
            Cv.Set(dst_img,new CvScalar(0));
            if(Mask.Length==GetConstant.Neighborhood8)
                for(int y=1;y<dst_img.Height-1;++y) 
                    for(int x=1;x<dst_img.Width-1;++x) 
                        unsafe {
                            int offset=dst_img.WidthStep*y+x;
                            byte* src=(byte*)src_img.ImageData,dst=(byte*)dst_img.ImageData;
                            int temp=(Mask[0]*src[offset-dst_img.Width-1]);
                            temp+=(Mask[1]*src[offset-dst_img.Width]);
                            temp+=(Mask[2]*src[offset-dst_img.Width+1]);

                            temp+=(Mask[3]*src[offset-1]);
                            temp += (Mask[4]*src[offset]);
                            temp+=(Mask[5]*src[offset+1]);

                            temp+=(Mask[6]*src[offset+dst_img.Width-1]);
                            temp+=(Mask[7]*src[offset+dst_img.Width]);
                            temp+=(Mask[8]*src[offset+dst_img.Width+1]);
                            temp = temp>255?255:temp<0?0:temp;
                            dst[offset]=(byte)temp;
                        }
            else if(Mask.Length==GetConstant.Neighborhood4)
                for(int y=1;y<dst_img.Height-1;++y) 
                    for(int x=1;x<dst_img.Width-1;++x) 
                        unsafe {
                            int offset=dst_img.WidthStep*y+x;
                            byte* src=(byte*)src_img.ImageData,dst=(byte*)dst_img.ImageData;
                            int temp=(Mask[0]*src[offset-dst_img.Width]);

                            temp+=(Mask[1]*src[offset-1]);
                            temp += (Mask[2]*src[offset]);
                            temp+=(Mask[3]*src[offset+1]);

                            temp+=(Mask[4]*src[offset+dst_img.Width]);
                            temp = temp>255?255:temp<0?0:temp;
                            dst[offset]=(byte)temp;
                        }
            else {
                return false;
            }
            return true;
        }
    }
}
    