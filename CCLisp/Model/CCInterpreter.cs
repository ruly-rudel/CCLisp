using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CCLisp.Model
{
    public class CCInterpreter
    {
        public string LogString = "";

        private string SpecialChar = "()";

        // single oblects (must make it singleton?
        private CCParenL ParenL = new CCParenL();
        private CCParenR ParenR = new CCParenR();
        private CCNil    Nil    = new CCNil();

        // simbol dictionary
        private Dictionary<string, CCSymbol> Symbols = new Dictionary<string, CCSymbol>();

        // evaluation environments
        private CCObject stack;
        private CCObject Stack
        {
            get
            {
                if (stack.GetType() == typeof(CCNil))
                {
                    return Nil;
                }
                else
                {
                    var top = stack as CCCons;
                    stack = top.cdr;
                    return top.car;
                }
            }

            set
            {
                var top = new CCCons();
                top.car = value;
                top.cdr = stack;
                stack = top;
            }
        }

        private CCObject env;
        private CCObject Env
        {
            get
            {
                return env;
            }

            set
            {
                env = value;
            }
        }

        private CCObject code;
        private CCObject Code
        {
            get
            {
                if (code.GetType() == typeof(CCNil))
                {
                    return Nil;
                }
                else if(code.GetType() == typeof(CCCons))
                {
                    var top = code as CCCons;
                    code = top.cdr;
                    return top.car;
                }
                else
                {
                    var top = code;
                    code = Nil;
                    return top;
                }
            }

            set
            {
                var top = new CCCons();
                top.car = value;
                top.cdr = code;
                code = top;
            }            
        }

        private CCObject dump;
        private CCObject Dump
        {
            get
            {
                if (dump.GetType() == typeof(CCNil))
                {
                    return Nil;
                }
                else
                {
                    var top = dump as CCCons;
                    dump = top.cdr;
                    return top.car;
                }
            }

            set
            {
                var top = new CCCons();
                top.car = value;
                top.cdr = dump;
                dump = top;
            }
        }

        public CCInterpreter()
        {
            // clear environment
            stack = Nil;
            env = Nil;
            code = Nil;
            dump = Nil;

            // make special simbol t
            var t = new CCSymbol();
            t.Name = "t";
            t.Value = t;
            Symbols["t"] = t;
        }

        public IEnumerable<CCObject> Read(StringReader sr)
        {
            return Parse(Scan(sr));
        }

        public void Eval(CCObject obj)
        {
            if (obj.GetType() == typeof(CCInt)) // number
            {
                Stack = obj;
            }
            else if(obj.GetType() == typeof(CCSymbol))  // symbol
            {
                var sym = obj as CCSymbol;
                if (sym.Value != null)
                {
                    Stack = sym;
                }
                else
                {
                    throw new CCRuntimeSymbolValueIsNotBoundException(stack, env, code, dump);
                }
            }
            else if (obj.GetType() == typeof(CCIdentifier)) // execute binding
            {
                var id = obj as CCIdentifier;
                if(Symbols.ContainsKey(id.Name))
                {
                    var sym = Symbols[id.Name];
                    if (sym.Value != null)
                    {
                        Stack = sym;
                    }
                    else
                    {
                        throw new CCRuntimeSymbolValueIsNotBoundException(stack, env, code, dump);
                    }
                }
                else
                {
                    throw new CCRuntimeSymbolIsNotFoundException(stack, env, code, dump);
                }

            }
            else if (obj.GetType() == typeof(CCCons))   // execute apply
            {
                SetEvalTop(obj as CCCons);
                while (EvalTop().GetType() != typeof(CCISHALT))
                    ;
            }
            else if (obj.GetType() == typeof(CCNil))    // nil
            {
                Stack = Nil;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private void SetEvalTop(CCCons obj)
        {
            if(obj.car.GetType() == typeof(CCIdentifier))
            {
                Code = new CCISHALT();
                var car = obj.car as CCIdentifier;

                // special forms
                switch(car.Name)
                {
                    case "if":
                        SetEvalTopWithIf(obj);
                        break;

                    default:    // assume normal function application;
                        SetEvalTopWithApply(obj);
                        break;
                }
                
            }
            else
            {
                SetEvalTopWithApply(obj);
            }
        }

        private void SetEvalTopWithApply(CCCons obj)
        {
            // push apply function
            var ap = new CCCons();
            ap.car = new CCISAP();
            ap.cdr = (obj as CCCons).car;
            Code = ap;

            CCObject p = obj.cdr;
            while (p.GetType() != typeof(CCNil))
            {
                var pp = p as CCCons;
                Code = pp.car;
                p = pp.cdr;
            }
        }

        private void SetEvalTopWithIf(CCCons obj)
        {
            CCCons c1 = obj.cdr as CCCons;
            CCCons c2 = c1.cdr as CCCons;
            CCCons c3 = c2.cdr as CCCons;

            CCObject cond = c1.car;
            CCObject tbr = c2.car;
            CCObject fbr = c3.car;


            // push if function
            var iffn = new CCCons();
            iffn.car = new CCISIF();

            var br = new CCCons();
            br.car = tbr;
            br.cdr = fbr;
            iffn.cdr = br;
            Code = iffn;

            Code = cond;
        }

        private CCObject EvalTop()
        {
            var obj = Code;

            if(obj.GetType() == typeof(CCISHALT))
            {
                return obj;
            }
            else if (obj.GetType() == typeof(CCCons) && (obj as CCCons).car.GetType() == typeof(CCISAP))
            {
                var sym = (obj as CCCons).cdr as CCIdentifier;
                CCInt a, b, r;

                // bulitin functions
                switch (sym.Name)
                {
                    case "+":
                        a = Stack as CCInt;
                        b = Stack as CCInt;

                        r = new CCInt();
                        r.value = a.value + b.value;
                        Stack = r;
                        return r;

                    default: // normal apply
                        throw new NotImplementedException();
                }
            }
            else if(obj.GetType() == typeof(CCCons) && (obj as CCCons).car.GetType() == typeof(CCISIF))
            {
                var br = (obj as CCCons).cdr as CCCons;
                var cond = Stack;
                if (cond.GetType() == typeof(CCNil))
                {
                    Eval(br.cdr);
                }
                else
                {
                    Eval(br.car);
                }
                return cond;    // must be fixed;
            }
            else if(obj.GetType() == typeof(CCNil))
            {
                Stack = Nil;
                return Nil;
            }
            else
            {
                // DUM
                Dump = code;
                Dump = env;
                Dump = stack;

                // setup
                stack = Nil;
                code = Nil;

                // eval
                Eval(obj);

                // RET
                var s1 = Stack;
                stack = Dump;
                env = Dump;
                code = Dump;
                Stack = s1;

                return s1;
            }
        }

        public CCObject GetResult()
        {
            return Stack;
        }

        //
        // evaluator
        //
        //private CCObject EvalCons(CCCons obj)
        //{
        //    // check special forms
        //    if(obj.car.GetType() == typeof(CCSymbol))
        //    {
        //        var car = obj.car as CCSymbol;
        //        switch (car.Name)
        //        {
        //            case "+":
        //                break;

        //            default:    // apply
        //                throw new NotImplementedException();
        //        }
        //    }
        //}

        private CCObject Apply(CCObject obj)
        {
            return Nil;
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
            else
            {
                return ts.Current;
            }
        }

        private CCObject ParseList(IEnumerator<CCObject> ts)
        {
            if (!ts.MoveNext()) // to car
            {
                // no car exists
                throw new CCParserException();
            }
            return ParseListContinue(ts);
        }

        private CCObject ParseListContinue(IEnumerator<CCObject> ts)
        {
            if (ts.Current == ParenR) // end of list
            {
                return Nil;
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

            if(!ts.MoveNext()) // to cdr
            {
                // no cdr exists
                throw new CCParserException();
            }
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
                if (sb.ToString() == "nil")
                {
                    return Nil;
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
