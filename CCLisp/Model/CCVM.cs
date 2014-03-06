using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CCLisp.Model
{
    public class CCVM
    {
        private string[] Builtin = {"+", "-", "*", "/", "car", "cdr", "cons", "eq", "<", ">", "<=", ">="};

        // evaluation environments
        private CCObject stack;
        private CCObject Stack
        {
            get
            {
                if (stack == null)
                {
                    return null;
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

        private CCObject _env;
        private CCObject Env
        {
            get
            {
                if (_env == null)
                {
                    return null;
                }
                else
                {
                    var top = _env as CCCons;
                    _env = top.cdr;
                    return top.car;
                }
            }

            set
            {
                var top = new CCCons(value, _env);
                _env = top;
            }
        }

        private CCObject code;
        private CCObject Code
        {
            get
            {
                if (code == null)
                {
                    return null;
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
                    code = null;
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
                if (dump == null)
                {
                    return null;
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
            stack = null;
            _env = null;
            code = null;
            dump = null;

            // make special symbol t

            _env = new CCCons(new CCCons(null, null), null);
        }



        public void Eval(CCObject obj)
        {
            stack = null;
            code = obj;
            dump = null;
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
            if (obj == null)
            {
                Stack = null;
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

                    case "ST":
                        {
                            var pos = Code as CCCons;
                            int x = (pos.car as CCInt).value;
                            int y = (pos.cdr as CCInt).value;

                            var set = Stack;
                            SetEnvIndex(x, y, set);
                            Stack = set;
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
                            if (tf != null)
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
                            Stack = new CCCons(fn, _env);
                        }
                        return true;

                    case "AP":
                        {
                            // get closure and arguments
                            var fe = Stack as CCCons;
                            var v = Stack;

                            // dump
                            Dump = code;
                            Dump = _env;
                            Dump = stack;

                            // apply
                            stack = null;
                            _env = fe.cdr;
                            Env = v;
                            code = fe.car;
                        }
                        return true;

                    case "RTN":
                        {
                            var ret = Stack;
                            stack = Dump;
                            _env = Dump;
                            code = Dump;

                            Stack = ret;
                        }
                        return true;

                    case "DUM":
                        Env = null;
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
                            Dump = _env;
                            Dump = stack;

                            // apply
                            stack = null;
                            _env = fe.cdr;
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
                                Stack = new CCT();
                            }
                            else
                            {
                                Stack = null;
                            }
                        }
                        return true;

                    case "<":
                        {
                            var s1 = Stack as CCInt;
                            var s2 = Stack as CCInt;
                            if (s1.value < s2.value)
                            {
                                Stack = new CCT();
                            }
                            else
                            {
                                Stack = null;
                            }
                        }
                        return true;

                    case ">":
                        {
                            var s1 = Stack as CCInt;
                            var s2 = Stack as CCInt;
                            if (s1.value > s2.value)
                            {
                                Stack = new CCT();
                            }
                            else
                            {
                                Stack = null;
                            }
                        }
                        return true;

                    case "<=":
                        {
                            var s1 = Stack as CCInt;
                            var s2 = Stack as CCInt;
                            if (s1.value <= s2.value)
                            {
                                Stack = new CCT();
                            }
                            else
                            {
                                Stack = null;
                            }
                        }
                        return true;

                    case ">=":
                        {
                            var s1 = Stack as CCInt;
                            var s2 = Stack as CCInt;
                            if (s1.value >= s2.value)
                            {
                                Stack = new CCT();
                            }
                            else
                            {
                                Stack = null;
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
            return GetEnvIndex1(x, y, _env);
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

        private void SetEnvIndex(int x, int y, CCObject v)
        {
            var env = _env as CCCons;
            SetEnvIndex1(x, y, env, v);
        }

        private void SetEnvIndex1(int x, int y, CCCons env, CCObject v)
        {
            if (x > 1)
            {
                var cdr = env.cdr as CCCons;
                SetEnvIndex1(x - 1, y, cdr, v);
            }
            else
            {
                var car = env.car as CCCons;
                SetEnvIndex2(y, car, v);
            }
        }

        private void SetEnvIndex2(int y, CCCons env, CCObject v)
        {
            if (y > 1)
            {
                var cdr = env.cdr as CCCons;
                if (cdr == null)
                {
                    cdr = new CCCons(null, null);
                }
                SetEnvIndex2(y - 1, cdr, v);
            }
            else
            {
                env.car = v;
            }
        }
    }
}
