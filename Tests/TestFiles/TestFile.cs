namespace RoseLib
{
    public class TestFile : Object
    {
        public TestFile()
        {
               
        }

        public void Method1(int par1, List<string> par2)
        {
            foreach (var item in par2)
            {
                if(item.Length < 20)
                {
                    Console.WriteLine("It is less than 20");
                }
                else
                {
                    Console.WriteLine("It is less than 20");
                }
            }
        }
    }
}
