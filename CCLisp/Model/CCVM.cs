using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace CCLisp.Model
{
    public class CCStack
    {
        public CCCons Stack { get; set; }
        public CCObject Top
        {
            get
            {
                return Stack.car;
            }
        }

        public CCStack()
        {
            Stack = null;
        }

        public void Push(CCObject val)
        {
            Stack = new CCCons(val, Stack);
        }

        public CCObject Pop()
        {
            var ret = Top;
            Stack = Stack.cdr as CCCons;
            return ret;
        }

        public void Clear()
        {
            Stack = null;
        }
    }

    public class CCVM
    {
        public static string[] Builtin = {"+", "-", "*", "/", "car", "cdr", "cons", "eq", "<", ">", "<=", ">="};

        // evaluation environments
        private CCStack Stack;
        private CCStack Env;
        private CCStack Code;
        private CCStack Dump;

        public CCVM()
        {
            // clear environment
            Stack = new CCStack();
            Env = new CCStack();
            Code = new CCStack();
            Dump = new CCStack();

            // make function and macro environments
            Env.Push(new CCCons(null, null));   // global macro
            Env.Push(new CCCons(null, null));   // global function or value
        }



        public void Eval(CCObject obj)
        {
            Stack.Clear();
            Code.Stack = obj as CCCons;
            Dump.Clear();
            while (EvalTop()) ;
        }


        public CCObject GetResult()
        {
            return Stack.Pop();
        }

        public void SaveCore(string filename)
        {
            IFormatter formatter = new BinaryFormatter();

            Stream stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None);
            formatter.Serialize(stream, Env.Stack);
            stream.Close();
        }

        public void LoadCore(string filename)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            Env.Stack = (CCCons)formatter.Deserialize(stream);
            stream.Close();
        }

        // private functions
        private bool EvalTop()
        {
            var obj = Code.Pop();
            if (obj == null)
            {
                Stack.Push(null);
                return true;
            }
            else if(obj.GetType() == typeof(CCIS))
            {
                var inst = obj as CCIS;
                switch(inst.Inst)
                {
                    case "LDC":
                        Stack.Push(Code.Pop());
                        return true;

                    case "LD":
                        {
                            var pos = Code.Pop() as CCCons;
                            int x = (pos.car as CCInt).value;
                            int y = (pos.cdr as CCInt).value;

                            Stack.Push(GetEnvIndex(x, y));
                        }
                        return true;

                    case "ST":
                        {
                            var pos = Code.Pop() as CCCons;
                            int x = (pos.car as CCInt).value;
                            int y = (pos.cdr as CCInt).value;

                            SetEnvIndex(x, y, Stack.Top);
                        }
                        return true;

                    case "CONS":
                        {
                            var car = Stack.Pop();
                            var cdr = Stack.Pop();
                            Stack.Push(new CCCons(car, cdr));
                        }
                        return true;

                    case "SEL":
                        {
                            var ct = Code.Pop();
                            var cf = Code.Pop();
                            var tf = Stack.Pop();
                            Dump.Push(Code.Stack);
                            if (tf != null)
                            {
                                Code.Stack = ct as CCCons;
                            }
                            else
                            {
                                Code.Stack = cf as CCCons;
                            }
                        }
                        return true;

                    case "JOIN":
                        Code.Stack = Dump.Pop() as CCCons;
                        return true;

                    case "LDF":
                        {
                            var fn = Code.Pop();
                            Stack.Push(new CCCons(fn, Env.Stack));
                        }
                        return true;

                    case "AP":
                        {
                            // get closure and arguments
                            var fe = Stack.Pop() as CCCons;
                            var v = Stack.Pop();

                            // dump
                            Dump.Push(Code.Stack);
                            Dump.Push(Env.Stack);
                            Dump.Push(Stack.Stack);

                            // apply
                            Stack.Clear();
                            Env.Stack = fe.cdr as CCCons;
                            Env.Push(v);
                            Code.Stack = fe.car as CCCons;
                        }
                        return true;

                    case "RTN":
                        {
                            var ret = Stack.Pop();
                            Stack.Stack = Dump.Pop() as CCCons;
                            Env.Stack = Dump.Pop() as CCCons;
                            Code.Stack = Dump.Pop() as CCCons;

                            Stack.Push(ret);
                        }
                        return true;

                    case "DUM":
                        Env.Clear();
                        return true;

                    case "RAP":
                        {
                            // get closure and arguments
                            var fe = Stack.Pop() as CCCons;
                            var v = Stack.Pop();

                            var cl = fe.cdr as CCCons;
                            cl.car = v;
                            var dummy = Env;

                            // dump
                            Dump.Push(Code.Stack);
                            Dump.Push(Env.Stack);
                            Dump.Push(Stack.Stack);

                            // apply
                            Stack.Clear();
                            Env.Stack = fe.cdr as CCCons;
                            Env.Push(v);
                            Code.Stack = fe.car as CCCons;
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
                            var s1 = Stack.Pop() as CCInt;
                            var s2 = Stack.Pop() as CCInt;
                            var ret = new CCInt();
                            ret.value = s1.value + s2.value;
                            Stack.Push(ret);
                        }
                        return true;

                    case "-":
                        {
                            var s1 = Stack.Pop() as CCInt;
                            var s2 = Stack.Pop() as CCInt;
                            var ret = new CCInt();
                            ret.value = s1.value - s2.value;
                            Stack.Push(ret);
                        }
                        return true;

                    case "*":
                        {
                            var s1 = Stack.Pop() as CCInt;
                            var s2 = Stack.Pop() as CCInt;
                            var ret = new CCInt();
                            ret.value = s1.value * s2.value;
                            Stack.Push(ret);
                        }
                        return true;

                    case "/":
                        {
                            var s1 = Stack.Pop() as CCInt;
                            var s2 = Stack.Pop() as CCInt;
                            var ret = new CCInt();
                            ret.value = s1.value / s2.value;
                            Stack.Push(ret);
                        }
                        return true;

                    case "cons":
                        {
                            var car = Stack.Pop();
                            var cdr = Stack.Pop();
                            Stack.Push(new CCCons(car, cdr));
                        }
                        return true;

                    case "car":
                        {
                            var cons = Stack.Pop() as CCCons;
                            Stack.Push(cons.car);
                        }
                        return true;

                    case "cdr":
                        {
                            var cons = Stack.Pop() as CCCons;
                            Stack.Push(cons.cdr);
                        }
                        return true;

                    case "eq":
                        {
                            var s1 = Stack.Pop();
                            var s2 = Stack.Pop();
                            if (s1.Equals(s2))
                            {
                                Stack.Push(new CCT());
                            }
                            else
                            {
                                Stack.Push(null);
                            }
                        }
                        return true;

                    case "<":
                        {
                            var s1 = Stack.Pop() as CCInt;
                            var s2 = Stack.Pop() as CCInt;
                            if (s1.value < s2.value)
                            {
                                Stack.Push(new CCT());
                            }
                            else
                            {
                                Stack.Push(null);
                            }
                        }
                        return true;

                    case ">":
                        {
                            var s1 = Stack.Pop() as CCInt;
                            var s2 = Stack.Pop() as CCInt;
                            if (s1.value > s2.value)
                            {
                                Stack.Push(new CCT());
                            }
                            else
                            {
                                Stack.Push(null);
                            }
                        }
                        return true;

                    case "<=":
                        {
                            var s1 = Stack.Pop() as CCInt;
                            var s2 = Stack.Pop() as CCInt;
                            if (s1.value <= s2.value)
                            {
                                Stack.Push(new CCT());
                            }
                            else
                            {
                                Stack.Push(null);
                            }
                        }
                        return true;

                    case ">=":
                        {
                            var s1 = Stack.Pop() as CCInt;
                            var s2 = Stack.Pop() as CCInt;
                            if (s1.value >= s2.value)
                            {
                                Stack.Push(new CCT());
                            }
                            else
                            {
                                Stack.Push(null);
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
            return GetEnvIndex1(x, y, Env.Stack);
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
            SetEnvIndex1(x, y, Env.Stack, v);
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
                    env.cdr = cdr;
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
