using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ParserNamespace
{
    enum ExprNodeType { Bool, Number, Null, Operator, Function};

    abstract class ExprNode
    {
        public int stringPos;
        public ExprNodeType nodeType;
        public abstract Value GetValue(Context context);
        public abstract ExprNode GetRoot();
        public abstract string Print();
        public void SkipSpaces(string s, ref int pos)
        {
            while (s[pos] == ' ')
                pos++;
        }
    }

    abstract class Value : ExprNode
    {
        public Queue<ArgumentsFact> argsList = new Queue<ArgumentsFact>();                 //suffix

        public virtual double DoubleValue()
        {
            return 0;
        }

        public virtual bool BoolValue()
        {
            return false;
        }

        public static Value TryParse(ref Source s)
        {
            Value res = null;
            Null tempNull = Null.TryParse(ref s);
            if (tempNull != null)
            {
                res = tempNull;
            }
            Number tempNum = Number.TryParse(ref s);
            if (tempNum != null)
            {
                res = tempNum;
            }
            Bool tempBool = Bool.TryParse(ref s);
            if (tempBool != null)
            {
                res = tempBool;
            }
            FuncValue tempFunc = FuncValue.TryParse(ref s);
            if (tempFunc != null)
            {
                res = tempFunc;
            }
            Variable tempVar = Variable.TryParse(ref s);
            if (tempVar != null)
            {
                res = tempVar;
            }
            if (res != null)
            {
                while (true)
                {
                    var tempArgs = ArgumentsFact.TryParse(ref s);
                    if (tempArgs != null)
                        res.argsList.Enqueue(tempArgs);
                    else
                        break;
                }
                //if (res.args != null && res.nodeType != ExprNodeType.Function)
                //    return null;
            }
            return res;
        }
    }

    class Null : Value
    {
        public Null()
        {
            nodeType = ExprNodeType.Null;
        }

        public static Null TryParse(ref Source s)
        {
            const string NullName = "null";
            Spaces.Skip(ref s);
            s.Save();
            if (!s.SkipIf(NullName))
                return null;

            return new Null();
        }

        public override string Print()
        {
            return "null";
        }

        public override Value GetValue(Context context)
        {
            return this;
        }

        public override ExprNode GetRoot()
        {
            return this;
        }
    }

    class Number : Value
    {
        double value;

        public Number(double val)
        {
            value = val;
            nodeType = ExprNodeType.Number;
        }

        public override string Print()
        {
            return value.ToString();
        }

        public override double DoubleValue()
        {
            return value;
        }

        public override Value GetValue(Context context)
        {
            return this;
        }

        public override ExprNode GetRoot()
        {
            return this;
        }

        public static Number TryParse(ref Source s)
        {
            Number res = null;
            int val = 0;
            bool flag = false;
            s.Save();
            for (; !s.IsEnd(); s.Next())
            {
                if (char.IsDigit(s.CurrSymbol))
                {
                    val = val * 10 + (s.CurrSymbol - '0');
                    flag = true;
                }
                else
                    break;
            }
            if (flag)
            {
                res = new Number(val);
                s.Check(res);
            }
            return res;
        }

        public override string ToString()
        {
            return value.ToString();
        }
    }

    class Bool : Value
    {
        bool value;

        public override string Print()
        {
            return value.ToString();
        }
        
        public override bool BoolValue()
        {
            return value;
        }

        public Bool(bool b)
        {
            value = b;
            nodeType = ExprNodeType.Bool;
        }

        public override Value GetValue(Context context)
        {
            return this;
        }

        public override ExprNode GetRoot()
        {
            return this;
        }

        public override double DoubleValue()
        {
            return value ? 1 : 0;
        }

        public static Bool TryParse(ref Source s)
        {
            Bool res = null;
            s.Save();
            if (s.SkipIf("true"))
            {
                res = new Bool(true);
            }
            else
                if (s.SkipIf("false"))
                {
                    res = new Bool(false);
                }
            s.Check(res);
            return res;
        }
    }

    class Operator : ExprNode
    {
        static string[] types = { "+", "-", "*", "/", "&", "|", "<=", ">=", "==", "!=", "<", ">", "%" };
        static int[] priorities = { 2, 2, 1, 1, 1, 2, 3, 3, 3, 3, 3, 3, 2 };

        int type;
        public ExprNode arg1, arg2;
        public Operator parent;
        //public Context context;
        public int level = 0;

        public Operator(String _type, ExprNode _arg1, ExprNode _arg2)
        {
            nodeType = ExprNodeType.Operator;
            arg1 = _arg1;
            arg2 = _arg2;
            type = -1;
            for (int i = 0; i < types.Length; i++)
            {
                if (types[i] == _type)
                {
                    type = i;
                    break;
                }
            }
        }

        public override string Print()
        {
            return types[type].ToString();
        }

        public override ExprNode GetRoot()
        {
            if (parent == null)
                return this;
            else
                return parent.GetRoot();
        }

        public static Operator TryParse(ref Source s)
        {
            Operator res = null;
            s.Save();
            for (int i = 0; i < types.Length; i++)
            {
                if (s.SkipIf(types[i]))
                {
                    res = new Operator(types[i], null, null);
                    break;
                }
            }

            s.Check(res);
            return res;
        }

        public void SetArg1(ExprNode _arg1)
        {
            arg1 = _arg1;
        }

        public void SetArg2(ExprNode _arg2)
        {
            arg2 = _arg2;
        }

        public bool HaveArg2()
        {
            return arg2 != null;
        }
        
        public int GetPriority()
        {
            return priorities[type];
        }

        public bool Balance()
        {
            bool res = false;
            if (parent != null)
            {
                if (parent.level == level)
                {
                    if (parent.GetPriority() <= GetPriority())
                    {
                        res = true;
                        parent.arg2 = arg1;
                        arg1 = parent;
                        var temp = parent.parent;
                        if (parent.parent != null)
                            parent.parent.SetArg2(this);
                        parent.parent = this;
                        parent = temp;
                    }
                }
                else                if (parent.level > level)
                {
                    res = true;
                    parent.arg2 = arg1;
                    arg1 = parent;
                    var temp = parent.parent;
                    if (parent.parent != null)
                        parent.parent.SetArg2(this);
                    parent.parent = this;
                    parent = temp;
                }
            }
            return res;
        }

        public override Value GetValue(Context context)
        {
            return null;
        }

        public Value GetValue(bool strict, ref int ErrPos, Context context)
        {
            Value result = null;
            if (arg1 != null && arg2 != null)
            {
                Value aa, ab;
                if (arg1 is Operator)
                    aa = (arg1 as Operator).GetValue(strict, ref ErrPos, context);
                else if (arg1 is Variable)
                    aa = context.FindFirstVar((arg1 as Variable).name).GetValue(context);
                else
                    aa = arg1.GetValue(context);
                if (arg2 is Operator)
                    ab = (arg2 as Operator).GetValue(strict, ref ErrPos, context);
                else if (arg2 is Variable)
                    ab = context.FindFirstVar((arg2 as Variable).name).GetValue(context);
                else
                    ab = arg2.GetValue(context);


                double d1 = aa.DoubleValue();
                double d2 = ab.DoubleValue();
                bool b1 = aa.BoolValue();
                bool b2 = ab.BoolValue();
                if (strict)
                    if (aa is Bool && ab is Number || aa is Number && ab is Bool)
                    {
                        ErrPos = stringPos;
                        return null;
                    }
                switch (type)
                {
                    case 0:
                        result = new Number(d1 + d2);
                        break;
                    case 1:
                        result = new Number(d1 - d2);
                        break;
                    case 2:
                        result = new Number(d1 * d2);
                        break;
                    case 3:
                        result = new Number(d1 / d2);
                        break;
                    case 4:
                        result = new Bool(b1 && b2);
                        break;
                    case 5:
                        result = new Bool(b1 || b2);
                        break;
                    case 6:
                        result = new Bool(d1 <= d2);
                        break;
                    case 7:
                        result = new Bool(d1 >= d2);
                        break;
                    case 8:
                        result = new Bool(d1 == d2);
                        break;
                    case 9:
                        result = new Bool(d1 != d2);
                        break;
                    case 10:
                        result = new Bool(d1 < d2);
                        break;
                    case 11:
                        result = new Bool(d1 > d2);
                        break;
                    case 12:
                        result = new Number(d1 % d2);
                        break;
                    default:
                        break;
                }
            }
            else
                result = new Bool(false);
            return result;
        }
    }

}
