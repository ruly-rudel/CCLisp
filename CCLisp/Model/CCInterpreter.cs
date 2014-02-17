using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCLisp.Model
{
    public class CCInterpreter
    {
        public string LogString = "";

        private string SpecialChar = "()";

        private CCParenL ParenL = new CCParenL();
        private CCParenR ParenR = new CCParenR();

        private Dictionary<string, CCSymbol> Symbols = new Dictionary<string, CCSymbol>();

        public IEnumerable<CCObject> Read(StringReader sr)
        {
            return Parse(Scan(sr));
        }

        public CCObject Eval(CCObject obj)
        {
            return obj;
        }

        private IEnumerable<CCObject> Parse(IEnumerator<CCObject> ts)
        {
            while (ts.MoveNext())
            {
                yield return ParseBasicForm(ts);
            }
        }

        private CCObject ParseBasicForm(IEnumerator<CCObject> ts)
        {
            if (ts.Current == ParenL)
            {
                return ParseList(ts);
            }
            else
            {
                return ts.Current;
            }
        }

        private CCObject ParseList(IEnumerator<CCObject> ts)
        {
            ts.MoveNext();  // to car
            return ParseListContinue(ts);
        }

        private CCObject ParseListContinue(IEnumerator<CCObject> ts)
        {
            if (ts.Current == ParenR) // null list
            {
                return null;
            }

            var list = new CCCons();
            if (ts.Current == ParenL)
            {
                list.car = ParseList(ts);
            }
            else
            {
                list.car = ts.Current;
            }

            ts.MoveNext();  // to cdr
            list.cdr = ParseListContinue(ts);

            return list;
        }

        private IEnumerator<CCObject> Scan(TextReader cs)
        {
            while (cs.Peek() != -1)
            {
                if (Char.IsWhiteSpace((char)cs.Peek()))
                {
                    cs.Read();
                }
                else
                {
                    var c = (char)cs.Peek();
                    var sel = from x in SpecialChar.ToCharArray() where x == c select x;
                    if (sel.Count() == 0)
                    {
                        yield return ReadToWhiteSpace(cs);
                    }
                    else
                    {
                        cs.Read();
                        if (c == '(')
                        {
                            yield return ParenL;
                        }
                        else if (c == ')')
                        {
                            yield return ParenR;
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                    }
                }
            }
        }

        private CCSymbol GetSymbol(string name)
        {
            if (Symbols.ContainsKey(name))
            {
                return Symbols[name];
            }
            else
            {
                var s = new CCSymbol()
                {
                    Name = name
                };
                Symbols[name] = s;
                return s;
            }
        }

        private CCObject ReadToWhiteSpace(TextReader tr)
        {
            int st = 0;

            var sb = new StringBuilder();
            while (tr.Peek() != -1 && !Char.IsWhiteSpace((char)tr.Peek()) && (from x in SpecialChar.ToCharArray() where x == tr.Peek() select x).Count() == 0)
            {
                // state machine
                switch (st)
                {
                    case 0: // first char
                        if ((char)tr.Peek() == '-')
                        {
                            st = 1;
                        }
                        else if (Char.IsDigit((char)tr.Peek()))
                        {
                            st = 1;
                        }
                        else
                        {
                            st = 2;
                        }
                        break;


                    case 1: // maybe number
                        if (Char.IsDigit((char)tr.Peek()))
                        {
                            st = 1;
                        }
                        else
                        {
                            st = 2;
                        }
                        break;

                    default:    // symbol
                        break;
                }
                sb.Append((char)tr.Read());
            }

            if (st == 1)
            {
                return new CCInt()
                {
                    value = int.Parse(sb.ToString())
                };
            }
            else
            {
                return GetSymbol(sb.ToString());
            }
        }
    }
}
