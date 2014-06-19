using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ParserNamespace
{
    class Expression
    {
        ExprNode root;
        private bool strict = true;
        private int SyntaxErrorPos = -1;
        private int k = 0;
        //Context context;

        public Expression(ref Source s)
        {
            root = Parse(ref s, null);
            if (k != 0 )
                SyntaxErrorPos = s.completedPos;
            if (root != null)
                root = root.GetRoot();
        }

        public Expression(Expression expr)
        {
            root = expr.root;
            //context = expr.context;
        }

        public Value GetValue(Context context)
        {
            if (root != null)
            {
                Value val;
                if (root is Operator)
                    val = (root as Operator).GetValue(true, ref SyntaxErrorPos, context);
                else
                    val = root.GetValue(context);
                return val;
            }
            return null;
        }

        public void ParseBracketOpen(ref Source s)
        {
            while (true)
            {
                if (!s.IsEnd())
                {
                    if (s.CurrSymbol == '(')
                    {
                        k++;
                        s.Next();
                        continue;
                    }
                }
                break;
            }
            if (k < 0)
                SyntaxErrorPos = s.currentPos;
        }

        public void ParseBracketClose(ref Source s)
        {
            while (true)
            {
                if (!s.IsEnd())
                {
                    if (s.CurrSymbol == ')')
                    {
                        if (k == 0)
                        {
                            break;
                        }
                        k--;
                        s.Next();
                        continue;
                    }
                }
                break;
            }
            if (k < 0)
                SyntaxErrorPos = s.currentPos;
        }

        public ExprNode Parse(ref Source s, Operator parent)
        {
            Spaces.Skip(ref s);
            ParseBracketOpen(ref s);
            Spaces.Skip(ref s);

            Value left = Value.TryParse(ref s);

            Spaces.Skip(ref s);
            ParseBracketClose(ref s);
            if (left == null)
                SyntaxErrorPos = s.currentPos;

            Spaces.Skip(ref s);
            if (s.IsEnd() ||  SyntaxErrorPos >= 0)
            {
                return left;
            }
            else
            {
                int opPos = s.currentPos;
                Operator op = Operator.TryParse(ref s);
                if (op != null)
                {
                    //op.context = context;
                    op.level = k;
                    op.SetArg1(left);
                    op.parent = parent;
                    if (parent != null)
                        parent.SetArg2(op);
                    while (op.Balance())
                        ;
                    //ParseBracketOpen(ref s);
                    if (SyntaxErrorPos >= 0)
                    {
                        SyntaxErrorPos = s.currentPos;
                        return op;
                    }
                    ExprNode right = Parse(ref s, op);
                    if (!op.HaveArg2())
                        op.SetArg2(right);
                }
                else
                {
                    return left;
                }
                return op;
            }
        }

       /* public string PrintRes()
        {
            if (SyntaxErrorPos >= 0)
                return "Ошибка парсинга в позиции " + SyntaxErrorPos.ToString();
            if (root != null)
            {
                Value val;
                if (root is Operator)
                    val = (root as Operator).GetValue(true, ref SyntaxErrorPos, context);
                else
                    val = root.GetValue();
                if (SyntaxErrorPos >= 0)
                    return "Ошибка вычисления " + SyntaxErrorPos.ToString();
                if (val.nodeType == ExprNodeType.Number)
                    return val.DoubleValue().ToString();
                else
                    return val.BoolValue().ToString();
            }
            else
                return "Парсинг не удался";
        }*/
        public bool Correct()
        {
            return SyntaxErrorPos == -1;
        }

        public string PrintTree()
        {
            string res = "";
            if (SyntaxErrorPos < 0)
                if (root != null)
                    PrintNode(ref res, root, 0);
            return res;
        }

        string Pad(string s, int k)
        {
            for (int i = 0; i < k; i++)
            {
                s += ' ';
            }
            return s;
        }

        public void PrintNode(ref string s, ExprNode node, int k)
        {
            if (node.nodeType == ExprNodeType.Operator)
            {
                PrintNode(ref s, (node as Operator).arg1, k + 0);

                s = Pad(s, k);
                s += node.Print();
                PrintNode(ref s, (node as Operator).arg2, k + 0);
            }
            else
            {
                s = Pad(s, k);
                s += node.Print();
            }
        }

        public override string ToString()
        {
            return PrintTree();
        }

    }
}
