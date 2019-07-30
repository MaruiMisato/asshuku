//#define DEBUG_SAVE
//#define DEBUG_DISPLAY
using System;
using System.IO;
//using System.IO.Path;
//using System.Text;
using System.Windows.Forms;
using System.Threading.Tasks;//setparalle
using System.Linq;//enum
using System.Collections.Generic;//enum
using System.Drawing;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;//正規表現
using System.Runtime.InteropServices;//Marshal.Copy(data.Scan0,b,0,b.Length);
using OpenCvSharp;
using static Image;
namespace asshuku {
    public partial class Form1 : Form {
        public Form1() {
            InitializeComponent();
        }
        public class Threshold {
            public byte Concentration { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public int Times { get; } = 3;
        }
        public class Rect {
            public int YLow { get; set; }
            public int XLow { get; set; }
            public int YHigh { get; set; }
            public int XHigh { get; set; }
            public CvSize Size = new CvSize();
        }
        private void ReNameAlfaBeta(string PathName, ref IEnumerable<string> files, string[] NewFileName) {
            int i = 0;
            foreach (string f in files) {
                FileInfo file = new FileInfo(f);
                string FileName = NewFileName[i] + ".png";
                if (file.Extension == ".jpg" || file.Extension == ".jpeg" || file.Extension == ".JPG" || file.Extension == ".JPEG") //jpg
                    FileName = NewFileName[i] + ".jpg";
                logs.Items.Add(Path.GetFileNameWithoutExtension(f) + " -> " + i++ + " " + FileName);
                file.MoveTo(PathName + "/" + FileName);
            }
        }
        private unsafe void WhiteCut(IplImage p_img, IplImage q_img, Rect NewImageRect) {
            byte* p = (byte*)p_img.ImageData, q = (byte*)q_img.ImageData;
            for (int y = NewImageRect.YLow; y < NewImageRect.YHigh; ++y)
                for (int x = NewImageRect.XLow; x < NewImageRect.XHigh; ++x)
                    q[q_img.WidthStep * (y - NewImageRect.YLow) + (x - NewImageRect.XLow)] = p[p_img.WidthStep * y + x];
        }
        private unsafe void WhiteCutColor(ref string f, IplImage q_img, Rect NewImageRect) {//階調値線形変換はしない
            Bitmap bmp = new Bitmap(f);
            BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            byte[] b = new byte[bmp.Width * bmp.Height * 4];
            Marshal.Copy(data.Scan0, b, 0, b.Length);
            byte* q = (byte*)q_img.ImageData;
            for (int y = NewImageRect.YLow; y < NewImageRect.YHigh; ++y)
                for (int x = NewImageRect.XLow; x < NewImageRect.XHigh; ++x) {
                    int qoffset = (q_img.WidthStep * (y - NewImageRect.YLow)) + (x - NewImageRect.XLow) * 3, offset = (bmp.Width * y + x) * 4;
                    q[0 + qoffset] = b[0 + offset]; q[1 + qoffset] = b[1 + offset]; q[2 + qoffset] = b[2 + offset];
                }
            bmp.UnlockBits(data);
            bmp.Dispose();
        }
        private bool CompareArrayAnd(int ___Threshold___, int[] ___CompareArray___) {
            foreach (int ___CompareValue___ in ___CompareArray___) {
                if (___Threshold___ > ___CompareValue___) continue;
                else return false;
            }
            return true;
        }
        private unsafe bool GetYLow(IplImage p_img, Threshold ImageThreshold, Rect NewImageRect) {
            byte* p = (byte*)p_img.ImageData;
            int[] TargetRowArray = new int[Var.MaxMarginSize + 1];
            for (int yy = 0; yy <= Var.MaxMarginSize; ++yy)
                for (int x = 0; x < p_img.Width; ++x)
                    if (p[p_img.WidthStep * yy + x] < ImageThreshold.Concentration) ++TargetRowArray[yy];
            if (CompareArrayAnd(ImageThreshold.Width, TargetRowArray)) {
                NewImageRect.YLow = 0;
                return true;
            }
            for (int y = 1; y < p_img.Height - Var.MaxMarginSize; y++) {
                int TargetRow = 0;
                for (int x = 0; x < p_img.Width; x++)
                    if (p[p_img.WidthStep * (y + Var.MaxMarginSize) + x] < ImageThreshold.Concentration) ++TargetRow;
                if (ImageThreshold.Width > TargetRow) {
                    NewImageRect.YLow = y - Var.MaxMarginSize < 0 ? 0 : y - Var.MaxMarginSize;
                    return true;
                }
            }
            return false;//絶対到達しない
        }
        private unsafe bool GetYHigh(IplImage p_img, Threshold ImageThreshold, Rect NewImageRect) {
            byte* p = (byte*)p_img.ImageData;
            int[] TargetRowArray = new int[Var.MaxMarginSize + 1];
            for (int yy = -Var.MaxMarginSize; yy < 1; ++yy)
                for (int x = 0; x < p_img.Width; ++x)
                    if (p[p_img.WidthStep * ((p_img.Height - 1) + yy) + x] < ImageThreshold.Concentration) ++TargetRowArray[-yy];
            if (CompareArrayAnd(ImageThreshold.Width, TargetRowArray)) {
                NewImageRect.YHigh = p_img.Height;//prb
                return true;
            }
            for (int y = p_img.Height - 2; y > (NewImageRect.YLow + Var.MaxMarginSize); --y) {//Y下取得
                int TargetRow = 0;
                for (int x = 0; x < p_img.Width; ++x)
                    if (p[p_img.WidthStep * (y - Var.MaxMarginSize) + x] < ImageThreshold.Concentration) ++TargetRow;
                if ((ImageThreshold.Width > TargetRow)) {
                    NewImageRect.YHigh = y + Var.MaxMarginSize > p_img.Height ? p_img.Height : y + Var.MaxMarginSize;//prb
                    return true;
                }
            }
            return false;//絶対到達しない
        }
        private unsafe bool GetXLow(IplImage p_img, Threshold ImageThreshold, Rect NewImageRect) {
            byte* p = (byte*)p_img.ImageData;
            int[] TargetRowArray = new int[Var.MaxMarginSize + 1];
            for (int xx = 0; xx <= Var.MaxMarginSize; ++xx)
                for (int y = NewImageRect.YLow; y < NewImageRect.YHigh; ++y)
                    if (p[xx + p_img.WidthStep * y] < ImageThreshold.Concentration) ++TargetRowArray[xx];
            if (CompareArrayAnd(ImageThreshold.Height, TargetRowArray)) {
                NewImageRect.XLow = 0;
                return true;
            }
            for (int x = 0; x < p_img.Width - Var.MaxMarginSize; x++) {//X左取得
                int TargetRow = 0;
                for (int y = NewImageRect.YLow; y < NewImageRect.YHigh; ++y)
                    if (p[x + Var.MaxMarginSize + p_img.WidthStep * y] < ImageThreshold.Concentration) ++TargetRow;
                if (ImageThreshold.Height > TargetRow) {
                    NewImageRect.XLow = x - Var.MaxMarginSize < 0 ? 0 : x - Var.MaxMarginSize;
                    return true;
                }
            }
            return false;//絶対到達しない
        }
        private unsafe bool GetXHigh(IplImage p_img, Threshold ImageThreshold, Rect NewImageRect) {
            byte* p = (byte*)p_img.ImageData;
            int[] TargetRowArray = new int[Var.MaxMarginSize + 1];
            for (int xx = -Var.MaxMarginSize; xx < 1; ++xx)
                for (int y = NewImageRect.YLow; y < NewImageRect.YHigh; ++y)
                    if (p[((p_img.Width - 1) + xx) + p_img.WidthStep * y] < ImageThreshold.Concentration) ++TargetRowArray[-xx];
            if (CompareArrayAnd(ImageThreshold.Height, TargetRowArray)) {
                NewImageRect.XHigh = p_img.Width; //prb
                return true;
            }
            for (int x = p_img.Width - 2; x > NewImageRect.XLow + Var.MaxMarginSize; --x) {//X右取得
                int TargetRow = 0;
                for (int y = NewImageRect.YLow; y < NewImageRect.YHigh; ++y)
                    if (p[x - Var.MaxMarginSize + p_img.WidthStep * y] < ImageThreshold.Concentration) ++TargetRow;
                if (ImageThreshold.Height > TargetRow) {
                    NewImageRect.XHigh = x + Var.MaxMarginSize > p_img.Width ? p_img.Width : x + Var.MaxMarginSize;//prb
                    return true;
                }
            }
            return false;
        }
        private bool GetNewImageSize(IplImage p_img, Threshold ImageThreshold, Rect NewImageRect) {
            ImageThreshold.Width = p_img.Width - ImageThreshold.Times;
            if (!GetYLow(p_img, ImageThreshold, NewImageRect))//Y上取得
                return false;
            if (!GetYHigh(p_img, ImageThreshold, NewImageRect))//X左
                return false;
            NewImageRect.Size.Height = NewImageRect.YHigh - NewImageRect.YLow;
            ImageThreshold.Height = NewImageRect.Size.Height - ImageThreshold.Times;
            if (!GetXLow(p_img, ImageThreshold, NewImageRect))//Y下取得
                return false;
            if (!GetXHigh(p_img, ImageThreshold, NewImageRect))//X右
                return false;
            NewImageRect.Size.Width = NewImageRect.XHigh - NewImageRect.XLow;
            return true;
        }
        private int GetRangeMedianF(IplImage p_img) {
            return StandardAlgorithm.Math.MakeItOdd((int)Math.Sqrt(Math.Sqrt(Image.GetShortSide(p_img) + 80)));
        }
        private byte GetConcentrationThreshold(ToneValue ImageToneValue, double MangaTextConst) {
            return (byte)((ImageToneValue.Max - ImageToneValue.Min) * MangaTextConst / Const.Tone8Bit);
        }
        private double GetMangaTextConst() {//図表がマンガ 小説がText それぞれ画像密度が違うので 閾値を変更したい、
            if (!MangaOrTextMode.Checked) {//故にこの定数を使って閾値を変える
                return 15;//小説Text
            } else {
                return 25;//図表マンガ
            }
        }
        private bool MedianLaplacianMedian(IplImage InputGrayImage, IplImage MedianImage, IplImage LaplacianImage) {
            if (!MangaOrTextMode.Checked) {
                Image.Filter.FastestMedian(InputGrayImage, MedianImage, 0);//小説Textはメディアンフィルタ適用外
            } else {//図表マンガ メディアンフィルタ実行 画像サイズに応じてマスクサイズを決める
                Image.Filter.FastestMedian(InputGrayImage, MedianImage, GetRangeMedianF(InputGrayImage));
            }
            int[] FilterMask = new int[Const.Neighborhood8];
            Image.Filter.ApplyMask(Image.Filter.SetMask.Laplacian(FilterMask), MedianImage, LaplacianImage);

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
            if (!MangaOrTextMode.Checked) {
                Image.Filter.FastestMedian(LaplacianImage, 0);//小説Textはメディアンフィルタ適用外
            } else {//図表マンガ メディアンフィルタ実行 画像サイズに応じてマスクサイズを決める
                Image.Filter.FastestMedian(LaplacianImage, GetRangeMedianF(LaplacianImage));
            }

#if (DEBUG_SAVE)
                Debug.SaveImage(LaplacianImage,nameof(LaplacianImage));//debug
#endif
#if (DEBUG_DISPLAY)
                Debug.DisplayImage(LaplacianImage,nameof(LaplacianImage));//debug
#endif
            return true;
        }
        private int GetNewHeightWidth(int[] TargetXColumnYRow, int HeightWidth, int InstanceThreshold) {
            int NewHeightWidth = 0;
            for (int xory = 0; xory < HeightWidth; ++xory) {
                if (TargetXColumnYRow[xory] > InstanceThreshold)//実態あり
                    ++NewHeightWidth;
            }
            return NewHeightWidth;
        }
        private unsafe bool UselessYRowSpacingDeletion(ref string f) {
            IplImage InputGrayImage = Cv.LoadImage(f, LoadMode.GrayScale);//
            IplImage LaplacianImage = Cv.CreateImage(InputGrayImage.Size, BitDepth.U8, 1);
            int[] FilterMask = new int[Const.Neighborhood4];
            Image.Filter.ApplyMask(Image.Filter.SetMask.Laplacian(FilterMask), InputGrayImage, LaplacianImage);
            byte* p = (byte*)LaplacianImage.ImageData;
            int[] TargetYRow = new int[LaplacianImage.Height];//TargetYRow[y]が閾値以下ならその行を削除
            for (int y = 0; y < LaplacianImage.Height; y++)
                for (int x = 0; x < LaplacianImage.Width; x++)
                    if (p[LaplacianImage.WidthStep * y + x] > 0) {
                        ++TargetYRow[y];
                    }
            int InstanceThreshold = 0;
            IplImage OutputCutImage = Cv.CreateImage(new CvSize(InputGrayImage.Size.Width, GetNewHeightWidth(TargetYRow, LaplacianImage.Height, InstanceThreshold)), BitDepth.U8, Is.GrayScale);
            byte* src = (byte*)InputGrayImage.ImageData, dst = (byte*)OutputCutImage.ImageData;
            for (int x = 0; x < InputGrayImage.Width; x++) {
                int ValidYs = 0;//有効なXの数
                for (int y = 0; y < InputGrayImage.Height; y++) {
                    if (TargetYRow[y] > InstanceThreshold) {//実態あり
                        dst[OutputCutImage.WidthStep * ValidYs + x] = src[InputGrayImage.WidthStep * y + x];
                        ++ValidYs;
                    }
                }
            }
            Cv.SaveImage(f, OutputCutImage, new ImageEncodingParam(ImageEncodingID.PngCompression, 0));
            Cv.ReleaseImage(InputGrayImage);
            Cv.ReleaseImage(LaplacianImage);
            Cv.ReleaseImage(OutputCutImage);
            return true;
        }
        private unsafe bool UselessXColumSpacingDeletion(ref string f) {
            IplImage InputGrayImage = Cv.LoadImage(f, LoadMode.GrayScale);//
            //IplImage MedianImage = Cv.CreateImage(InputGrayImage.Size, BitDepth.U8, 1);
            IplImage LaplacianImage = Cv.CreateImage(InputGrayImage.Size, BitDepth.U8, 1);
            int[] FilterMask = new int[Const.Neighborhood4];
            Image.Filter.ApplyMask(Image.Filter.SetMask.Laplacian(FilterMask), InputGrayImage, LaplacianImage);
            byte* p = (byte*)LaplacianImage.ImageData;
            int[] TargetXColumn = new int[LaplacianImage.Width];//TargetRow[x]が閾値以下ならその行を削除
            for (int y = 0; y < LaplacianImage.Height; y++)
                for (int x = 0; x < LaplacianImage.Width; x++)
                    if (p[LaplacianImage.WidthStep * y + x] > 0) {
                        ++TargetXColumn[x];
                    }
            int InstanceThreshold = 0;
            IplImage OutputCutImage = Cv.CreateImage(new CvSize(GetNewHeightWidth(TargetXColumn, LaplacianImage.Width, InstanceThreshold), InputGrayImage.Size.Height), BitDepth.U8, Is.GrayScale);
            byte* src = (byte*)InputGrayImage.ImageData, dst = (byte*)OutputCutImage.ImageData;
            for (int y = 0; y < InputGrayImage.Height; y++) {
                int ValidXs = 0;//有効なXの数
                for (int x = 0; x < InputGrayImage.Width; x++) {
                    if (TargetXColumn[x] > InstanceThreshold) {//実態あり
                        dst[OutputCutImage.WidthStep * y + ValidXs] = src[InputGrayImage.WidthStep * y + x];
                        ++ValidXs;
                    }
                }
            }
            Cv.SaveImage(f, OutputCutImage, new ImageEncodingParam(ImageEncodingID.PngCompression, 0));
            Cv.ReleaseImage(InputGrayImage);
            Cv.ReleaseImage(LaplacianImage);
            Cv.ReleaseImage(OutputCutImage);
            return true;
        }
        private unsafe void NoiseRemoveTwoArea(ref string f, byte max) {
            IplImage p_img = Cv.LoadImage(f, LoadMode.GrayScale);
            IplImage q_img = Cv.CreateImage(p_img.Size, BitDepth.U8, 1);
            Cv.Copy(p_img, q_img);
            byte* p = (byte*)p_img.ImageData, q = (byte*)q_img.ImageData;
            for (int y = 0; y < q_img.ImageSize; ++y) q[y] = p[y] < max ? (byte)0 : (byte)255;//First, binarize
            for (int y = 1; y < q_img.Height - 1; ++y) {
                int yoffset = (q_img.WidthStep * y);
                for (int x = 1; x < q_img.Width - 1; ++x)
                    if (q[yoffset + x] == 0)//Count white spots around black dots
                        for (int yy = -1; yy < 2; ++yy) {
                            int yyyoffset = q_img.WidthStep * (y + yy);
                            for (int xx = -1; xx < 2; ++xx) if (q[yyyoffset + (x + xx)] == 255) ++q[yoffset + x];
                        }
            }
            for (int y = 1; y < q_img.Height - 1; ++y) {
                int yoffset = (q_img.WidthStep * y);
                for (int x = 1; x < q_img.Width - 1; ++x) {
                    if (q[yoffset + x] == 7)//When there are seven white spots in the periphery
                        for (int yy = -1; yy < 2; ++yy) {
                            int yyyoffset = q_img.WidthStep * (y + yy);
                            for (int xx = -1; xx < 2; ++xx) {
                                int offset = yyyoffset + (x + xx);
                                if (q[offset] == 7) {//仲間 ペア
                                    p[yoffset + x] = max;//q[yoffset+p_img.NChannels*x]=6;//Unnecessary
                                    p[offset] = max; q[offset] = 6;
                                    yy = 1; break;
                                } else { };
                            }
                        }
                    else if (q[yoffset + x] == 8) p[yoffset + x] = max;//Independent
                }
            }
            Cv.SaveImage(f, p_img, new ImageEncodingParam(ImageEncodingID.PngCompression, 0));
            Cv.ReleaseImage(q_img);
            Cv.ReleaseImage(p_img);
        }
        private unsafe void NoiseRemoveWhite(ref string f, byte min) {
            IplImage p_img = Cv.LoadImage(f, LoadMode.GrayScale);
            IplImage q_img = Cv.CreateImage(p_img.Size, BitDepth.U8, 1);
            Cv.Copy(p_img, q_img);
            byte* p = (byte*)p_img.ImageData, q = (byte*)q_img.ImageData;
            for (int y = 0; y < q_img.ImageSize; ++y) q[y] = p[y] > min ? (byte)255 : (byte)0;//First, binarize
            for (int y = 1; y < q_img.Height - 1; ++y) {
                int yoffset = (q_img.WidthStep * y);
                for (int x = 1; x < q_img.Width - 1; ++x)
                    if (q[yoffset + x] == 0)//Count white spots around black dots
                        for (int yy = -1; yy < 2; ++yy) {
                            int yyyoffset = q_img.WidthStep * (y + yy);
                            for (int xx = -1; xx < 2; ++xx) if (q[yyyoffset + (x + xx)] == 0) ++q[yoffset + x];
                        }
            }
            for (int y = 1; y < q_img.Height - 1; ++y) {
                int yoffset = (q_img.WidthStep * y);
                for (int x = 1; x < q_img.Width - 1; ++x) {
                    /*if(q[yoffset+x]==7)//When there are seven white spots in the periphery
                        for(int yy=-1;yy<2;++yy) {
                            int yyyoffset = q_img.WidthStep*(y+yy);
                            for(int xx=-1;xx<2;++xx) {
                                int offset=yyyoffset+(x+xx);
                                if(q[offset]==7) {//仲間 ペア
                                    p[yoffset+x]=min;//q[yoffset+p_img.NChannels*x]=6;//Unnecessary
                                    p[offset]=min;q[offset]=6;
                                    yy=1;break;
                                } else;
                            }
                        }
                    else/**/
                    if (q[yoffset + x] == 8) p[yoffset + x] = min;//Independent
                }
            }
            Cv.SaveImage(f, p_img, new ImageEncodingParam(ImageEncodingID.PngCompression, 0));
            Cv.ReleaseImage(q_img);
            Cv.ReleaseImage(p_img);
        }
        private static unsafe bool FixPixelMissing(ref string f) {
            IplImage InputGrayImage = Cv.LoadImage(f, LoadMode.GrayScale);//
            IplImage FixedImage = Cv.CreateImage(InputGrayImage.Size, BitDepth.U8, 1);
            Cv.Copy(InputGrayImage, FixedImage);
            byte* src = (byte*)InputGrayImage.ImageData, dst = (byte*)FixedImage.ImageData;
            for (int y = 2; y < InputGrayImage.Height - 2; ++y) {
                for (int x = 2; x < InputGrayImage.Width - 2; ++x) {
                    int offset = (InputGrayImage.WidthStep * y) + x;//current position
                    if (((src[offset - 1]) == (src[offset + 1])) && ((src[offset + 1]) == (src[offset - InputGrayImage.WidthStep])) && ((src[offset + 1]) == (src[offset - InputGrayImage.WidthStep])) && ((src[offset - 2]) == (src[offset + 1])) && ((src[offset + 2]) == (src[offset + 1])) && ((src[offset - 2 * InputGrayImage.WidthStep]) == (src[offset + 1])) && ((src[offset + 2 * InputGrayImage.WidthStep]) == (src[offset + 1])))
                        dst[offset] = (src[offset + 1]);
                }
            }
            //MessageBox.Show(f);
            Cv.SaveImage(f, FixedImage, new ImageEncodingParam(ImageEncodingID.PngCompression, 0));
            Cv.ReleaseImage(InputGrayImage);
            Cv.ReleaseImage(FixedImage);
            return true;
        }
        private bool CutPNGMarginMain(ref string f, TextWriter writerSync) {
            int[] OriginHistgram = new int[Const.Tone8Bit];
            if (Image.GetHistgramR(ref f, OriginHistgram) == Is.Color) {//カラーでドット埋めは無理
            } else {
                FixPixelMissing(ref f);//ピクセル欠けを修正
                NoiseRemoveTwoArea(ref f, Image.GetToneValueMax(OriginHistgram));//小さいゴミ削除
                NoiseRemoveWhite(ref f, Image.GetToneValueMin(OriginHistgram));//小さいゴミ削除
                UselessXColumSpacingDeletion(ref f);//空白列削除
                UselessYRowSpacingDeletion(ref f);//空白行削除
            }
            IplImage InputGrayImage = Cv.LoadImage(f, LoadMode.GrayScale);//
            IplImage MedianImage = Cv.CreateImage(InputGrayImage.Size, BitDepth.U8, 1);
            IplImage LaplacianImage = Cv.CreateImage(MedianImage.Size, BitDepth.U8, 1);
            MedianLaplacianMedian(InputGrayImage, MedianImage, LaplacianImage);//MedianLaplacianMedianをかけて画像平滑化
            GetImageToneValue(f, out int Channel, out ToneValue ImageToneValue);
            if (ImageToneValue.Max == ImageToneValue.Min) {//豆腐
                Cv.ReleaseImage(InputGrayImage);
                Cv.ReleaseImage(LaplacianImage);
                return false;
            }
            //ImageThreshold.Concentration=GetConcentrationThreshold(ImageToneValue);//勾配が重要？
            Threshold ImageThreshold = new Threshold {
                Concentration = GetConcentrationThreshold(ImageToneValue, GetMangaTextConst())
            };
            Rect NewImageRect = new Rect();
            if (!GetNewImageSize(LaplacianImage, ImageThreshold, NewImageRect)) {
                Cv.ReleaseImage(InputGrayImage);
                Cv.ReleaseImage(LaplacianImage);
                return false;
            }
            Cv.ReleaseImage(LaplacianImage);
            writerSync.WriteLine(f + " threshold=" + ImageThreshold.Concentration + ",Min=" + ImageToneValue.Min + ",Max=" + ImageToneValue.Max + "\n(" + NewImageRect.XLow + "," + NewImageRect.YLow + "),(" + NewImageRect.XHigh + "," + NewImageRect.YHigh + ")\n (" + InputGrayImage.Width + "," + InputGrayImage.Height + ")->(" + NewImageRect.Size.Width + "," + NewImageRect.Size.Height + ")");//prb
            IplImage OutputCutImage = Cv.CreateImage(NewImageRect.Size, BitDepth.U8, Channel);//prb
            if (Channel == Is.GrayScale) {
                WhiteCut(InputGrayImage, OutputCutImage, NewImageRect);
                Image.Transform2Linear(OutputCutImage, ImageToneValue);// 階調値変換
            } else {//Is.Color
                WhiteCutColor(ref f, OutputCutImage, NewImageRect);//bitmapで読まないと4Byteなのか3Byteなのか曖昧なので統一は出来ない
            }
            Cv.SaveImage(f, OutputCutImage, new ImageEncodingParam(ImageEncodingID.PngCompression, 0));
            Cv.ReleaseImage(InputGrayImage);
            Cv.ReleaseImage(OutputCutImage);
            return true;
        }

