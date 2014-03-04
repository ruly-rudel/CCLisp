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

        private string[] Builtin = {"+", "-", "*", "/", "car", "cdr", "cons"};

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
                var top = new CCCons(value, stack);
                stack = top;
            }
        }

        private CCObject env;
        private CCObject Env
        {
            get
            {
                if (env.GetType() == typeof(CCNil))
                {
                    return Nil;
                }
                else
                {
                    var top = env as CCCons;
                    env = top.cdr;
                    return top.car;
                }
            }

            set
            {
                var top = new CCCons(value, env);
                env = top;
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
                var top = new CCCons(value, code);
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
                var top = new CCCons(value, dump);
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

        public CCObject Compile(CCObject obj)
        {
            var cont = new CCCons(new CCIS("HALT"), Nil);

            return Compile1(obj, Nil, cont);
        }

        private CCObject Compile1(CCObject exp, CCObject en, CCObject cont)
        {
            if(exp.GetType() != typeof(CCCons)) // nil, number or identifier
            {
                if (exp.GetType() == typeof(CCNil)) // nil
                {
                    return new CCCons(Nil, cont);
                }
                else
                {
                    var ij = Index(exp, en);
                    if (ij == Nil) // number
                    {
                        return new CCCons(new CCIS("LDC"), new CCCons(exp, cont));
                    }
                    else // identifier
                    {
                        return new CCCons(new CCIS("LD"), new CCCons(ij, cont));
                    }
                }
            }
            else // apply
            {
                var expc = exp as CCCons;
                var fcn = expc.car;
                var args = expc.cdr;
                if(fcn.GetType() != typeof(CCCons)) // apply function is a builtin, lambda or special form
                {
                    var fn = fcn as CCIdentifier;
                    var name = from x in Builtin where x == fn.Name select x;
                    if(name.Count() == 1)  // builtin
                    {
                        return CompileBuiltin(args, en, new CCCons(fcn, cont));
                    }
                    else if (fn.Name == "lambda") // lambda special form
                    {
                        var argsc = args as CCCons;
                        return CompileLambda(argsc.cadr, new CCCons(argsc.car, en), cont);
                    }
                    else if (fn.Name == "if") // if special form
                    {
                        var argsc = args as CCCons;
                        return CompileIf(argsc.car, argsc.cadr, argsc.caddr, en, cont);
                    }
                    else if(fn.Name == "let" || fn.Name == "letrec") // let or letrec
                    {
                        var argsc = args as CCCons;

                        var newn = new CCCons(argsc.car, en);
                        var values = argsc.cadr;
                        var body = argsc.caddr;

                        if(fn.Name == "let") // let
                        {
                            return new CCCons(Nil, CompileApp(values, en, CompileLambda(body, newn, new CCCons(new CCIS("AP"), cont))));
                        }
                        else // letrec
                        {
                            return new CCCons(new CCIS("DUM"), new CCCons(Nil, CompileApp(values, newn, CompileLambda(body, newn, new CCCons(new CCIS("RAP"), cont)))));
                        }
                    }
                    else if (fn.Name == "quote")    // quote
                    {
                        return new CCCons(new CCIS("LDC"), new CCCons((args as CCCons).car, cont));
                    }
                    else
                    {
                        return new CCCons(Nil, CompileApp(args, en, new CCCons(new CCIS("LD"), new CCCons(Index(fcn, en), new CCCons(new CCIS("AP"), cont)))));
                    }
                }
                else // application with nested function
                {
                    return new CCCons(Nil, CompileApp(args, en, Compile1(fcn, en, new CCCons(new CCIS("AP"), cont))));
                }
            }
        }

        private CCObject CompileBuiltin(CCObject args, CCObject en, CCObject cont)
        {
            if(args.GetType() == typeof(CCNil))
            {
                return cont;
            }
            else
            {
                return CompileBuiltin((args as CCCons).cdr, en, Compile1((args as CCCons).car, en, cont));
            }
        }

        private CCObject CompileIf(CCObject test, CCObject t, CCObject f, CCObject en, CCObject cont)
        {
            return Compile1(test, en, new CCCons(
                new CCIS("SEL"), new CCCons(
                    Compile1(t, en, new CCCons(new CCIS("JOIN"), Nil)), new CCCons(
                        Compile1(f, en, new CCCons(new CCIS("JOIN"), Nil)), cont))));
        }

        private CCObject CompileLambda(CCObject body, CCObject en, CCObject cont)
        {
            return new CCCons(
                new CCIS("LDF"), new CCCons(
                    Compile1(body, en, new CCCons(new CCIS("RTN"), Nil)),
                    cont));
        }

        private CCObject CompileApp(CCObject args, CCObject en, CCObject cont)
        {
            if(args.GetType() == typeof(CCNil))
            {
                return cont;
            }
            else
            {
                return CompileApp((args as CCCons).cdr, en, Compile1((args as CCCons).car, en, new CCCons(new CCIS("CONS"), cont)));
            }
        }

        private CCObject Index(CCObject exp, CCObject en)
        {
            return Index(exp, en, 1);
        }

        private CCObject Index(CCObject exp, CCObject en, int i)
        {
            if(en.GetType() == typeof(CCNil))
            {
                return Nil;
            }
            else
            {
                CCObject j = Index2(exp, (en as CCCons).car, 1);
                if(j.GetType() == typeof(CCNil))
                {
                    return Index(exp, (en as CCCons).cdr, i + 1);
                }
                else
                {
                    return new CCCons(new CCInt() { value = i }, j);
                }
            }
        }

        private CCObject Index2(CCObject exp, CCObject en, int j)
        {
            if (en.GetType() == typeof(CCNil))
            {
                return Nil;
            }
            else
            {
                var e = en as CCCons;
                if(e.car.ToString() == exp.ToString())
                {
                    return new CCInt()
                    {
                        value = j
                    };
                }
                else
                {
                    return Index2(exp, e.cdr, j + 1);
                }
            }
        }

        public void Eval(CCObject obj)
        {
            stack = Nil;
            env = Nil;
            code = obj;
            dump = Nil;
            while (EvalTop()) ;
        }

        private bool EvalTop()
        {
            var obj = Code;
            if (obj.GetType() == typeof(CCNil))
            {
                Stack = Nil;
                return true;
            }
            else if(obj.GetType() == typeof(CCIS))
            {
                var inst = obj as CCIS;
                switch(inst.Inst)
                {
                    case "LDC":
                        Stack = Code;
                        return true;

                    case "LD":
                        {
                            var pos = Code as CCCons;
                            int x = (pos.car as CCInt).value;
                            int y = (pos.cdr as CCInt).value;

                            Stack = GetEnvIndex(x, y);
                        }
                        return true;

                    case "CONS":
                        {
                            var car = Stack;
                            var cdr = Stack;
                            Stack = new CCCons(car, cdr);
                        }
                        return true;

                    case "SEL":
                        {
                            var ct = Code;
                            var cf = Code;
                            var tf = Stack;
                            Dump = code;
                            if (tf.GetType() != typeof(CCNil))
                            {
                                Code = ct;
                            }
                            else
                            {
                                Code = cf;
                            }
                        }
                        return true;

                    case "JOIN":
                        code = Dump;
                        return true;

                    case "LDF":
                        {
                            var fn = Code;
                            Stack = new CCCons(fn, env);
                        }
                        return true;

                    case "AP":
                        {
                            // get closure and arguments
                            var fe = Stack as CCCons;
                            var v = Stack;

                            // dump
                            Dump = code;
                            Dump = env;
                            Dump = stack;

                            // apply
                            stack = Nil;
                            env = fe.cdr;
                            Env = v;
                            code = fe.car;
                        }
                        return true;

                    case "RTN":
                        {
                            var ret = Stack;
                            stack = Dump;
                            env = Dump;
                            code = Dump;

                            Stack = ret;
                        }
                        return true;

                    case "DUM":
                        Env = Nil;
                        return true;

                    case "RAP":
                        {
                            // get closure and arguments
                            var fe = Stack as CCCons;
                            var v = Stack;

                            var cl = fe.cdr as CCCons;
                            cl.car = v;
                            var dummy = Env;

                            // dump
                            Dump = code;
                            Dump = env;
                            Dump = stack;

                            // apply
                            stack = Nil;
                            env = fe.cdr;
                            Env = v;
                            code = fe.car;
                        }
                        return true;

                    case "HALT":
                        return false;

                    default:
                        throw new NotImplementedException();
                }
            }
            else
            {
                var inst = obj as CCIdentifier;
                switch(inst.Name)
                {
                    case "+":
                        {
                            var s1 = Stack as CCInt;
                            var s2 = Stack as CCInt;
                            var ret = new CCInt();
                            ret.value = s1.value + s2.value;
                            Stack = ret;
                        }
                        return true;

                    case "-":
                        {
                            var s1 = Stack as CCInt;
                            var s2 = Stack as CCInt;
                            var ret = new CCInt();
                            ret.value = s1.value - s2.value;
                            Stack = ret;
                        }
                        return true;

                    case "*":
                        {
                            var s1 = Stack as CCInt;
                            var s2 = Stack as CCInt;
                            var ret = new CCInt();
                            ret.value = s1.value * s2.value;
                            Stack = ret;
                        }
                        return true;

                    case "/":
                        {
                            var s1 = Stack as CCInt;
                            var s2 = Stack as CCInt;
                            var ret = new CCInt();
                            ret.value = s1.value / s2.value;
                            Stack = ret;
                        }
                        return true;

                    case "cons":
                        {
                            var car = Stack;
                            var cdr = Stack;
                            Stack = new CCCons(car, cdr);
                        }
                        return true;

                    case "car":
                        {
                            var cons = Stack as CCCons;
                            Stack = cons.car;
                        }
                        return true;

                    case "cdr":
                        {
                            var cons = Stack as CCCons;
                            Stack = cons.cdr;
                        }
                        return true;

                    default:
                        throw new NotImplementedException();
                }
            }

        }

        private CCObject GetEnvIndex(int x, int y)
        {
            return GetEnvIndex1(x, y, env);
        }

        private CCObject GetEnvIndex1(int x, int y, CCObject env)
        {
            if (x > 1)
            {
                return GetEnvIndex1(x - 1, y, (env as CCCons).cdr);
            }
            else
            {
                return GetEnvIndex2(y, (env as CCCons).car);
            }
        }

        private CCObject GetEnvIndex2(int y, CCObject env)
        {
            if(y > 1)
            {
                return GetEnvIndex2(y - 1, (env as CCCons).cdr);
            }
            else
            {
                return (env as CCCons).car;
            }
        }

        public CCObject GetResult()
        {
            return Stack;
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

            var list = new CCCons(Nil, Nil);
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
