using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace CCLisp.Model
{
    class CCCompiler
    {
        private CCCons fn_symbols;
        private CCCons mc_symbols;
        private CCCons root_env;
        private CCVM vm;

        public CCCompiler(CCVM v)
        {
            fn_symbols = new CCCons(null, null);
            mc_symbols = new CCCons(null, null);
            root_env = new CCCons(fn_symbols, new CCCons(mc_symbols, null));
            vm = v;
        }

        public void SaveSymbol(string filename)
        {
            IFormatter formatter = new BinaryFormatter();

            Stream stream = new FileStream(filename + ".s", FileMode.Create, FileAccess.Write, FileShare.None);
            formatter.Serialize(stream, root_env);
            stream.Close();
        }

        public void LoadSymbol(string filename)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(filename + ".s", FileMode.Open, FileAccess.Read, FileShare.Read);
            root_env = (CCCons)formatter.Deserialize(stream);
            fn_symbols = root_env.car as CCCons;
            mc_symbols = root_env.cadr as CCCons;

            stream.Close();
        }


        public CCObject Compile(CCObject obj)
        {
            var cont = new CCCons(new CCIS("HALT"), null);

            return Compile1(obj, root_env, cont);
        }

        private CCObject Compile1(CCObject exp, CCCons env, CCObject cont)
        {
            if (exp == null)    // nil
            {
                return new CCCons(null, cont);
            }
            else if (exp.GetType() != typeof(CCCons)) // number or identifier
            {
                if (exp.GetType() == typeof(CCInt))
                {
                    return new CCCons(new CCIS("LDC"), new CCCons(exp, cont));
                }
                else // identifier
                {
                    var ij = Index(exp as CCIdentifier, env);
                    if (ij == null)
                    {
                        throw new CCCompileIdentifierNotFoundException(exp.ToString());
                    }
                    else
                    {
                        if (ij.cdr.GetType() == typeof(CCCons)) // parameter with additional information
                        {
                            if (ij.caddr.ToString() == "&rest")  // rest parameter
                            {
                                ij.cdr = ij.cadr;  // variable position
                                return new CCCons(new CCIS("LDR"), new CCCons(ij, cont));
                            }
                            else
                            {
                                throw new CCCompileInvalidFormalParameterException(ij.caddr.ToString());
                            }
                        }
                        else
                        {
                            return new CCCons(new CCIS("LD"), new CCCons(ij, cont));
                        }
                    }
                }
            }
            else // apply
            {
                var expc = exp as CCCons;
                var fcn = expc.car;
                var args = expc.cdr;
                if (fcn.GetType() != typeof(CCCons)) // apply function is a builtin, lambda or special form
                {
                    var fn = fcn as CCIdentifier;
                    var name = from x in CCVM.Builtin where x == fn.Name select x;
                    if (name.Count() == 1)  // builtin
                    {
                        if (name.First() == "list")
                        {
                            return new CCCons(null, CompileList(args, env, cont));
                        }
                        else
                        {
                            return CompileBuiltin(args, env, new CCCons(fcn, cont));
                        }
                    }
                    else if (fn.Name == "fn") // lambda special form
                    {
                        var argsc = args as CCCons;
                        return CompileLambda(argsc.cadr, new CCCons(argsc.car, env), cont);
                    }
                    else if (fn.Name == "if") // if special form
                    {
                        var argsc = args as CCCons;
                        return CompileIf(argsc.car, argsc.cadr, argsc.caddr, env, cont);
                    }
                    else if (fn.Name == "let" || fn.Name == "letrec") // let or letrec
                    {
                        var argsc = args as CCCons;

                        var newn = new CCCons(argsc.car, env);
                        var values = argsc.cadr;
                        var body = argsc.caddr;

                        if (fn.Name == "let") // let
                        {
                            return new CCCons(null, CompileApp(values as CCCons, env, CompileLambda(body, newn, new CCCons(new CCIS("AP"), cont))));
                        }
                        else // letrec
                        {
                            return new CCCons(new CCIS("DUM"), new CCCons(null, CompileApp(values as CCCons, newn, CompileLambda(body, newn, new CCCons(new CCIS("RAP"), cont)))));
                        }
                    }
                    else if (fn.Name == "quote")    // quote
                    {
                        return new CCCons(new CCIS("LDC"), new CCCons((args as CCCons).car, cont));
                    }
                    else if(fn.Name == "set")    // setf
                    {
                        var argsc = args as CCCons;

                        var symbol = argsc.car as CCIdentifier;
                        var value = argsc.cadr;

                        var pos = Index(symbol, env);
                        if (pos == null)
                        {
                            // create new symbol(root environment)
                            CCCons i = fn_symbols;
                            while (i.cdr != null)
                                i = i.cdr as CCCons;

                            if (i.car == null)
                            {
                                i.car = symbol;
                            }
                            else
                            {
                                var cons = new CCCons(symbol, null);
                                i.cdr = cons;
                            }

                            // re-find index
                            pos = Index(symbol, env);
                        }

                        return Compile1(value, env, new CCCons(new CCIS("ST"), new CCCons(pos, cont)));
                    }
                    else if(fn.Name == "defm")  // defmacro
                    {
                        var argsc = args as CCCons;

                        var symbol = argsc.car as CCIdentifier;
                        var margs = argsc.cadr;
                        var mbody = argsc.caddr;

                        var pos = Index(symbol, env);
                        if (pos == null)
                        {
                            // create new symbol(root environment)
                            CCCons i = mc_symbols;
                            while (i.cdr != null)
                                i = i.cdr as CCCons;

                            if (i.car == null)
                            {
                                i.car = symbol;
                            }
                            else
                            {
                                var cons = new CCCons(symbol, null);
                                i.cdr = cons;
                            }

                            // re-find index
                            pos = Index(symbol, env);
                        }

                        return CompileLambda(mbody, new CCCons(margs, env), new CCCons(new CCIS("ST"), new CCCons(pos, cont)));
                    }
                    else // application or macro
                    {
                        // check if it is macro or not
                        if (Index2(fcn as CCIdentifier, mc_symbols, 1) != null)
                        {   // macro expansion compile
                            var expand_code = new CCCons(new CCIS("LDC"), new CCCons(args, new CCCons(new CCIS("LD"), new CCCons(Index(fcn as CCIdentifier, env), new CCCons(new CCIS("AP"), new CCCons(new CCIS("HALT"), null))))));
                            vm.Eval(expand_code);
                            var r = vm.GetResult();
                            return Compile1(r, env, cont);
                        }
                        else // normal application
                        {
                            var func = Index(fcn as CCIdentifier, env);
                            if(func == null)
                            {
                                throw new CCCompileIdentifierNotFoundException(fcn.ToString());
                            }
                            else
                            {
                                return new CCCons(null, CompileApp(args as CCCons, env, new CCCons(new CCIS("LD"), new CCCons(func, new CCCons(new CCIS("AP"), cont)))));
                            }
                        }
                    }
                }
                else // application with nested function
                {
                    return new CCCons(null, CompileApp(args as CCCons, env, Compile1(fcn, env, new CCCons(new CCIS("AP"), cont))));
                }
            }
        }

        private CCObject CompileBuiltin(CCObject args, CCCons env, CCObject cont)
        {
            if (args == null)
            {
                return cont;
            }
            else
            {
                return CompileBuiltin((args as CCCons).cdr, env, Compile1((args as CCCons).car, env, cont));
            }
        }

        private CCObject CompileList(CCObject args, CCCons env, CCObject cont)
        {
            if (args == null)
            {
                return cont;
            }
            else
            {
                return CompileList((args as CCCons).cdr, env, Compile1((args as CCCons).car, env, new CCCons(new CCIS("CONS"), cont)));
            }
        }

        private CCObject CompileIf(CCObject test, CCObject t, CCObject f, CCCons env, CCObject cont)
        {
            return Compile1(test, env, new CCCons(
                new CCIS("SEL"), new CCCons(
                    Compile1(t, env, new CCCons(new CCIS("JOIN"), null)), new CCCons(
                        Compile1(f, env, new CCCons(new CCIS("JOIN"), null)), cont))));
        }

        private CCObject CompileLambda(CCObject body, CCCons env, CCObject cont)
        {
            return new CCCons(
                new CCIS("LDF"), new CCCons(
                    Compile1(body, env, new CCCons(new CCIS("RTN"), null)),
                    cont));
        }

        private CCObject CompileApp(CCCons args, CCCons env, CCObject cont)
        {
            if (args == null)
            {
                return cont;
            }
            else
            {
                return CompileApp(args.cdr as CCCons, env, Compile1((args as CCCons).car, env, new CCCons(new CCIS("CONS"), cont)));
            }
        }

        private CCCons Index(CCIdentifier exp, CCCons env)
        {
            return Index(exp, env, 1);
        }

        private CCCons Index(CCIdentifier exp, CCCons env, int i)
        {
            if (env == null)
            {
                return null;
            }
            else
            {
                var j = Index2(exp, env.car as CCCons, 1);
                if (j == null)
                {
                    return Index(exp, env.cdr as CCCons, i + 1);
                }
                else
                {
                    return new CCCons(new CCInt() { value = i }, j);
                }
            }
        }

        private CCObject Index2(CCIdentifier exp, CCCons env, int j)
        {
            if (env == null || env.car == null)
            {
                return null;
            }
            else
            {
                if(env.car.GetType() == typeof(CCCons)) // function or macro
                {
                    if (env.caar.ToString() == exp.ToString())
                    {
                        return new CCCons(new CCInt() { value = j }, env.cdar);
                    }
                    else
                    {
                        return Index2(exp, env.cdr as CCCons, j + 1);
                    }
                }
                else // atom
                {
                    if (env.car.ToString() == exp.ToString())
                    {
                        return new CCInt() { value = j };
                    }
                    else
                    {
                        return Index2(exp, env.cdr as CCCons, j + 1);
                    }
                }
            }
        }


    }
}
