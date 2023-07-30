using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IntelOrca.Biohazard.Model;

namespace IntelOrca.Biohazard.Script.Compilation
{
    public partial class ScdCompiler : IScdGenerator
    {
        public ErrorList Errors { get; } = new ErrorList();
        public IRdtEditOperation[] Operations { get; private set; } = new IRdtEditOperation[0];

        public int Generate(IFileIncluder includer, string path)
        {
            var lexer = new Lexer(includer, Errors);
            var tokens = lexer.GetTokens(path).ToArray();
            if (Errors.Count != 0)
                return 1;

            var parser = new Parser(Errors);
            var syntaxTree = parser.BuildSyntaxTree(tokens);

            var generator = new Generator(Errors, path);
            var result = generator.Generate(syntaxTree);
            if (result != 0)
                return result;

            Operations = generator.Operations;
            return 0;
        }
    }
}
