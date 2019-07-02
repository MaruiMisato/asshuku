using OpenCvSharp;
//using System.Drawing.Imaging;
public class Debug{
    public static void DisplayImage(IplImage p_img,string WindowName){//debug
        Cv.NamedWindow(WindowName);
        Cv.ShowImage(WindowName,p_img);
        Cv.WaitKey();            
        Cv.DestroyWindow(WindowName);
    }
    public static void SaveImage(IplImage p_img,string FileName){
        Cv.SaveImage(FileName+".png",p_img,new ImageEncodingParam(ImageEncodingID.PngCompression,0));
    }
}