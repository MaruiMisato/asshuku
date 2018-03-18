public class StandardAlgorithm{
    public class Sort{
        public static void Bubble(byte[] numbers){
            for (int i = 0; i < (numbers.Length - 1); i++)
                for (int j = (numbers.Length - 1); j > i; j--) 
                    if (numbers[j-1] > numbers[j]) {
                        byte temp = numbers[j-1];
                        numbers[j-1] = numbers[j];
                        numbers[j] = temp;
                    }
        }  
    }
    public class Math{
        public static int MakeItOdd(int Integer){
            return (Integer - ((Integer+1)&1));//奇数にしたい -1は奇数
        }
        public static int MakeItEven(int Integer){
            return (Integer - (Integer&1));//偶数にしたい
        }
    }
}