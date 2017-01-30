﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Pastel.Nodes
{
    class FunctionInvocation : Expression
    {
        public Expression Root { get; set; }
        public Token OpenParenToken { get; set; }
        public Expression[] Args { get; set; }

        public FunctionInvocation(
            Expression root,
            Token openParen,
            IList<Expression> args) : base(root.FirstToken)
        {
            this.Root = root;
            this.OpenParenToken = openParen;
            this.Args = args.ToArray();
        }

        public Expression MaybeImmediatelyResolve(PastelParser parser)
        {
            if (this.Root is CompileTimeFunctionReference)
            {
                CompileTimeFunctionReference constFunc = (CompileTimeFunctionReference)this.Root;
                if (constFunc.NameToken.Value == "ext_constant")
                {
                    InlineConstant argName = (InlineConstant)this.Args[0];
                    bool boolValue = parser.GetParseTimeConstant(argName.Value.ToString());
                    return new InlineConstant(PType.BOOL, this.FirstToken, boolValue);
                }
            }
            return this;
        }


        public override Expression NameResolution(Dictionary<string, FunctionDefinition> functionLookup, Dictionary<string, StructDefinition> structLookup)
        {
            throw new NotImplementedException();
        }

        public override void ResolveTypes()
        {
            throw new NotImplementedException();
        }
    }
}