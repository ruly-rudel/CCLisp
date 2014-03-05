using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCLisp.Model
{
    class CCCompiler
    {
        private CCNil Nil = new CCNil();
        private string[] Builtin = { "+", "-", "*", "/", "car", "cdr", "cons", "eq", "<", ">", "<=", ">=" };


        public CCObject Compile(CCObject obj)
        {
            var cont = new CCCons(new CCIS("HALT"), Nil);

            return Compile1(obj, Nil, cont);
        }

        private CCObject Compile1(CCObject exp, CCObject en, CCObject cont)
        {
            if (exp.GetType() != typeof(CCCons)) // nil, number or identifier
            {
                if (exp.GetType() == typeof(CCNil)) // nil
                {
                    return new CCCons(Nil, cont);
                }
                else
                {
                    if (exp.GetType() == typeof(CCInt))
                    {
                        return new CCCons(new CCIS("LDC"), new CCCons(exp, cont));
                    }
                    else // identifier
                    {
                        var ij = Index(exp as CCIdentifier, en);
                        return new CCCons(new CCIS("LD"), new CCCons(ij, cont));
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
                    var name = from x in Builtin where x == fn.Name select x;
                    if (name.Count() == 1)  // builtin
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
                    else if (fn.Name == "let" || fn.Name == "letrec") // let or letrec
                    {
                        var argsc = args as CCCons;

                        var newn = new CCCons(argsc.car, en);
                        var values = argsc.cadr;
                        var body = argsc.caddr;

                        if (fn.Name == "let") // let
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
                        return new CCCons(Nil, CompileApp(args, en, new CCCons(new CCIS("LD"), new CCCons(Index(fcn as CCIdentifier, en), new CCCons(new CCIS("AP"), cont)))));
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
            if (args.GetType() == typeof(CCNil))
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
            if (args.GetType() == typeof(CCNil))
            {
                return cont;
            }
            else
            {
                return CompileApp((args as CCCons).cdr, en, Compile1((args as CCCons).car, en, new CCCons(new CCIS("CONS"), cont)));
            }
        }

        private CCCons Index(CCIdentifier exp, CCObject en)
        {
            return Index(exp, en, 1);
        }

        private CCCons Index(CCIdentifier exp, CCObject en, int i)
        {
            if (en.GetType() == typeof(CCNil))
            {
                return null;
            }
            else
            {
                var j = Index2(exp, (en as CCCons).car, 1);
                if (j < 0)
                {
                    return Index(exp, (en as CCCons).cdr, i + 1);
                }
                else
                {
                    return new CCCons(new CCInt() { value = i }, new CCInt() { value = j });
                }
            }
        }

        private int Index2(CCIdentifier exp, CCObject en, int j)
        {
            if (en.GetType() == typeof(CCNil))
            {
                return -1;
            }
            else
            {
                var e = en as CCCons;
                if (e.car.ToString() == exp.ToString())
                {
                    return j;
                }
                else
                {
                    return Index2(exp, e.cdr, j + 1);
                }
            }
        }


    }
}