        private static void GetImageToneValue(string f, out int Channel, out ToneValue ImageToneValue) {
            int[] Histgram = new int[Const.Tone8Bit];
            Channel = Image.GetHistgramR(ref f, Histgram);
            ImageToneValue = new ToneValue {
                Max = Image.GetToneValueMax(Histgram),
                Min = Image.GetToneValueMin(Histgram)
            };
        }

        private bool CutJPGMarginMain(ref string f, TextWriter writerSync) {
            IplImage InputGrayImage = Cv.LoadImage(f, LoadMode.GrayScale);//
            IplImage MedianImage = Cv.CreateImage(InputGrayImage.Size, BitDepth.U8, 1);
            IplImage LaplacianImage = Cv.CreateImage(MedianImage.Size, BitDepth.U8, 1);
            MedianLaplacianMedian(InputGrayImage, MedianImage, LaplacianImage);
            GetImageToneValue(f, Channel: out _, out ToneValue ImageToneValue);
            if (ImageToneValue.Max == ImageToneValue.Min) {//豆腐
                Cv.ReleaseImage(InputGrayImage);
                Cv.ReleaseImage(LaplacianImage);
                return false;
            }
            if (ImageToneValue.Max == ImageToneValue.Min) {
                Cv.ReleaseImage(InputGrayImage);
                Cv.ReleaseImage(LaplacianImage);
                return false;
            }
            Rect NewImageRect = new Rect();
            //ImageThreshold.Concentration=GetConcentrationThreshold(ImageToneValue);//勾配が重要？
            if (!GetNewImageSize(LaplacianImage, new Threshold { Concentration = GetConcentrationThreshold(ImageToneValue, GetMangaTextConst()) }, NewImageRect)) {
                Cv.ReleaseImage(InputGrayImage);
                Cv.ReleaseImage(LaplacianImage);
                return false;
            }
            writerSync.WriteLine(f + " (" + NewImageRect.XLow + "," + NewImageRect.YLow + "),(" + NewImageRect.XHigh + "," + NewImageRect.YHigh + ")\n (" + InputGrayImage.Width + "," + InputGrayImage.Height + ")->(" + NewImageRect.Size.Width + "," + NewImageRect.Size.Height + ")");//prb
            Cv.ReleaseImage(InputGrayImage);
            Cv.ReleaseImage(LaplacianImage);
            //jpegtran.exe -crop 808x1208+0+63 -outfile Z:\bin\22\6.jpg Z:\bin\22\6.jpg
            string Arguments = "-crop " + NewImageRect.Size.Width + "x" + NewImageRect.Size.Height + "+" + NewImageRect.XLow + "+" + NewImageRect.YLow + " -outfile \"" + f + "\" \"" + f + "\"";
            ExecuteAnotherApp("jpegtran.exe", Arguments, false, true);
            return true;
        }
        private void ExecutePNGout(string PathName) {
            IEnumerable<string> PNGFiles = System.IO.Directory.EnumerateFiles(PathName, "*.png", System.IO.SearchOption.AllDirectories);//Acquire only png files under the path.
            if (PNGFiles.Any()) {
                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();//stop watch get time
                sw.Start();
                Parallel.ForEach(PNGFiles, new ParallelOptions() { MaxDegreeOfParallelism = System.Environment.ProcessorCount }, f => {
                    ExecuteAnotherApp("pngout.exe", "\"" + f + "\"", false, true);//PNGOptimize
                });
                sw.Stop(); richTextBox1.Text += ("\npngout:" + sw.Elapsed);
            }
        }
        private void RemoveMarginEntry(string PathName) {
            using (TextWriter writerSync = TextWriter.Synchronized(new StreamWriter(DateTime.Now.ToString("yyyy.MM.dd.HH.mm.ss") + ".log", false, System.Text.Encoding.GetEncoding("shift_jis")))) {
                IEnumerable<string> PNGFiles = System.IO.Directory.EnumerateFiles(PathName, "*.png", System.IO.SearchOption.AllDirectories);//Acquire only png files under the path.
                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();//stop watch get time
                if (PNGFiles.Any()) {
                    sw.Start();
                    Parallel.ForEach(PNGFiles, new ParallelOptions() { MaxDegreeOfParallelism = System.Environment.ProcessorCount }, f => {//Specify the number of concurrent threads(The number of cores is reasonable).
                        CutPNGMarginMain(ref f, writerSync);
                    });
                    writerSync.WriteLine(DateTime.Now.ToString("yyyy.MM.dd.HH.mm.ss"));
                    sw.Stop(); richTextBox1.Text += ("\nPNGWhiteRemove:" + sw.Elapsed);
                }
                IEnumerable<string> JPGFiles = System.IO.Directory.EnumerateFiles(PathName, "*.jpg", System.IO.SearchOption.AllDirectories);//Acquire only png files under the path.
                if (JPGFiles.Any()) {
                    sw.Restart();
                    Parallel.ForEach(JPGFiles, new ParallelOptions() { MaxDegreeOfParallelism = System.Environment.ProcessorCount }, f => {//Specify the number of concurrent threads(The number of cores is reasonable).
                        CutJPGMarginMain(ref f, writerSync);
                    });
                    writerSync.WriteLine(DateTime.Now.ToString("yyyy.MM.dd.HH.mm.ss"));
                    sw.Stop(); richTextBox1.Text += ("\nJPGWhiteRemove:" + sw.Elapsed + "\n");
                }
            }

        }
        private int GetFileNameBeforeChange(IEnumerable<string> files, string[] AllOldFileName) {//ゴミファイルを除去 JPG jpeg PNG png種々あるので
            int MaxFile = -1;
            foreach (string f in files) {
                FileInfo file = new FileInfo(f);
                if (file.Extension == ".db" || file.Extension == ".ini") file.Delete();//Disposal of garbage
                else AllOldFileName[++MaxFile] = f;
            }
            return ++MaxFile;
        }
        /*
        (file.Name.Length-file.Extension.Length)拡張子を除いたファイル名長さ
        ファイル名のサイズを三桁以上にして先頭にzを付加
        ファイル名に重複がある場合どうにもならないのでfalseを返す
         */
        private bool SortFiles(int MaxFile, string PathName, string[] AllOldFileName) {
            for (int i = MaxFile - 1; i >= 0; --i) {//尻からリネームしないと終わらない?
                FileInfo file = new FileInfo(AllOldFileName[i]);
                while ((file.Name.Length - file.Extension.Length) < 3)
                    if (System.IO.File.Exists(PathName + "/0" + file.Name)) {//重複
                        richTextBox1.Text += "\n:" + PathName + "/0" + file.Name + ":Exists";
                        return false;
                    } else file.MoveTo((PathName + "/0" + file.Name));//0->000  1000枚までしか無理 7zは650枚
                if ((file.Name[0] != 'z')) file.MoveTo((PathName + "/z" + file.Name));//000->z000
            }
            return true;
        }
        private void CreateNewFileName(int MaxFile, string[] NewFileName) {
            if (radioButton2.Checked && MaxFile <= 26 * 25) {//7zip under 26*25=650
                int MaxRoot = (int)Math.Sqrt(MaxFile) + 1;
                richTextBox1.Text += "\nroot MaxRoot" + MaxRoot;
                for (int i = 0; i < NewFileName.Length; ++i) NewFileName[i] = (char)((i / MaxRoot) + 'a') + ((char)(i % MaxRoot + 'a')).ToString();//26*25  36*35mezasu
            } else if (MaxFile < 35) {//一桁で1-9,a-y
                for (int i = 0; i < NewFileName.Length && i < 10; ++i) NewFileName[i] = i.ToString();//0 ~ 9
                for (int i = 10; i < NewFileName.Length; ++i) NewFileName[i] = ((char)((i - 10) + 'a')).ToString();//a~y
            } else {//zip under 36*25+100=1000
                for (int i = 0; (i < NewFileName.Length) && (i < 100); ++i) NewFileName[i] = i.ToString();//0 ~ 99 zipではこの法が軽い
                if (MaxFile < 100) return;
                char[] y1 = new char[36];
                for (int i = 0; i < 10; ++i) y1[i] = (char)(i + '0');//0 ~ 9
                for (int i = 10; i < y1.Length; ++i) y1[i] = (char)(i - 10 + 'a');//a~y
                for (int i = 100; i < NewFileName.Length; ++i) NewFileName[i] += (char)(((i - 100) / 36) + 'a') + (y1[(i - 100) % 36]).ToString();
            }
        }
        private void CarmineCliAuto(string PathName) {//ハフマンテーブルの最適化によってjpgサイズを縮小
            IEnumerable<string> files = System.IO.Directory.EnumerateFiles(PathName, "*.jpg", System.IO.SearchOption.AllDirectories);//Acquire only jpg files under the path.
            if (files.Any()) {
                Parallel.ForEach(files, new ParallelOptions() { MaxDegreeOfParallelism = System.Environment.ProcessorCount }, f => {//マルチスレッド化するのでファイル毎
                    ExecuteAnotherApp("carmine_cli.exe", "\"" + f + "\" -o", false, true);
                });
            }
        }
        private void ExecuteAnotherApp(string FileName, string Arguments, bool UseShellExecute, bool CreateNoWindow) {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                FileName = FileName,
                Arguments = Arguments,
                UseShellExecute = UseShellExecute,
                CreateNoWindow = CreateNoWindow    // コンソール・ウィンドウを開
            }).WaitForExit();
        }
        private void CreateZip(string PathName) {
            string Extension = ".zip";
            string FileName = "Rar.exe";
            string Arguments;
            if (radioButton3.Checked) {//winrar
                Extension = ".rar";
                if (radioButton6.Checked)//non compress
                    Arguments = " a \"" + PathName + ".rar\" -rr5 -mt" + System.Environment.ProcessorCount + "-m0 -ep \"" + PathName + "\"";
                else//compress level max
                    Arguments = " a \"" + PathName + ".rar\" -rr5 -mt" + System.Environment.ProcessorCount + " -m5 -ep \"" + PathName + "\"";
                //MessageBox.Show(App.Arguments);
                /*
                a             書庫にファイルを圧縮
                rr[N]         リカバリレコードを付加
                m<0..5>       圧縮方式を指定 (0-無圧縮...5-標準...5-最高圧縮)
                mt<threads>   スレッドの数をセット
                ep            名前からパスを除外/**/
            } else {
                FileName = "7z.exe";
                if (radioButton2.Checked) Extension = ".7z";
                if (radioButton5.Checked)
                    Arguments = "a \"" + PathName + Extension + "\" -mmt=on \"" + PathName + "\\*\"";
                else if (radioButton4.Checked)
                    Arguments = "a \"" + PathName + Extension + "\" -mmt=on -mx9 \"" + PathName + "\\*\"";
                else
                    Arguments = "a \"" + PathName + Extension + "\" -mmt=on -mx0 \"" + PathName + "\\*\"";
            }
            ExecuteAnotherApp(FileName, Arguments, false, true);
            RenameNumberOnlyFile(PathName, Extension);
        }
        private string GetNumberOnlyPath(string PathName) {//ファイル名からX巻のXのみを返す
            string FileName = System.IO.Path.GetFileName(PathName);//Z:\[宮下英樹] センゴク権兵衛 第05巻 ->[宮下英樹] センゴク権兵衛 第05巻
            Match MatchedNumber = Regex.Match(FileName, "(\\d)+巻");//[宮下英樹] センゴク権兵衛 第05巻 ->05巻
            if (MatchedNumber.Success)
                MatchedNumber = Regex.Match(MatchedNumber.Value, "(\\d)+");//05巻->05
            else {
                MatchedNumber = Regex.Match(FileName, "(\\d)+");//[宮下英樹] センゴク権兵衛 第05 ->05
                if (!MatchedNumber.Success)
                    return PathName;//[宮下英樹] センゴク権兵衛 第 ->
            }
            //文字列を置換する（FileNameをMatchedNumber.Valueに置換する）
            return PathName.Replace(FileName, int.Parse(MatchedNumber.Value).ToString());//Z:\5
        }
        private void RenameNumberOnlyFile(string PathName, string Extension) {
            string NewFileName = GetNumberOnlyPath(PathName) + Extension;
            if (!System.IO.File.Exists(NewFileName))//重複
                File.Move(PathName + Extension, NewFileName);//重複してない
            richTextBox1.Text += NewFileName + ".this folder finish.\n";//Show path
        }
        private bool IsTheNumberOfFilesAppropriate(int MaxFile) {
            if (MaxFile > (36 * 25) + 100) {
                richTextBox1.Text += "\nMaxFile:" + MaxFile + " => over 1,000\n";
                return false;
            } else if (MaxFile < 1) {
                richTextBox1.Text += "\nMaxFile:" + MaxFile + " 0";
                return false;
            } else {
                richTextBox1.Text += "\nMaxFile:" + MaxFile + ":OK.";
                return true;
            }
        }
        private void FileProcessing(System.Collections.Specialized.StringCollection filespath) {
            foreach (string PathName in filespath) {//Enumerate acquired paths
                logs.Items.Add(PathName);
                richTextBox1.Text += PathName;//Show path
                if (File.GetAttributes(PathName).HasFlag(FileAttributes.Directory)) {//フォルダ
                    IEnumerable<string> files = System.IO.Directory.EnumerateFiles(PathName, "*", System.IO.SearchOption.TopDirectoryOnly);//Acquire  files  the path.
                    string[] AllOldFileName = new string[files.Count()];//36*25+100 ファイル数 ゴミ込み
                    int MaxFile = GetFileNameBeforeChange(files, AllOldFileName);//ゴミ処理
                    if (WhetherToRename.Checked)//リネームするか？
                        if (IsTheNumberOfFilesAppropriate(MaxFile))//個数
                            if (SortFiles(MaxFile, PathName, AllOldFileName))//ソートできるファイルか
                                RenameEntry(PathName, files, MaxFile);//リネームする
                    if (OptimizeTheImages.Checked)
                        RemoveMarginEntry(PathName);
                    if (PNGout.Checked)
                        ExecutePNGout(PathName);
                    CarmineCliAuto(PathName);
                    CreateZip(PathName);
                } else {//ファイルはnewをつくりそこで実行
                    string NewPath = System.IO.Path.GetDirectoryName(PathName) + "\\new\\";//"\\new"
                    string NewFilePath = NewPath + Path.GetFileName(PathName);//"\\new\\hoge.jpg"
                    System.IO.Directory.CreateDirectory(NewPath);
                    System.IO.File.Copy(PathName, NewFilePath, true);
                    if (OptimizeTheImages.Checked)
                        RemoveMarginEntry(NewPath);//該当ファイルのあるフォルダの奴はすべて実行される別フォルダに単体コピーが理想
                    if (PNGout.Checked)
                        ExecutePNGout(NewPath);
                }
            }
        }
        private void RenameEntry(string PathName, IEnumerable<string> files, int MaxFile) {
            string[] NewFileName = new string[MaxFile];
            CreateNewFileName(MaxFile, NewFileName);
            ReNameAlfaBeta(PathName, ref files, NewFileName);
            ScrollAllTextBox();
        }
        private void ScrollAllTextBox() {
            richTextBox1.SelectionStart = richTextBox1.Text.Length;//末尾に移動
            richTextBox1.ScrollToCaret();
            logs.TopIndex = logs.Items.Count - 1;
        }
        //JudgeFileOrDirectory FileProcessing
        private void button1_Click(object sender, EventArgs e) {
            if (Clipboard.ContainsFileDropList()) {//Check if clipboard has file drop format data.
                FileProcessing(Clipboard.GetFileDropList());
            } else {//Check if clipboard has file drop format data.
                MessageBox.Show("Please select folders.");
            }
        }
        private void BrowserButtonClick(object sender, EventArgs e) {//Folder dialog related.
            FolderBrowserDialog fbd = new FolderBrowserDialog {
                Description = "Please specify a folder.",//Specify explanatory text to be displayed at the top.
                SelectedPath = @"Z:\download\",//Specify the folder to select first // It must be a folder under RootFolder
                ShowNewFolderButton = false//not AlLow users to create new folders
            };//Create an instance of the FolderBrowserDialog class
            fbd.ShowDialog(this);//Display a dialog
            richTextBox1.Text = fbd.SelectedPath;//Show path
            Clipboard.SetFileDropList(new System.Collections.Specialized.StringCollection() { fbd.SelectedPath });//コピーするファイルのパスをStringCollectionに追加する. Copy to clipboard
        }
        private void MangaOrTextMode_CheckedChanged(object sender, EventArgs e) {
            MangaOrTextMode.Text = "Text mode";
            if (MangaOrTextMode.Checked)
                MangaOrTextMode.Text = "Manga mode";
        }
        private void DoNotOptimizeTheImages_CheckedChanged(object sender, EventArgs e) {
            MangaOrTextMode.Visible = true;
            if (DoNotOptimizeTheImages.Checked)
                MangaOrTextMode.Visible = false;
        }
    }
}