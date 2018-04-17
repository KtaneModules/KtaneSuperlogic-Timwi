namespace Superlogic
{
    abstract class Expression { }

    sealed class VariableExpression : Expression
    {
        public int Variable { get; private set; }
        public VariableExpression(int variable) { Variable = variable; }
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
}
