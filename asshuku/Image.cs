using OpenCvSharp;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System;
//using System.Windows.Forms;
public class Image {
    private static unsafe void Median8(IplImage src_img, IplImage dst_img) {
        Cv.Copy(src_img, dst_img);
        byte* src = (byte*)src_img.ImageData, dst = (byte*)dst_img.ImageData;
        for (int y = 1; y < src_img.Height - 1; ++y) {
            for (int x = 1; x < src_img.Width - 1; ++x) {
                int offset = (src_img.WidthStep * y) + x;
                byte[] temp = new byte[Const.Neighborhood8];
                temp[0] = (src[offset - src_img.WidthStep - 1]);
                temp[1] = (src[offset - src_img.WidthStep]);
                temp[2] = (src[offset - src_img.WidthStep + 1]);

                temp[3] = (src[offset - 1]);
                temp[4] = (src[offset]);
                temp[5] = (src[offset + 1]);

                temp[6] = (src[offset + src_img.WidthStep - 1]);
                temp[7] = (src[offset + src_img.WidthStep]);
                temp[8] = (src[offset + src_img.WidthStep + 1]);
                StandardAlgorithm.Sort.Bubble(temp);
                dst[offset] = src[offset];
            }
        }
    }
    //	DCT関数 離散コサイン変換
    // https://jp.mathworks.com/help/images/discrete-cosine-transform.html
    public static unsafe void DCT(IplImage src_img, ref double[,] dst_img) {//org,dct
        byte* src = (byte*)src_img.ImageData;
        for (int p = 0; p < src_img.Height; ++p) {
            double AlphaP = Math.Sqrt(2.0 / src_img.Height);
            if (p == 0)
                AlphaP = Math.Sqrt(1.0 / src_img.Height);
            for (int q = 0; q < src_img.Width; ++q) {
                double AlphaQ = Math.Sqrt(2.0 / src_img.Width);
                if (q == 0)
                    AlphaQ = Math.Sqrt(1.0 / src_img.Width);
                double temp = 0;
                for (int m = 0; m < src_img.Height; ++m)//m ->y,n->x
                    for (int n = 0; n < src_img.Width; ++n)
                        temp += (src[src_img.WidthStep * m + n]) * Math.Cos(Math.PI * p * (2 * m + 1) / (2 * src_img.Height)) * Math.Cos(Math.PI * q * (2 * n + 1) / (2 * src_img.Width));
                dst_img[p, q] = ((temp * AlphaP * AlphaQ));
            }
        }
    }
    public static unsafe void DCTN(IplImage src_img, ref double[,] dst_img) {//org,dct
        double[,] CosMPTable = new double[src_img.Height, src_img.Height];
        for (int p = 0; p < src_img.Height; ++p) {
            for (int m = 0; m < src_img.Height; ++m) {
                CosMPTable[p, m] = Math.Cos(Math.PI * p * (2 * m + 1) / (2 * src_img.Height));
            }
        }
        double[,] CosNQTable = new double[src_img.Width, src_img.Width];
        for (int q = 0; q < src_img.Width; ++q) {
            for (int n = 0; n < src_img.Width; ++n) {
                CosNQTable[q, n] = Math.Cos(Math.PI * q * (2 * n + 1) / (2 * src_img.Width));
            }
        }
        for (int p = 0; p < src_img.Height; ++p) {
            double AlphaP = Math.Sqrt(2.0 / src_img.Height);
            if (p == 0)
                AlphaP = Math.Sqrt(1.0 / src_img.Height);
            for (int q = 0; q < src_img.Width; ++q) {
                double AlphaQ = Math.Sqrt(2.0 / src_img.Width);
                if (q == 0)
                    AlphaQ = Math.Sqrt(1.0 / src_img.Width);
                double temp = 0;
                byte* src = (byte*)src_img.ImageData;
                for (int m = 0; m < src_img.Height; ++m)//m ->y,n->x
                    for (int n = 0; n < src_img.Width; ++n)
                        temp += (src[src_img.WidthStep * m + n]) * CosMPTable[p, m] * CosNQTable[q, n];
                dst_img[p, q] = temp * AlphaP * AlphaQ;
            }
        }
    }
    public static unsafe void DCTNK(IplImage src_img, ref double[,] dst_img) {//org,dct
        double[,] CosMPTable = new double[src_img.Height, src_img.Height];
        fixed (double* CMPT = &CosMPTable[0, 0])
            for (int p = 0; p < src_img.Height; ++p)
                for (int m = 0; m < src_img.Height; ++m)
                    CMPT[p * src_img.Height + m] = Math.Cos(Math.PI * p * (m + 0.5) / (src_img.Height));
        double[,] CosNQTable = new double[src_img.Width, src_img.Width];
        fixed (double* CNQT = &CosNQTable[0, 0])
            for (int q = 0; q < src_img.Width; ++q)
                for (int n = 0; n < src_img.Width; ++n)
                    CNQT[q * src_img.Width + n] = Math.Cos(Math.PI * q * (n + 0.5) / (src_img.Width));
        for (int p = 0; p < src_img.Height; ++p) {
            double AlphaP = Math.Sqrt(2.0 / src_img.Height);
            if (p == 0)
                AlphaP = Math.Sqrt(1.0 / src_img.Height);
            fixed (double* CMPT = &CosMPTable[0, 0])
                for (int q = 0; q < src_img.Width; ++q) {
                    double temp = 0;
                    fixed (double* CNQT = &CosNQTable[0, 0])
                        for (int m = 0; m < src_img.Height; ++m) {
                            for (int n = 0; n < src_img.Width; ++n)
                                temp += (((byte*)src_img.ImageData)[src_img.WidthStep * m + n]) * CNQT[q * src_img.Width + n] * CMPT[p * src_img.Height + m];
                        }
                    if (q == 0)
                        dst_img[p, 0] = temp * AlphaP / Math.Sqrt(src_img.Width);
                    else
                        dst_img[p, q] = temp * AlphaP * Math.Sqrt(2.0 / src_img.Width);
                }

        }
    }
    public static unsafe void DCTNKS(IplImage src_img, ref double[,] dst_img) {//org,dct
        double[,] CosMPTable = new double[src_img.Height, src_img.Height];
        fixed (double* CMPT = &CosMPTable[0, 0])
            for (int p = 0; p < src_img.Height; ++p)
                for (int m = 0; m < src_img.Height; ++m)
                    CMPT[p * src_img.Height + m] = Math.Cos(Math.PI * p * (m + 0.5) / (src_img.Height));
        double[,] CosNQTable = new double[src_img.Width, src_img.Width];
        fixed (double* CNQT = &CosNQTable[0, 0])
            for (int q = 0; q < src_img.Width; ++q)
                for (int n = 0; n < src_img.Width; ++n)
                    CNQT[q * src_img.Width + n] = Math.Cos(Math.PI * q * (n + 0.5) / (src_img.Width));
        for (int p = 0; p < src_img.Height; ++p) {
            double AlphaP = Math.Sqrt(2.0 / src_img.Height);
            if (p == 0)
                AlphaP = Math.Sqrt(1.0 / src_img.Height);
            int pHeight = p * src_img.Height;
            fixed (double* CMPT = &CosMPTable[0, 0])
                for (int q = 0; q < src_img.Width; ++q) {
                    double temp = 0;
                    int qWidth = q * src_img.Width;
                    fixed (double* CNQT = &CosNQTable[0, 0])
                        for (int m = 0; m < src_img.Height; ++m) {
                            double CMPTpHeightm = CMPT[pHeight + m];
                            int WidthStepm = src_img.WidthStep * m;
                            for (int n = 0; n < src_img.Width; ++n)
                                temp += (((byte*)src_img.ImageData)[WidthStepm + n]) * CNQT[qWidth + n] * CMPTpHeightm;
                        }
                    if (q == 0)
                        dst_img[p, 0] = temp * AlphaP / Math.Sqrt(src_img.Width);
                    else
                        dst_img[p, q] = temp * AlphaP * Math.Sqrt(2.0 / src_img.Width);
                }

        }
    }
    public static unsafe void DCTNKK(IplImage src_img, ref double[,] dst_img) {//org,dct
        double[,] CosMPTable = new double[src_img.Height, src_img.Height];
        fixed (double* CMPT = &CosMPTable[0, 0])
            for (int p = 0; p < src_img.Height; ++p)
                for (int m = 0; m < src_img.Height; ++m)
                    CMPT[p * src_img.Height + m] = Math.Cos(Math.PI * p * (2 * m + 1) / (2 * src_img.Height));
        double[,] CosNQTable = new double[src_img.Width, src_img.Width];
        fixed (double* CNQT = &CosNQTable[0, 0])
            for (int q = 0; q < src_img.Width; ++q)
                for (int n = 0; n < src_img.Width; ++n)
                    CNQT[q * src_img.Width + n] = Math.Cos(Math.PI * q * (2 * n + 1) / (2 * src_img.Width));

        double[,,] src_imgCosNQTable = new double[src_img.Width, src_img.Height, src_img.Width];
        fixed (double* CNQT = &CosNQTable[0, 0])
        fixed (double* sCNQT = &src_imgCosNQTable[0, 0, 0])
            for (int q = 0; q < src_img.Width; ++q) {
                for (int m = 0; m < src_img.Height; ++m) {
                    for (int n = 0; n < src_img.Width; ++n)
                        sCNQT[(q * src_img.Height + m) * src_img.Width + n] = (((byte*)src_img.ImageData)[src_img.WidthStep * m + n]) * CNQT[q * src_img.Width + n];
                }
            }

        for (int p = 0; p < src_img.Height; ++p) {
            double AlphaP = Math.Sqrt(2.0 / src_img.Height);
            if (p == 0)
                AlphaP = Math.Sqrt(1.0 / src_img.Height);
            for (int q = 0; q < src_img.Width; ++q) {
                double temp = 0;
                fixed (double* sCNQT = &src_imgCosNQTable[0, 0, 0])
                fixed (double* CMPT = &CosMPTable[0, 0])
                    for (int m = 0; m < src_img.Height; ++m) {
                        for (int n = 0; n < src_img.Width; ++n)
                            temp += sCNQT[(q * src_img.Height + m) * src_img.Width + n] * CMPT[p * src_img.Height + m];
                    }
                if (q == 0)
                    dst_img[p, 0] = temp * AlphaP * Math.Sqrt(1.0 / src_img.Width);
                else
                    dst_img[p, q] = temp * AlphaP * Math.Sqrt(2.0 / src_img.Width);
            }

        }
    }
    public static unsafe void DCTK(IplImage src_img, ref double[,] dst_img) {//org,dct//DCTの掛け算削減
        int Height2 = (2 * src_img.Height);
        int Width2 = (2 * src_img.Width);
        double Sqrt1Width = Math.Sqrt(1.0 / src_img.Width);
        double Sqrt2Width = Math.Sqrt(2.0 / src_img.Width);
        double Sqrt2Height = Math.Sqrt(2.0 / src_img.Height);
        for (int p = 0; p < src_img.Height; ++p) {
            double AlphaP = Sqrt2Height;
            double PIpHeight2 = Math.PI * p / Height2;
            if (p == 0) {
                AlphaP = Math.Sqrt(1.0 / src_img.Height);
            }
            for (int q = 0; q < src_img.Width; ++q) {
                double PIqWidth2 = Math.PI * q / Width2;
                fixed (double* CPqW22n1 = &(new double[src_img.Width])[0]) {
                    // m==0//
                    //n==0,m==0
                    CPqW22n1[0] = Math.Cos(PIqWidth2);
                    byte* src = (byte*)src_img.ImageData;
                    double temp = (src[0]) * Math.Cos(PIpHeight2) * CPqW22n1[0];
                    //n==0,m==0
                    for (int n = 1; n < src_img.Width; ++n) {
                        CPqW22n1[n] = Math.Cos(PIqWidth2 * (2 * n + 1));
                        temp += (src[n]) * Math.Cos(PIpHeight2) * CPqW22n1[n];
                    }
                    //m==0//
                    for (int m = 1; m < src_img.Height; ++m) {//m ->y,n->x
                        double CosPIpHeight22m1 = Math.Cos(PIpHeight2 * (2 * m + 1));
                        int src_imgWidthStepm = src_img.WidthStep * m;
                        for (int n = 0; n < src_img.Width; ++n)
                            temp += (src[src_imgWidthStepm + n]) * CosPIpHeight22m1 * CPqW22n1[n];
                    }
                    if (q == 0)
                        dst_img[p, 0] = temp * AlphaP * Sqrt1Width;
                    else dst_img[p, q] = temp * AlphaP * Sqrt2Width;
                }
            }
        }
    }
    //	逆DCT関数
    public static unsafe void IDCT(IplImage src_img, in double[,] dst_img) {//org,dct
        byte* src = (byte*)src_img.ImageData;
        for (int m = 0; m < src_img.Height; ++m) {
            for (int n = 0; n < src_img.Width; ++n) {
                double temp = 0;
                for (int p = 0; p < src_img.Height; ++p) {//m ->y,n->x
                    double AlphaP = Math.Sqrt(2.0 / src_img.Height);
                    if (p == 0)
                        AlphaP = Math.Sqrt(1.0 / src_img.Height);
                    for (int q = 0; q < src_img.Width; ++q) {
                        double AlphaQ = Math.Sqrt(2.0 / src_img.Width);
                        if (q == 0)
                            AlphaQ = Math.Sqrt(1.0 / src_img.Width);
                        temp += AlphaP * AlphaQ * (dst_img[p, q]) * Math.Cos(Math.PI * (2 * m + 1) * p / (2 * src_img.Height)) * Math.Cos(Math.PI * (2 * n + 1) * q / (2 * src_img.Width));
                    }
                }
                src[src_img.WidthStep * m + n] = (byte)Math.Truncate(temp);
            }
        }
    }
    public static unsafe void IDCTN(IplImage src_img, in double[,] dst_img) {//org,dct
        double[,] CosMPTable = new double[src_img.Height, src_img.Height];
        for (int m = 0; m < src_img.Height; ++m) {
            for (int p = 0; p < src_img.Height; ++p) {
                CosMPTable[p, m] = Math.Cos(Math.PI * p * (2 * m + 1) / (2 * src_img.Height));
            }
        }
        double[,] CosNQTable = new double[src_img.Width, src_img.Width];
        for (int n = 0; n < src_img.Width; ++n) {
            for (int q = 0; q < src_img.Width; ++q) {
                CosNQTable[q, n] = Math.Cos(Math.PI * q * (2 * n + 1) / (2 * src_img.Width));
            }
        }
        for (int m = 0; m < src_img.Height; ++m) {
            for (int n = 0; n < src_img.Width; ++n) {
                double temp = 0;
                for (int p = 0; p < src_img.Height; ++p) {//m ->y,n->x
                    double AlphaP = Math.Sqrt(2.0 / src_img.Height);
                    if (p == 0)
                        AlphaP = Math.Sqrt(1.0 / src_img.Height);
                    temp += (dst_img[p, 0]) * AlphaP * Math.Sqrt(1.0 / src_img.Width) * CosMPTable[p, m] * CosNQTable[0, n];//q==0
                    for (int q = 1; q < src_img.Width; ++q) {
                        temp += (dst_img[p, q]) * AlphaP * Math.Sqrt(2.0 / src_img.Width) * CosMPTable[p, m] * CosNQTable[q, n];
                    }
                } ((byte*)src_img.ImageData)[src_img.WidthStep * m + n] = (byte)Math.Truncate(temp);
            }
        }
    }
    public static unsafe void IDCTNK(IplImage src_img, in double[,] dst_img) {//org,dct
        double[,] CosMPTable = new double[src_img.Height, src_img.Height];
        fixed (double* CMPT = &CosMPTable[0, 0])
            for (int m = 0; m < src_img.Height; ++m)
                for (int p = 0; p < src_img.Height; ++p)
                    CMPT[p * src_img.Height + m] = Math.Cos(Math.PI * p * (2 * m + 1) / (2 * src_img.Height));
        double[,] CosNQTable = new double[src_img.Width, src_img.Width];
        fixed (double* CNQT = &CosNQTable[0, 0])
            for (int n = 0; n < src_img.Width; ++n)
                for (int q = 0; q < src_img.Width; ++q)
                    CNQT[q * src_img.Width + n] = Math.Cos(Math.PI * q * (2 * n + 1) / (2 * src_img.Width));
        double Sqrt2HeightSqrt2Width = 2.0 / Math.Sqrt(src_img.Width * src_img.Height);
        double Sqrt1HeightSqrt1Width = 1.0 / Math.Sqrt(src_img.Height * src_img.Width);
        double Sqrt2HeightSqrt1Width = Math.Sqrt(2.0 / (src_img.Height * src_img.Width));
        for (int m = 0; m < src_img.Height; ++m) {
            for (int n = 0; n < src_img.Width; ++n) {
                fixed (double* CMPT = &CosMPTable[0, 0])
                fixed (double* CNQT = &CosNQTable[0, 0])
                fixed (double* dst = &dst_img[0, 0]) {
                    //p==0
                    double temp = (dst[0]) * Sqrt1HeightSqrt1Width * CMPT[m] * CNQT[n];//q==0
                    for (int q = 1; q < src_img.Width; ++q) {
                        temp += (dst[q]) * Sqrt2HeightSqrt1Width * CMPT[m] * CNQT[q * src_img.Width + n];
                    }
                    for (int p = 1; p < src_img.Height; ++p) {//m ->y,n->x
                        temp += (dst[p * src_img.Width]) * Sqrt2HeightSqrt1Width * CMPT[p * src_img.Height + m] * CNQT[n];//q==0
                        double Sqrt2HeightSqrt2WidthCosMPTable = Sqrt2HeightSqrt2Width * CMPT[p * src_img.Height + m];
                        for (int q = 1; q < src_img.Width; ++q) {
                            temp += (dst[p * src_img.Width + q]) * Sqrt2HeightSqrt2WidthCosMPTable * CNQT[q * src_img.Width + n];
                        }
                    }
                     ((byte*)src_img.ImageData)[src_img.WidthStep * m + n] = (byte)Math.Truncate(temp);
                }
            }
        }
    }
    public static unsafe void IDCTK(IplImage src_img, in double[,] dst_img) {//org,dct //IDCTの掛け算削減
        int Height2 = 2 * src_img.Height;
        int Width2 = 2 * src_img.Width;
        double Sqrt1Width = Math.Sqrt(1.0 / src_img.Width);
        double Sqrt2Width = Math.Sqrt(2.0 / src_img.Width);
        double Sqrt1Height = Math.Sqrt(1.0 / src_img.Height);
        double Sqrt2Height = Math.Sqrt(2.0 / src_img.Height);
        double Sqrt2HeightSqrt2Width = 2 / Math.Sqrt(src_img.Width * src_img.Height);
        fixed (double* dst = &dst_img[0, 0]) {
            for (int m = 0; m < src_img.Height; ++m) {
                double Pai2M1Height2 = Math.PI * (2 * m + 1) / Height2;
                int src_imgWidthStepm = src_img.WidthStep * m;
                for (int n = 0; n < src_img.Width; ++n) {
                    double Pai2n1Width2 = Math.PI * (2 * n + 1) / Width2;
                    //if (p == 0)
                    double Sqrt1HeightSqrt2Width = Sqrt1Height * Sqrt2Width;
                    double temp = (dst[0]) * Sqrt1Height * Sqrt1Width;//q=0
                    fixed (double* CP2n1W2q = &(new double[src_img.Width])[0]) {
                        for (int q = 1; q < src_img.Width; ++q) {
                            CP2n1W2q[q] = Math.Cos(Pai2n1Width2 * q);
                            temp += (dst[q]) * CP2n1W2q[q] * Sqrt1HeightSqrt2Width;
                        }
                        //if (p == 0)
                        for (int p = 1; p < src_img.Height; ++p) {//m ->y,n->xx
                            double SqrtHeightSqrt2WidthCosPai2n1Width2p = Math.Cos(Pai2M1Height2 * p) * Sqrt2HeightSqrt2Width;
                            int psrc_imgWidth = p * src_img.Width;
                            temp += Sqrt2Height * Sqrt1Width * (dst[psrc_imgWidth]) * Math.Cos(Pai2M1Height2 * p);//q=0
                            for (int q = 1; q < src_img.Width; ++q) {
                                temp += (dst[psrc_imgWidth + q]) * CP2n1W2q[q] * SqrtHeightSqrt2WidthCosPai2n1Width2p;
                            }
                        }
                    } ((byte*)src_img.ImageData)[src_imgWidthStepm + n] = (byte)Math.Truncate(temp);
                }
            }
        }
    }
    public static unsafe byte GetPixel(IplImage src_img, int x, int y) {
        byte* src = (byte*)src_img.ImageData;
        return src[src_img.WidthStep * y + x];
    }
    public static unsafe bool SetPixel(IplImage src_img, int x, int y, byte PixelValue) {
        byte* src = (byte*)src_img.ImageData;
        src[src_img.WidthStep * y + x] = PixelValue;
        return true;
    }
    public static int GetShortSide(IplImage p_img) {
        return p_img.Width > p_img.Height ? p_img.Width : p_img.Height;
    }
    public static int GetLongSide(IplImage p_img) {
        return p_img.Width > p_img.Height ? p_img.Height : p_img.Width;
    }
    private static byte CheckRange2Byte(int ByteValue) {
        return (byte)(ByteValue > 255 ? 255 : ByteValue < 0 ? 0 : ByteValue);
    }
    private static byte CheckRange2Byte(double ByteValue) {
        return (byte)(ByteValue > 255 ? 255 : ByteValue < 0 ? 0 : ByteValue);
    }
    public static unsafe byte GetToneValueMax(IplImage p_img) {
        byte ToneValueMax = 0;
        byte* p = (byte*)p_img.ImageData;
        for (int y = 0; y < p_img.ImageSize; ++y)
            ToneValueMax = p[y] > ToneValueMax ? p[y] : ToneValueMax;
        return ToneValueMax;
    }
    public static unsafe byte GetToneValueMin(IplImage p_img) {
        byte ToneValueMin = 255;
        byte* p = (byte*)p_img.ImageData;
        for (int y = 0; y < p_img.ImageSize; ++y)
            ToneValueMin = p[y] < ToneValueMin ? p[y] : ToneValueMin;
        return ToneValueMin;
    }
    /*public static byte GetToneValueMax(int[] Histgram) {
        int i = 255;
        while (Histgram[i--] == 0) ;
        return (byte)++i;
    }
    public static byte GetToneValueMin(int[] Histgram) {
        int i = 0;
        while (Histgram[i++] == 0) ;
        return (byte)--i;
    }*/
    public static int GetHistgramR(ref string f, byte[] Histgram) {
        int Channel = Is.GrayScale;//1:gray,3:bgr color
        Bitmap bmp = new Bitmap(f);
        BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);//32bit で読む
        byte[] b = new byte[bmp.Width * bmp.Height * 4];
        Marshal.Copy(data.Scan0, b, 0, b.Length);
        for (int i = 0; i < b.Length; i += 4)
            if (Channel == Is.Color || b[i] != b[i + 1] || b[i + 2] != b[i]) {//Color images are not executed.
                Channel = Is.Color;
                Histgram[(byte)((b[i] + b[i + 1] + b[i + 2] + 0.5) / 3)]++;//四捨五入
            } else Histgram[b[i]]++;
        bmp.UnlockBits(data);
        bmp.Dispose();
        return Channel;
    }
    public class ToneValue {
        public byte Max { get; set; }
        public byte Min { get; set; }
    }
    public static unsafe void Transform2Linear(IplImage p_img, ToneValue ImageToneValue) {//階調値の線形変換 グレイスケールのみ
        double magnification = 255.99 / (ImageToneValue.Max - ImageToneValue.Min);//255.99ないと255が254になる
        byte* p = (byte*)p_img.ImageData;
        for (int y = 0; y < p_img.ImageSize; ++y)
            p[y] = Image.CheckRange2Byte(magnification * (p[y] - ImageToneValue.Min));
    }
    public class Filter {
        private static byte GetBucketMedianAscendingOrder(int[,] Bucket, int Median, int x) {
            byte YIndex = 0;//256 探索範囲の最小値を探す　
            int ScanHalf = 0;
            while ((ScanHalf += Bucket[x, YIndex++]) < Median) ;//Underflow
            return --YIndex;
        }/* */
        private static byte GetBucketMedianDescendingOrder(int[,] Bucket, int Median, int x) {
            byte YIndex = 0;//256 探索範囲の最小値を探す　
            int ScanHalf = 0;
            while ((ScanHalf += Bucket[x, --YIndex]) < Median) ;//Underflow
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
        private static bool GetBucketMedianAscendingOrder(int[] Bucket, int Median, ref byte MedianValue) {
            int YIndex = -1;//256 探索範囲の最小値を探す　
            for (int ScanHalf = 0; ScanHalf < Median; ScanHalf += Bucket[YIndex]) {
                if (++YIndex > 255) return false;
                else if (Bucket[YIndex] < 0) return false;
            }
            MedianValue = (byte)(YIndex);
            return true;
        }/* */
        private static bool GetBucketMedianDescendingOrder(int[] Bucket, int Median, ref byte MedianValue) {
            int YIndex = 256;//256 探索範囲の最小値を探す　
            for (int ScanHalf = 0; ScanHalf < Median; ScanHalf += Bucket[YIndex]) {
                if (--YIndex < 0) return false;
                else if (Bucket[YIndex] < 0) return false;
            }
            MedianValue = (byte)(YIndex);
            return true;
        }
        private static byte GetBucketMedianDescendingOrder(int[] Bucket, int Median) {
            byte YIndex = 0;//256 探索範囲の最小値を探す　
            int ScanHalf = 0;
            while ((ScanHalf += Bucket[--YIndex]) < Median) ;//Underflow
            return YIndex;
        }
        private static byte GetBucketMedianAscendingOrder(int[] Bucket, int Median) {
            byte YIndex = 0;//256 探索範囲の最小値を探す　
            int ScanHalf = 0;
            while ((ScanHalf += Bucket[YIndex++]) < Median) ;//Underflow
            return --YIndex;
        }/* */
         //src_img:入出力
        public static bool FastestMedian(IplImage src_img, int n) {
            if ((n & 1) == 0) return false;//偶数はさいなら
            IplImage dst_img = Cv.CreateImage(src_img.GetSize(), BitDepth.U8, 1);
            FastestMedian(src_img, dst_img, n);
            Cv.Copy(dst_img, src_img);//dst_img->src_img
            Cv.ReleaseImage(dst_img);
            return true;
        }
        private static unsafe bool SelectAscendingDescendingOrder(IplImage src_img) {
            byte* src = (byte*)src_img.ImageData;
            return src[0] + src[src_img.ImageSize - (src_img.WidthStep - src_img.Width) - 1] + src[src_img.Width - 1] + src[src_img.ImageSize - src_img.Width - 1] > 511 ? Is.DESCENDING_ORDER : Is.ASCENDING_ORDER;
        }
        delegate byte SelectBucketMedian(int[] Bucket, int Median);
        public static unsafe bool FastestMedian(IplImage src_img, IplImage dst_img, int n) {
            Cv.Copy(src_img, dst_img);
            if ((n & 1) == 0) return false;//偶数はさいなら 元のをコピー
            int MaskSize = n >> 1;//
            SelectBucketMedian BucketMedian = GetBucketMedianAscendingOrder;
            if (SelectAscendingDescendingOrder(src_img) == Is.DESCENDING_ORDER)
                BucketMedian = GetBucketMedianDescendingOrder;

            byte* dst = (byte*)dst_img.ImageData;
            dst += MaskSize * (src_img.WidthStep) + MaskSize;
            for (int y = MaskSize; y < src_img.Height - MaskSize; ++y, dst += src_img.WidthStep) {
                int[] Bucket = new int[Const.Tone8Bit];//256tone It is cleared each time
                for (int x = 0; x < n; ++x) {
                    byte* src = (byte*)src_img.ImageData;
                    src += (y - MaskSize) * src_img.WidthStep + x;
                    for (int yy = 0; yy < n; ++yy, src += src_img.WidthStep)
                        ++Bucket[*src];
                }/* */
                *dst = BucketMedian(Bucket, ((n * n) >> 1));

                for (int x = 0; x < src_img.Width - n; ++x) {
                    byte* src = (byte*)src_img.ImageData;
                    src += (y - MaskSize) * src_img.WidthStep + x;
                    for (int yy = 0; yy < n; ++yy, src += src_img.WidthStep) {
                        --Bucket[*src];
                        ++Bucket[*(src + n)];
                    }
                    *(dst + x + 1) = BucketMedian(Bucket, ((n * n) >> 1));
                }
            }
            return true;
        }
        //src_img:入出力
        public static void Median8(IplImage src_img) {
            IplImage dst_img = Cv.CreateImage(src_img.GetSize(), BitDepth.U8, 1);
            Median8(src_img, dst_img);
            Cv.Copy(dst_img, src_img);//dst_img->src_img
            Cv.ReleaseImage(dst_img);
        }
        private static unsafe void Median8(IplImage src_img, IplImage dst_img) {
            Cv.Copy(src_img, dst_img);
            byte* src = (byte*)src_img.ImageData, dst = (byte*)dst_img.ImageData;
            for (int y = 1; y < src_img.Height - 1; ++y) {
                for (int x = 1; x < src_img.Width - 1; ++x) {
                    int offset = (src_img.WidthStep * y) + x;
                    byte[] temp = new byte[Const.Neighborhood8];
                    temp[0] = (src[offset - src_img.WidthStep - 1]);
                    temp[1] = (src[offset - src_img.WidthStep]);
                    temp[2] = (src[offset - src_img.WidthStep + 1]);

                    temp[3] = (src[offset - 1]);
                    temp[4] = (src[offset]);
                    temp[5] = (src[offset + 1]);

                    temp[6] = (src[offset + src_img.WidthStep - 1]);
                    temp[7] = (src[offset + src_img.WidthStep]);
                    temp[8] = (src[offset + src_img.WidthStep + 1]);
                    StandardAlgorithm.Sort.Bubble(temp);
                    dst[offset] = temp[4];
                }
            }
        }
        public class SetMask {
            public static int[] Laplacian(int[] FilterMask) {//マスクサイズは5or9を想定
                for (int i = 0; i < FilterMask.Length; ++i) {       //1, 1,1     , 1,
                    FilterMask[i] = 1;                        //1.-8,1    1,-4,1
                }                                           //1, 1,1     , 1,
                FilterMask[FilterMask.Length >> 2] = -FilterMask.Length + 1;
                return FilterMask;
            }
        }
        //src_img:入出力
        public static bool ApplyMask(int[] Mask, IplImage src_img) {
            if ((Mask.Length != Const.Neighborhood8) && (Mask.Length != Const.Neighborhood4)) return false;
            IplImage dst_img = Cv.CreateImage(src_img.GetSize(), BitDepth.U8, 1);
            ApplyMask(Mask, src_img, dst_img);
            Cv.Copy(dst_img, src_img);//dst_img->src_img
            Cv.ReleaseImage(dst_img);
            return true;
        }
        public static unsafe bool ApplyMask(int[] Mask, IplImage src_img, IplImage dst_img) {
            if (Mask.Length != Const.Neighborhood8 && Mask.Length != Const.Neighborhood4) return false;
            Cv.Set(dst_img, new CvScalar(0));
            byte* src = (byte*)src_img.ImageData, dst = (byte*)dst_img.ImageData;
            for (int y = 1; y < src_img.Height - 1; ++y)
                for (int x = 1; x < src_img.Width - 1; ++x) {
                    int offset = src_img.WidthStep * y + x;
                    int temp;
                    if (Mask.Length == Const.Neighborhood8) {
                        temp = (Mask[0] * src[offset - src_img.WidthStep - 1]);
                        temp += (Mask[1] * src[offset - src_img.WidthStep]);
                        temp += (Mask[2] * src[offset - src_img.WidthStep + 1]);

                        temp += (Mask[3] * src[offset - 1]);
                        temp += (Mask[4] * src[offset]);
                        temp += (Mask[5] * src[offset + 1]);

                        temp += (Mask[6] * src[offset + src_img.WidthStep - 1]);
                        temp += (Mask[7] * src[offset + src_img.WidthStep]);
                        temp += (Mask[8] * src[offset + src_img.WidthStep + 1]);
                    } else {
                        temp = (Mask[0] * src[offset - src_img.WidthStep]);

                        temp += (Mask[1] * src[offset - 1]);
                        temp += (Mask[2] * src[offset]);
                        temp += (Mask[3] * src[offset + 1]);

                        temp += (Mask[4] * src[offset + src_img.WidthStep]);
                    }
                    dst[offset] = Image.CheckRange2Byte(temp);
                }
            return true;
        }
    }
}
