
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

    public class CCIdentifier : CCObject
    {
        public string Name { get; set; }

        public override string ToString()
        {
            return Name;
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() == typeof(CCIdentifier))
            {
                return (obj as CCIdentifier).Name.Equals(Name);
            }
            else
            {
                return false;
            }
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

    public class CCISIF : CCObject
    {
        public override string ToString()
        {
            return "[IS:IF]";
        }
    }

    public class CCISHALT : CCObject
    {
        public override string ToString()
        {
            return "[IS:HALT]";
        }
    }

}
