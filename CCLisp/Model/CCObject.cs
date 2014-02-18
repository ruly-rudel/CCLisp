
namespace CCLisp.Model
{
    public abstract class CCObject
    {
    }

    public class CCCons : CCObject
    {
        public CCObject car { get; set; }
        public CCObject cdr { get; set; }

        public override string ToString()
        {
            return "(" + car.ToString() + " " + cdr.ToString() + ")";
        }
    }

    public class CCInt : CCObject
    {
        public int value { get; set; }

        public override string ToString()
        {
            return value.ToString();
        }
    }

    public class CCSymbol : CCObject
    {
        public string Name { get; set; }
        public CCObject Value { get; set; }

        public override string ToString()
        {
            return Name.ToString();
        }
    }

    public class CCParenL : CCObject
    {
        public override string ToString()
        {
            return "(";
        }
    }

    public class CCParenR : CCObject
    {
        public override string ToString()
        {
            return ")";
        }
    }

    public class CCNil : CCObject
    {
        public override string ToString()
        {
            return "nil";
        }
    }

    public class CCISAP : CCObject
    {
        public override string ToString()
        {
            return "[IS:AP]";
        }
    }
}
