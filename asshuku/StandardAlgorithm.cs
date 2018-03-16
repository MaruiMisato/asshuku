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
}