using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCLisp.Model
{
    class CCParser
    {
        private string SpecialChar = "(),'`@";

        private CCParenL ParenL = new CCParenL();
        private CCParenR ParenR = new CCParenR();
        private CCQuote  Quote = new CCQuote();
        private CCBackQuote BackQuote = new CCBackQuote();
        private CCComma Comma = new CCComma();
        private CCAtmark Atmark = new CCAtmark();

        public IEnumerable<CCObject> Read(TextReader sr)
        {
            return Parse(Scan(sr));
        }

        //
        // parser
        //
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
            else if(ts.Current == Quote)
            {
                if (!ts.MoveNext()) // to body
                {
                    throw new CCParserException();
                }

                return QuoteObject(ParseBasicForm(ts));
            }
            else if(ts.Current == BackQuote)
            {
                if (!ts.MoveNext()) // to body
                {
                    throw new CCParserException();
                }

                return ParseBackQuotedBasicForm(ts);
            }
            else
            {
                return ts.Current;
            }
        }

        private CCObject QuoteObject(CCObject obj)
        {
            return new CCCons(new CCIdentifier() { Name = "quote" }, new CCCons(obj, null));
        }

        private CCObject ParseList(IEnumerator<CCObject> ts)
        {
            if (!ts.MoveNext()) // to car
            {
                // no car exists
                throw new CCParserException();
            }

            if (ts.Current == ParenR) // end of list
            {
                return null;
            }

            var list = new CCCons(null, null);
            list.car = ParseBasicForm(ts);
            list.cdr = ParseList(ts);

            return list;
        }


        private CCObject ParseBackQuotedBasicForm(IEnumerator<CCObject> ts)
        {
            if (ts.Current == ParenL)
            {
                return ParseBackQuotedList(ts);
            }
            else if (ts.Current == Comma)
            {
                if (!ts.MoveNext()) // to body
                {
                    throw new CCParserException();
                }

                if (ts.Current == Atmark)
                {
                    if (!ts.MoveNext()) // to body
                    {
                        throw new CCParserException();
                    }

                    return ts.Current;
                }
                else
                {
                    return new CCCons(new CCIdentifier() {Name = "list"}, new CCCons(ts.Current, null));
                }
            }
            else
            {
                return new CCCons(new CCIdentifier() { Name = "list" }, new CCCons(QuoteObject(ts.Current), null));
            }
        }

        private CCObject ParseBackQuotedList(IEnumerator<CCObject> ts)
        {
            return new CCCons(new CCIdentifier() { Name = "append" }, ParseBackQuotedListCont(ts));
        }

        private CCObject ParseBackQuotedListCont(IEnumerator<CCObject> ts)
        {
            if (!ts.MoveNext()) // to car
            {
                // no car exists
                throw new CCParserException();
            }

            if (ts.Current == ParenR) // end of list
            {
                return null;
            }

            var list = ParseBackQuotedBasicForm(ts);
            return new CCCons(list, ParseBackQuotedListCont(ts));
        }

        //
        // scanner
        //
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
                        else if(c == '\'')
                        {
                            yield return Quote;
                        }
                        else if (c == '`')
                        {
                            yield return BackQuote;
                        }
                        else if (c == ',')
                        {
                            yield return Comma;
                        }
                        else if (c == '@')
                        {
                            yield return Atmark;
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                    }
                }
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
                            st = 3;
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

                    case 3: // first minus
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
                if (sb.ToString() == "nil")
                {
                    return null;
                }
                else
                {
                    //return GetSymbol(sb.ToString());
                    return new CCIdentifier()
                    {
                        Name = sb.ToString()
                    };
                }
            }
        }
    }
}
