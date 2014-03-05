using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCLisp.Model
{
    class CCCompiler
    {
        private string[] Builtin = { "+", "-", "*", "/", "car", "cdr", "cons", "eq", "<", ">", "<=", ">=" };

        private CCCons symbols;

        public CCCompiler()
        {
            symbols = new CCCons(new CCCons(new CCIdentifier() { Name = "t" }, null), null);
        }


        public CCObject Compile(CCObject obj)
        {
            var cont = new CCCons(new CCIS("HALT"), null);

            return Compile1(obj, new CCCons(symbols, null), cont);
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
                        return new CCCons(new CCIS("LD"), new CCCons(ij, cont));
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
                    var name = from x in Builtin where x == fn.Name select x;
                    if (name.Count() == 1)  // builtin
                    {
                        return CompileBuiltin(args, env, new CCCons(fcn, cont));
                    }
                    else if (fn.Name == "lambda") // lambda special form
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
                            return new CCCons(null, CompileApp(values, env, CompileLambda(body, newn, new CCCons(new CCIS("AP"), cont))));
                        }
                        else // letrec
                        {
                            return new CCCons(new CCIS("DUM"), new CCCons(null, CompileApp(values, newn, CompileLambda(body, newn, new CCCons(new CCIS("RAP"), cont)))));
                        }
                    }
                    else if (fn.Name == "quote")    // quote
                    {
                        return new CCCons(new CCIS("LDC"), new CCCons((args as CCCons).car, cont));
                    }
                    else if(fn.Name == "setf")    // setf
                    {
                        var argsc = args as CCCons;

                        var symbol = argsc.car as CCIdentifier;
                        var value = argsc.cadr;

                        var pos = Index(symbol, env);
                        if (pos == null)
                        {
                            // create new symbol(root environment)
                            CCCons i = symbols;
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

                        return new CCCons(new CCIS("ST"), new CCCons(pos, Compile1(value, env, cont)));
                    }
                    else
                    {
                        return new CCCons(null, CompileApp(args, env, new CCCons(new CCIS("LD"), new CCCons(Index(fcn as CCIdentifier, env), new CCCons(new CCIS("AP"), cont)))));
                    }
                }
                else // application with nested function
                {
                    return new CCCons(null, CompileApp(args, env, Compile1(fcn, env, new CCCons(new CCIS("AP"), cont))));
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

        private CCObject CompileApp(CCObject args, CCCons env, CCObject cont)
        {
            if (args == null)
            {
                return cont;
            }
            else
            {
                return CompileApp((args as CCCons).cdr, env, Compile1((args as CCCons).car, env, new CCCons(new CCIS("CONS"), cont)));
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
                if (j < 0)
                {
                    return Index(exp, env.cdr as CCCons, i + 1);
                }
                else
                {
                    return new CCCons(new CCInt() { value = i }, new CCInt() { value = j });
                }
            }
        }

        private int Index2(CCIdentifier exp, CCCons env, int j)
        {
            if (env == null || env.car == null)
            {
                return -1;
            }
            else
            {
                if (env.car.ToString() == exp.ToString())
                {
                    return j;
                }
                else
                {
                    return Index2(exp, env.cdr as CCCons, j + 1);
                }
            }
        }


    }
}
