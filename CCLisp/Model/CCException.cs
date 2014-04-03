using System;

namespace CCLisp.Model
{
    public class CCException : Exception
    {
    }

    public class CCRuntimeException : CCException
    {
        public CCObject Stack { get; private set; }
        public CCObject Env { get; private set; }
        public CCObject Code { get; private set; }
        public CCObject Dump { get; private set; }

        public CCRuntimeException(CCObject stack, CCObject env, CCObject code, CCObject dump)
        {
            Stack = stack;
            Env = env;
            Code = code;
            Dump = dump;
        }
    }

    public class CCCompileIdentifierNotFoundException : CCException
    {
    }

    public class CCCompileInvalidFormalParameterException : CCException
    {
    }

    public class CCParserException : CCException
    {
    }
}
