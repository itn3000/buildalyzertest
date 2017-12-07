namespace BuildalyzerTest.LibUser.Sub
{
    public class SubClass
    {
        public int X{get;set;}
        public string Y = "abc";
        public class SubSubClass
        {
            public int Z{get;set;}
        }
    }
    internal struct SubStruct
    {
        public int A{get;set;}
        public string B;
    }
}