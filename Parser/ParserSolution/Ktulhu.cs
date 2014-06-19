using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ParserNamespace
{
    class ProgramParse
    {
        Source sourceCode;
        Context context;
        public StatementSequence statements;
        public string output;

        public ProgramParse()
        {
            context = new Context(null);
            statements = new StatementSequence();
            InitPrint();
        }

        public void Parse(string str)
        {
            sourceCode = new Source(str);
            statements = StatementSequence.TryParse(ref sourceCode);
        }

        public void Run()
        {
            statements.Execute(context);
            /*foreach (Variable var in context.variables)
            {
                output += var.name + ": "+var.Print()+Environment.NewLine;
            }*/
        }

        private void InitPrint()
        {
            FuncValue print = new FuncValue();
            print.body = new StatementSequence();
            var temp = this;
            print.body.AddStatement(new PrintStatement(ref temp));
            var printVar = context.FindFirstVar("print");
            printVar.Value = print;
        }
    }

    class Source
    {
        public string text;
        public int currentPos;
        public int completedPos;

        public string GG { get { return text.Substring(currentPos); } }

        public override string ToString()
        {
            return GG;
        }

        public Source(string s)
        {
            text = s;
            currentPos = 0;
            completedPos = 0;
        }

        public char CurrSymbol 
        {
            get
            {
                if (currentPos == text.Length)
                    return (char)0;             //EOF
                return text[currentPos];
            }
        }
        public bool IsEnd()
        {
            return currentPos == text.Length;
        }
        
        public void Next()
        {
            if (currentPos < text.Length)
                currentPos++;
        }

        public bool SkipIf(string s)
        {
            int NameLength = s.Length;

            if (text.Length >= NameLength + currentPos)
                if (text.Substring(currentPos, NameLength) == s)
                {
                    currentPos += NameLength;
                    return true;
                }
            return false;
        }

        public void Save()
        {
            completedPos = currentPos;
        }

        public void Rollback()
        {
            currentPos = completedPos;
        }

        public Source Clone()
        {
            return new Source(text) { completedPos = completedPos, currentPos = currentPos };
        }

        public void Check(Node node) //метод не для этой сущности
        {
            if (node != null)
            {
                node.position = completedPos;
                Save();
            }
            else
                Rollback();
        }

        public void Check(ExprNode node) //метод не для этой сущности
        {
            if (node != null)
            {
                node.stringPos = completedPos;
                Save();
            }
            else
                Rollback();
        }
    }


    class Context
    {
        public List<Variable> variables;
        public Context parent;
        public Value returnValue;

        public Context(Context parent)
        {
            this.parent = parent;
            variables = new List<Variable>();
            //returnValue = new Null();
        }
        
        public Variable FindFirstVar(string name, bool canCreate = true)
        {
            Variable res = variables.LastOrDefault(a => a.name == name);
            if (res == null)
            {                
                if (parent != null)
                {
                    res = parent.FindFirstVar(name, false);
                }
                if (canCreate && res == null)
                {
                    res = new Variable(name);
                    variables.Add(res);
                }
            }
            return res;
        }

        public void AddVar(string ident)
        {
            Variable res = new Variable(ident);
            /*if (FindFirstVar(ident, false) != null)
            {
                throw new lolException();
            }*/
            variables.Add(res);
        }

        public void SetVars(ArgumentsFact argsList)
        {
            for (int i = 0; i < argsList.args.Count; i++)
			{
                variables[i].Value = argsList.args[i].GetValue(this.parent);
			}
        }

        public void GenerateNVars(int n)
        {
            for (int i = 0; i < n; i++)
            {
                AddVar("_" + i.ToString());
            }
        }
    }

    class Variable : Value
    {
        public string name;
        Value value;

        public Variable(string name)
        {
            this.name = name;
            value = new Null();
        }

        public Value Value 
        {
            get
            {
                return value;
            }
            set
            {
                this.value = value;
                if (value != null)
                    this.nodeType = value.nodeType;
            }
        }

        public static Variable TryParse(ref Source s)
        {
            Variable res = null;
            string ident = Identifier.Parse(ref s);
            if (ident == "")
                return null;
            res = new Variable(ident);
            return res;
        }

        public override string Print()
        {
            return this.ToString();
        }

        public override double DoubleValue()
        {
            return value.DoubleValue();
        }

        public override bool BoolValue()
        {
            return value.BoolValue();
        }

        public override Value GetValue(Context context)
        {
            var temp = context.FindFirstVar(name, false);
            if (temp != null)
            {
                value = temp.value;
                value.argsList = new Queue<ArgumentsFact>(argsList);
            }
            return value.GetValue(context);
        }

        public override ExprNode GetRoot()
        {
            return this;
        }

        public override string ToString()
        {
            string res = name;
            bool first = true;
            foreach (ArgumentsFact item in argsList)
            {
                if (item != null)
                {
                    if (!first)
                        res += ", ";
                    first = false;
                    res += item.ToString();
                }
            }
            return res;
        }


    }

    abstract class Node
    {
        public int position;
    }

    class StatementSequence : Statement
    {
        List<Statement> statements;

        public StatementSequence()
        {
            statements = new List<Statement>();
        }

        public static StatementSequence TryParse(ref Source s)
        {
            StatementSequence res = new StatementSequence();
            Spaces.Skip(ref s);
            bool flag = s.SkipIf("{");

            while (true)
            {
                Statement st = Statement.TryParseAny(ref s);
                if (st != null)
                {
                    res.AddStatement(st);
                }
                else
                    break;
            }
            Spaces.Skip(ref s);
            if (flag)
                if (!s.SkipIf("}"))
                    return null;

            return res;
        }

        public void AddStatement(Statement statement)
        {
            statements.Add(statement);
        }

        public override bool Execute(Context context)
        {
            foreach (Statement statement in statements)
            {
                if (!statement.Execute(context))
                    return false;
                if (context.returnValue != null)
                    if (context.returnValue.nodeType != ExprNodeType.Null) //TODO: how to return NULL?
                        return true;
            }
            if (context.returnValue == null)
                context.returnValue = new Null();
            return true;
        }

        public bool IsPrint()
        {
            return statements.Count == 1 && statements[0] is PrintStatement;
        }

        public override string ToString()
        {
            string res = "";
            foreach (Statement item in statements)
            {
                res += item.ToString();
            }
            return res;
        }
    }

    abstract class Statement : Node
    {
        public static Statement TryParseAny(ref Source s)
        {
            Spaces.Skip(ref s);

            Statement res;
            s.Save();

            res = If.TryParse(ref s);
            if (res == null)
            {
                s.Rollback();
                res = While.TryParse(ref s);
            }
            if (res == null)
            {
                s.Rollback();
                res = Return.TryParse(ref s);
            }
            if (res == null)
            {
                s.Rollback();
                res = Assignment.TryParse(ref s);
            }
            if (res == null)
            {
                s.Rollback();
                res = ExprStatement.TryParse(ref s);
            }
            if (res == null)
            {
                s.Rollback();
                s.SkipIf(";");
            }

            s.Check(res);
            return res;
        }

        public abstract bool Execute(Context context);
    }

    class If : Statement
    {
        public Expression condition;
        public StatementSequence body;

        public If()
        {
        }

        public static If TryParse(ref Source s)
        {
            const string IfName = "if";
            If res = null;

            if (!s.SkipIf(IfName))
            {
                s.Rollback();
                return null;
            }
            res = new If();

            res.condition = Condition.TryParse(ref s);
            if (res.condition == null)
            {
                s.Rollback();
                return null;
            }
            
            Source tempSource = s.Clone();
            res.body = StatementSequence.TryParse(ref tempSource);
            s.currentPos = tempSource.currentPos;
            s.Save();

            return res;
        }

        public override bool Execute(Context context)
        {
            Value val = condition.GetValue(context);
            if (val != null)
            {
                if (val.BoolValue())
                {
                    body.Execute(context);
                }
            }
            return true;
        }

        public override string ToString()
        {
            return string.Format("if({0}){{\n{1}}}\n", condition, body);
        }
    }

    class While : Statement
    {
        Expression condition;
        StatementSequence body;

        public While()
        {
        }

        public While(Expression condition)
        {
            this.condition = condition;
        }

        public static While TryParse(ref Source s)
        {
            const string WhileName = "while";
            While res = null;

            if (!s.SkipIf(WhileName))
            {
                s.Rollback();
                return null;
            }
            res = new While();

            res.condition = Condition.TryParse(ref s);
            if (res.condition == null)
            {
                s.Rollback();
                return null;
            }

            Source tempSource = s.Clone();
            res.body = StatementSequence.TryParse(ref tempSource);
            s.currentPos = tempSource.currentPos;
            s.Save();

            return res;
        }

        public override bool Execute(Context context)
        {
            while (true)
            {
                Value val = condition.GetValue(context);
                if (val != null)
                {
                    if (val.BoolValue())
                    {
                        body.Execute(context);
                    }
                    else
                        break;
                }
                else
                    break;
            }

            return true;
        }
        public override string ToString()
        {
            return string.Format("while({0}){{\n{1}}}\n", condition, body);
        }
    }

    class PrintStatement : Statement
    {
        ProgramParse program;

        public PrintStatement(ref ProgramParse program)
        {
            this.program = program;
        }

        public override bool Execute(Context context)
        {
            if (context.variables == null)
                return false;
            program.output += ">>";
            for (int i = 0; i < context.variables.Count; i++)
            {
                Value val = context.variables[i].GetValue(context);
                if (val != null)
                {
                    program.output += val.Print() + " ";
                }
                else
                    return false;
            }
            program.output += Environment.NewLine;
            return true;
        }

        public override string ToString()
        {
            return "MagicPrint;\n";
        }
    }

    class Condition
    {
        public static Expression TryParse(ref Source s)
        {
            Spaces.Skip(ref s);
            if (!s.SkipIf("("))
                return null;
            Expression res = new Expression(ref s);
            if (!res.Correct())
            {
                s.Rollback();
                return null;
            }
            Spaces.Skip(ref s);
            if (!s.SkipIf(")"))
            {
                s.Rollback();
                return null;
            }
            return res;
        }
    }

    class ArgumentsFact
    {
        public List<Expression> args;

        public ArgumentsFact()
        {
            args = new List<Expression>();
        }

        public void AddArg(Expression expr)
        {
            args.Add(expr);
        }

        public static ArgumentsFact TryParse(ref Source s)
        {
            ArgumentsFact res = new ArgumentsFact();
            Spaces.Skip(ref s);
            if (!s.SkipIf("("))
                return null;

            while (true)
            {
                Spaces.Skip(ref s);
                if (s.SkipIf(")"))
                {
                    break;
                }
                Source tempSource = s.Clone();
                Expression expr = new Expression(ref tempSource);
                if (!expr.Correct())
                {
                    throw new lolException();
                }
                s.currentPos = tempSource.currentPos;
                s.Save();
                res.AddArg(expr);

                Spaces.Skip(ref s);
                if (s.SkipIf(","))
                    continue;
                if (s.SkipIf(")"))
                    break;
            }
            //s.Rollback();
          
            return res;
        }

        public override string ToString()
        {
            string res = "(";
            bool first = true;
            foreach (Expression item in args)
            {
                if (!first)
                    res += ", ";
                first = false;
                res += item.ToString();
            }
            res += ")";
            return res;
        }
    }

    class ArgumentsFormal
    {
        public static bool TryParse(ref Source s, FuncValue fv)
        {
            Spaces.Skip(ref s);
            if (!s.SkipIf("("))
                return false;

            while (true)
            {
                string ident = Identifier.Parse(ref s);
                if (ident == "") 
                    break;
                fv.arguments.Add(ident);
                if (!s.SkipIf(","))
                        break;
            }

            Spaces.Skip(ref s);
            if (!s.SkipIf(")")) 
                return false;
            return true;
        }
    }

    class lolException : Exception
    {

    }

    class CallExcpetion : Exception
    {

    }


    /*class FuncCall : Expression
    {
        public Expression func;
        public ArgumentsFact argsList;


        public FuncCall(Expression expr)
            : base(expr)
        {
            this.func = expr;
        }

        public static FuncCall TryParse(Expression expr, ref Source s) //TODO: funccall(Expr e, Expr[] args) 
        {
            FuncCall res = null;

            Source tempSource = s.Clone();
            ArgumentsFact tempArgs = ArgumentsFact.TryParse(ref tempSource);
            if (tempArgs == null)
            {
                //s.Rollback();
                return null;
            }
            s.currentPos = tempSource.currentPos;
            s.Save();

            res = new FuncCall(expr);
            res.argsList = tempArgs;

            return res;
        }

        public Value Call(Context context)
        {
            var val = func.GetValue(context);
            if (val.nodeType != ExprNodeType.Function)
                throw new CallExcpetion();
            return (val as FuncValue).GetValue(context);
        }

        public override string ToString()
        {
            string res = string.Join(", ", argsList.args);
            return string.Format("{0}({1})", func, res);
        }
        
        public Value GetValue(Context context)
        {
            return Call(context);
        }

    }*/

    class FuncValue : Value
    {
        public StatementSequence body;
        public List<string> arguments = new List<string>();

        public FuncValue()
        {
            nodeType = ExprNodeType.Function;
        }

        public static FuncValue TryParse(ref Source s)
        {
            const string funcName = "function";
            FuncValue res = null;

            if (!s.SkipIf(funcName))
            {
                s.Rollback();
                return null;
            }

            res = new FuncValue();

            if (!ArgumentsFormal.TryParse(ref s, res))
            {
                s.Rollback();
                return null;
            }


            Source tempSource = s.Clone();
            res.body = StatementSequence.TryParse(ref tempSource);
            s.currentPos = tempSource.currentPos;
            s.Save();

            return res;
        }

        public override string Print()
        {
            string argsStr = "";
            bool first = true;
            foreach (string item in arguments)
            {
                if (!first)
                    argsStr += ", ";
                first = false;
                argsStr += item;
            }

            string res = body.ToString();

            string argsCall = "";
            first = true;
            foreach (ArgumentsFact item in argsList)
            {
                if (item != null)
                {
                    if (!first)
                        argsCall += ", ";
                    first = false;
                    argsCall += item.ToString();
                }
            }
            return string.Format("function({0}) {{\n{1}}}{2}", argsStr, res, argsCall);
        }

        public override Value GetValue(Context context)
        {
            if (argsList.Count == 0)
                return this;
            else
            {
                var tempContext = new Context(context);
                Execute(tempContext);
                while (argsList.Count > 0)
                {
                    var tempFunc = tempContext.returnValue;
                    tempFunc.argsList = argsList;
                    tempContext = new Context(tempContext);
                    (tempFunc as FuncValue).Execute(tempContext);
                    //tempContext.returnValue = tempFunc.GetValue(tempContext);
                }
                return tempContext.returnValue;
            }
        }

        public override ExprNode GetRoot()
        {
            return this;
        }


        public bool Execute(Context context)
        {
            ArgumentsFact currentArgsList = null;
            if (argsList.Count>0)
                currentArgsList = argsList.Dequeue();
            foreach (string item in arguments)
            {
                context.AddVar(item);
            }
            if (body.IsPrint())
            {
                if (currentArgsList != null)
                    context.GenerateNVars(currentArgsList.args.Count);
            }
            if (currentArgsList != null)
                context.SetVars(currentArgsList);
            body.Execute(context);
            return true;
        }

    }

    class Assignment : Statement
    {
        Variable variable;
        Expression expr;

        public Assignment(Variable v, Expression e)
        {
            variable = v;
            expr = e;
        }


        public static Assignment TryParse(ref Source s)
        {
            Assignment res = null;

            string ident = Identifier.Parse(ref s);
            if (ident == "")
            {
                s.Rollback();
                return null;
            }

            Spaces.Skip(ref s);
            if (!s.SkipIf("="))
            {
                s.Rollback();
                return null;
            }
            Variable v = new Variable(ident);
            Spaces.Skip(ref s);
            Expression expr = new Expression(ref s);

            if (expr.Correct())
            {
                res = new Assignment(v,expr);
            }

            Spaces.Skip(ref s);
            if (!s.SkipIf(";"))
            {
                s.Rollback();
                return null;
            }

            return res;
        }

        public override bool Execute(Context context)
        {
            var tempVar = context.FindFirstVar(variable.name);
            variable = tempVar;
            variable.Value = expr.GetValue(context);
            return true;
        }

        public override string ToString()
        {
            return string.Format("{0} = {1};\n", variable.name, expr) ;
        }
    }

    class Return : Statement
    {
        Expression expr;

        public Return(Expression e)
        {
            expr = e;
        }

        public static Return TryParse(ref Source s)
        {
            const string returnName = "return";
            Return res = null;

            Spaces.Skip(ref s);
            if (!s.SkipIf(returnName))
            {
                s.Rollback();
                return null;
            }

            Spaces.Skip(ref s);
            Expression expr = new Expression(ref s);

            if (expr.Correct())
            {
                res = new Return(expr);
            }

            Spaces.Skip(ref s);
            if (!s.SkipIf(";"))
            {
                s.Rollback();
                return null;
            }

            return res;
        }

        public override bool Execute(Context context)
        {
            context.returnValue = expr.GetValue(context);
            return true;
        }

        public override string ToString()
        {

            return string.Format("return {0};\n", expr);
        }

    }

    class ExprStatement : Statement
    {
        Expression expr;

        public ExprStatement(Expression e)
        {
            expr = e;
        }

        public static ExprStatement TryParse(ref Source s)
        {
            ExprStatement res = null;

            Spaces.Skip(ref s);
            Expression expr = new Expression(ref s);

            if (expr.Correct())
            {
                res = new ExprStatement(expr);
            }

            Spaces.Skip(ref s);
            if (!s.SkipIf(";"))
            {
                s.Rollback();
                return null;
            }

            return res;
        }

        public override bool Execute(Context context)
        {
            expr.GetValue(context);
            return true;
        }

        public override string ToString()
        {
            return expr.ToString()+ ";\n";
        }

    }

    static class Identifier
    {
        public static string Parse(ref Source s)
        {
            string res = "";
            Spaces.Skip(ref s);
            if (char.IsLetter(s.CurrSymbol) || s.CurrSymbol == '_')
            {
                res += s.CurrSymbol;
                s.Next();
            }
            else
                return res;

            while (char.IsLetterOrDigit(s.CurrSymbol) || s.CurrSymbol == '_')//!Spaces.IsSpace(s.CurrSymbol) && s.CurrSymbol != (char)0 && 
            {
                res += s.CurrSymbol;
                s.Next();
            }

            return res;
        }
    }

    static class Spaces
    {
        public static bool IsSpace(char c)
        {
            return c == ' ' || c == '\t' || c == '\n' || c == '\r'; // space || tab || enter 
        }

        public static void Skip(ref Source s)
        {
            while (IsSpace(s.CurrSymbol))
                s.Next();
        }
    }
}
