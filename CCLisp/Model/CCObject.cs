
using System;
using System.Collections.Generic;
using System.Linq;

namespace CCLisp.Model
{
    [Serializable]
    public abstract class CCObject
    {
    }

    [Serializable]
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
            var p = new List<CCCons>();
            var r = new List<CCCons>();
            return ToStringBegin(p, ref r);
        }

        public string ToStringBegin(List<CCCons> p, ref List<CCCons> r)
        {
            // existence check
            var s = p.IndexOf(this);
            if (s >= 0)
            {
                r.Add(this);
                return "#" + s.ToString();
            }
            else
            {
                var ret = ToStringCont(p, ref r);
                var ss = r.IndexOf(this);
                if (ss >= 0)
                {
                    var i = p.IndexOf(this);
                    return "#" + i.ToString() + "=(" + ret;
                }
                else
                {
                    return "(" + ret;
                }
            }
        }

        public string ToStringCont(List<CCCons> p, ref List<CCCons> r)
        {
            var ret = "";

            // existence check
            var s = p.IndexOf(this);
            if (s >= 0)
            {
                r.Add(this);
                return "#" + s.ToString();
            }
            p.Add(this);

            // print car
            if(car == null)
            {
                ret += "nil";
            }
            else if(car.GetType() == typeof(CCCons))
            {
                ret += (car as CCCons).ToStringBegin(p, ref r);
            }
            else
            {
                ret += car.ToString();
            }

            // print cdr
            if (cdr == null)
            {
                ret += ")";
            }
            else if (cdr.GetType() == typeof(CCCons))
            {
                ret += " " + (cdr as CCCons).ToStringCont(p, ref r);
            }
            else
            {
                ret += " . " + cdr.ToString() + ")";
            }

            return ret;
        }
    }

    [Serializable]
    public class CCT : CCObject
    {
        public override string ToString()
        {
            return "t";
        }
    }

    [Serializable]
    public class CCInt : CCObject
    {
        public int value { get; set; }

        public override string ToString()
        {
            return value.ToString();
        }
    }

    [Serializable]
    public class CCIdentifier : CCObject
    {
        public string Name { get; set; }

        public override string ToString()
        {
            return Name;
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

    [Serializable]
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
}
