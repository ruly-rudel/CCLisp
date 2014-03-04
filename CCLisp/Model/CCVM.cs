using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CCLisp.Model
{
    public class CCVM
    {
        // single oblects (must make it singleton?
        private CCNil    Nil    = new CCNil();

        private string[] Builtin = {"+", "-", "*", "/", "car", "cdr", "cons", "eq", "<", ">", "<=", ">="};

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

        public CCVM()
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



        public void Eval(CCObject obj)
        {
            stack = Nil;
            env = Nil;
            code = obj;
            dump = Nil;
            while (EvalTop()) ;
        }


        public CCObject GetResult()
        {
            return Stack;
        }


        // private functions
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
                                code = ct;
                            }
                            else
                            {
                                code = cf;
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

                    case "eq":
                        {
                            var s1 = Stack;
                            var s2 = Stack;
                            if (s1.Equals(s2))
                            {
                                Stack = Symbols["t"];
                            }
                            else
                            {
                                Stack = Nil;
                            }
                        }
                        return true;

                    case "<":
                        {
                            var s1 = Stack as CCInt;
                            var s2 = Stack as CCInt;
                            if (s1.value < s2.value)
                            {
                                Stack = Symbols["t"];
                            }
                            else
                            {
                                Stack = Nil;
                            }
                        }
                        return true;

                    case ">":
                        {
                            var s1 = Stack as CCInt;
                            var s2 = Stack as CCInt;
                            if (s1.value > s2.value)
                            {
                                Stack = Symbols["t"];
                            }
                            else
                            {
                                Stack = Nil;
                            }
                        }
                        return true;

                    case "<=":
                        {
                            var s1 = Stack as CCInt;
                            var s2 = Stack as CCInt;
                            if (s1.value <= s2.value)
                            {
                                Stack = Symbols["t"];
                            }
                            else
                            {
                                Stack = Nil;
                            }
                        }
                        return true;

                    case ">=":
                        {
                            var s1 = Stack as CCInt;
                            var s2 = Stack as CCInt;
                            if (s1.value >= s2.value)
                            {
                                Stack = Symbols["t"];
                            }
                            else
                            {
                                Stack = Nil;
                            }
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
    }
}
