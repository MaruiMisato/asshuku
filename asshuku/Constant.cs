static class Is{//主としてif文で使う               変数名に焦点 
    public const int Color = 3; 
    public const int GrayScale = 1; 
    public const bool DESCENDING_ORDER = true;//Value is meaningless
    public const bool ASCENDING_ORDER = false;//Value is meaningless
}
static class Const{//配列の宣言で使うことが多い 値に焦点
    public const int Tone8Bit=256;
    public const int Neighborhood8=9;
    public const int Neighborhood4=5;
}
static class Var{//配列の宣言で使うことが多い 不変普遍定数ではない　ユーザが勝手に変えろ
    public const int MaxMarginSize=4;//実際は＋1
}