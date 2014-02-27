
namespace CCLisp.Model
{
    public abstract class CCObject
    {
    }

    public class CCCons : CCObject
    {
        public CCObject car { get; set; }
        public CCObject cdr { get; set; }
        public CCObject cadr
        {
            get
            {
                return (cdr as CCCons).car;
            }
        }
        public CCObject caddr
        {
            get
            {
                return ((cdr as CCCons).cdr as CCCons).car;
            }
        }


        public CCCons(CCObject a, CCObject d)
        {
            car = a;
            cdr = d;
        }

        public override string ToString()
        {
            if(cdr.GetType() == typeof(CCCons))
            {
                return "(" + car.ToString() + " " + (cdr as CCCons).ToStringCont();
            }
            else
            {
                return "(" + car.ToString() + " . " + cdr.ToString() + ")";
            }
        }

        public string ToStringCont()
        {
            if(cdr.GetType() == typeof(CCNil)) 
            {
                return car.ToString() + ")";
            }
            else if(cdr.GetType() == typeof(CCCons))
            {
                return car.ToString() + " " + (cdr as CCCons).ToStringCont();
            }
            else
            {
                return car.ToString() + " . " + cdr.ToString();
            }
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

    public class CCIS : CCObject
    {
        public string Inst;

        public CCIS(string inst)
        {
            Inst = inst;
        }

        public override string ToString()
        {
            return "[IS:" + Inst + "]";
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
