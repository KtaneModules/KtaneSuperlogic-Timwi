using System;

namespace Superlogic
{
    abstract class Expression
    {
        public abstract bool Evaluate(int valuesBitfield);
        public abstract string ToString(bool paren);
        public override string ToString()
        {
            return ToString(paren: false);
        }

        public abstract float Instantiate(float x, bool paren, Func<char, float, float> instantiate);
    }

    sealed class VariableExpression : Expression
    {
        public int Variable { get; private set; }
        public VariableExpression(int variable) { Variable = variable; }

        public override bool Evaluate(int valuesBitfield)
        {
            return (valuesBitfield & (1 << Variable)) != 0;
        }

        public override string ToString(bool paren)
        {
            return ((char) ('A' + Variable)).ToString();
        }

        public override float Instantiate(float x, bool paren, Func<char, float, float> instantiate)
        {
            return instantiate((char) ('A' + Variable), x);
        }
    }

    enum BinaryOperator
    {
        And = 0,
        Or = 1,
        Xor = 2,
        Nand = 3,
        Nor = 4,
        Xnor = 5,
        Implies = 6,
        ImpliedBy = 7,
        NumOperators = 8
    }

    sealed class BinaryOperatorExpression : Expression
    {
        public Expression Left { get; private set; }
        public Expression Right { get; private set; }
        public BinaryOperator Operator { get; private set; }
        public BinaryOperatorExpression(Expression left, Expression right, BinaryOperator op)
        {
            Left = left;
            Right = right;
            Operator = op;
        }

        public override bool Evaluate(int valuesBitfield)
        {
            var l = Left.Evaluate(valuesBitfield);
            var r = Right.Evaluate(valuesBitfield);
            switch (Operator)
            {
                case BinaryOperator.And: return l & r;
                case BinaryOperator.Or: return l | r;
                case BinaryOperator.Xor: return l ^ r;
                case BinaryOperator.Nand: return !(l & r);
                case BinaryOperator.Nor: return !(l | r);
                case BinaryOperator.Xnor: return !(l ^ r);
                case BinaryOperator.Implies: return !l | r;
                case BinaryOperator.ImpliedBy: return !r | l;
            }
            throw new NotImplementedException();
        }

        public override string ToString(bool paren)
        {
            return string.Format(paren ? "({0} {1} {2})" : "{0} {1} {2}", Left.ToString(paren: true), op(Operator), Right.ToString(paren: true));
        }

        private char op(BinaryOperator @operator)
        {
            switch (@operator)
            {
                case BinaryOperator.And: return '∧';
                case BinaryOperator.Or: return '∨';
                case BinaryOperator.Xor: return '⊻';
                case BinaryOperator.Nand: return '|';
                case BinaryOperator.Nor: return '↓';
                case BinaryOperator.Xnor: return '↔';
                case BinaryOperator.Implies: return '→';
                case BinaryOperator.ImpliedBy: return '←';
            }
            return '\0';
        }

        public override float Instantiate(float x, bool paren, Func<char, float, float> instantiate)
        {
            var adv = 0f;
            if (paren)
                adv += instantiate('(', x);
            adv += Left.Instantiate(x + adv, true, instantiate);
            adv += instantiate(op(Operator), x + adv);
            adv += Right.Instantiate(x + adv, true, instantiate);
            if (paren)
                adv += instantiate(')', x + adv);
            return adv;
        }
    }

    sealed class NotExpression : Expression
    {
        public Expression Inner { get; private set; }
        public NotExpression(Expression inner) { Inner = inner; }

        public override bool Evaluate(int valuesBitfield)
        {
            return !Inner.Evaluate(valuesBitfield);
        }

        public override string ToString(bool paren)
        {
            return "¬" + Inner.ToString(paren: true);
        }

        public override float Instantiate(float x, bool paren, Func<char, float, float> instantiate)
        {
            var adv = instantiate('¬', x);
            adv += Inner.Instantiate(x + adv, true, instantiate);
            return adv;
        }
    }
}
